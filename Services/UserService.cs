using Netplwiz.Helpers;
using Netplwiz.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Netplwiz.Services
{
    public interface IUserService
    {
        List<UserAccount> GetLocalUsers();
        UserAccount? GetUserDetails(string userName);
        bool UpdateUser(UserAccount user);
        bool DeleteUser(string userName);
        bool SetPassword(string userName, string newPassword);
        bool IsSecureLogonRequired();
        void SetSecureLogonRequired(bool required);
        void OpenCredentialManager();
        void OpenAdvancedUserManagement();
        void OpenAddUserDialog();
        bool AddLocalUser(string userName, string password, string fullName, string description, bool isAdministrator);
        bool IsProtectedAccount(string userName);
        PasswordPolicy GetPasswordPolicy();
        bool SetPasswordPolicy(PasswordPolicy policy);
    }

    public class UserService : IUserService
    {
        // P/Invoke for reading password policy via NetUserModalsGet
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetUserModalsGet(
            string? serverName,
            int level,
            out IntPtr bufPtr);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr bufPtr);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetUserModalsSet(
            string? serverName,
            int level,
            ref USER_MODALS_INFO_0 buf,
            out int parm_err);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetUserModalsSet(
            string? serverName,
            int level,
            ref USER_MODALS_INFO_3 buf,
            out int parm_err);

        [StructLayout(LayoutKind.Sequential)]
        private struct USER_MODALS_INFO_0
        {
            public int min_passwd_len;
            public int max_passwd_age;
            public int min_passwd_age;
            public int force_logoff;
            public int password_hist_len;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct USER_MODALS_INFO_3
        {
            public int lockout_duration;
            public int lockout_observation_window;
            public int lockout_threshold;
        }

        private static readonly ReadOnlyCollection<string> ProtectedAccounts = new List<string>
        {
            "administrator",
            "guest",
            "defaultaccount",
            "wdagutilityaccount"
        }.AsReadOnly();

        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<UserService>();

        public bool IsProtectedAccount(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return true;
            var normalized = userName.Trim().ToLowerInvariant();
            if (ProtectedAccounts.Contains(normalized)) return true;

            // Also protect the currently logged-in user
            var currentName = Environment.UserName?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(currentName) && normalized == currentName) return true;

            return false;
        }

        public List<UserAccount> GetLocalUsers()
        {
            _logger.Information("Getting local users...");
            var users = new List<UserAccount>();

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var userPrincipal = new UserPrincipal(context);
                var searcher = new PrincipalSearcher(userPrincipal);

                foreach (var result in searcher.FindAll())
                {
                    if (result is UserPrincipal user)
                    {
                        var account = MapToUserAccount(user, context);
                        users.Add(account);
                        _logger.Debug("Found user: {UserName}, Groups: {Groups}", account.UserName, account.GroupsDisplay);
                    }
                }

                _logger.Information("Found {Count} local users", users.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get local users");
                throw;
            }

            return users;
        }

        public UserAccount? GetUserDetails(string userName)
        {
            _logger.Information("Getting details for user: {UserName}", userName);

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var user = UserPrincipal.FindByIdentity(context, userName);
                if (user == null)
                {
                    _logger.Warning("User not found: {UserName}", userName);
                    return null;
                }

                return MapToUserAccount(user, context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get user details for {UserName}", userName);
                throw;
            }
        }

        public bool UpdateUser(UserAccount user)
        {
            _logger.Information("Updating user: {UserName}", user.UserName);

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var principal = UserPrincipal.FindByIdentity(context, user.UserName);
                if (principal == null)
                {
                    _logger.Warning("Cannot update - user not found: {UserName}", user.UserName);
                    return false;
                }

                principal.DisplayName = user.FullName;
                principal.Description = user.Description;
                principal.Save();

                _logger.Information("User updated successfully: {UserName}", user.UserName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update user: {UserName}", user.UserName);
                return false;
            }
        }

        public bool DeleteUser(string userName)
        {
            _logger.Information("Deleting user: {UserName}", userName);

            if (IsProtectedAccount(userName))
            {
                _logger.Warning("Delete blocked - protected account: {UserName}", userName);
                return false;
            }

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var user = UserPrincipal.FindByIdentity(context, userName);
                if (user == null)
                {
                    _logger.Warning("Cannot delete - user not found: {UserName}", userName);
                    return false;
                }

                user.Delete();
                _logger.Information("User deleted successfully: {UserName}", userName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete user: {UserName}", userName);
                return false;
            }
        }

        public bool SetPassword(string userName, string newPassword)
        {
            _logger.Information("Setting password for user: {UserName}", userName);

            if (IsProtectedAccount(userName))
            {
                _logger.Warning("Password change blocked - protected account: {UserName}", userName);
                return false;
            }

            var policy = GetPasswordPolicy();
            if (!policy.IsPasswordValid(newPassword, out string? error))
            {
                _logger.Warning("Password change blocked - policy violation for {UserName}: {Error}", userName, error);
                return false;
            }

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);
                var user = UserPrincipal.FindByIdentity(context, userName);
                if (user == null)
                {
                    _logger.Warning("Cannot set password - user not found: {UserName}", userName);
                    return false;
                }

                user.SetPassword(newPassword);
                user.Save();
                _logger.Information("Password set successfully for user: {UserName}", userName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set password for user: {UserName}", userName);
                return false;
            }
        }

        public bool IsSecureLogonRequired()
        {
            _logger.Debug("Checking if secure logon is required");
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                var value = key?.GetValue("DisableCAD") as int? ?? 0;
                var required = value == 0;
                _logger.Debug("Secure logon required: {Required}", required);
                return required;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check secure logon setting");
                return false;
            }
        }

        public void SetSecureLogonRequired(bool required)
        {
            _logger.Information("Setting secure logon required: {Required}", required);
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", writable: true);
                key?.SetValue("DisableCAD", required ? 0 : 1, Microsoft.Win32.RegistryValueKind.DWord);
                _logger.Information("Secure logon setting updated successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set secure logon setting");
                throw;
            }
        }

        public void OpenCredentialManager()
        {
            _logger.Information("Opening Credential Manager");
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "/name Microsoft.CredentialManager",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open Credential Manager");
                throw;
            }
        }

        public void OpenAdvancedUserManagement()
        {
            _logger.Information("Opening Advanced User Management (lusrmgr.msc)");
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "lusrmgr.msc",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open Advanced User Management");
                throw;
            }
        }

        public void OpenAddUserDialog()
        {
            _logger.Information("Opening Add User dialog (ms-settings:otherusers)");
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ms-settings:otherusers",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open Add User dialog");
                throw;
            }
        }

        public bool AddLocalUser(string userName, string password, string fullName, string description, bool isAdministrator)
        {
            _logger.Information("Adding local user: {UserName}", userName);

            if (string.IsNullOrWhiteSpace(userName))
            {
                _logger.Warning("Add user blocked - empty username");
                return false;
            }

            var policy = GetPasswordPolicy();
            if (!policy.IsPasswordValid(password, out string? error))
            {
                _logger.Warning("Add user blocked - password policy violation for {UserName}: {Error}", userName, error);
                return false;
            }

            try
            {
                using var context = new PrincipalContext(ContextType.Machine);

                var existing = UserPrincipal.FindByIdentity(context, userName);
                if (existing != null)
                {
                    _logger.Warning("Add user blocked - user already exists: {UserName}", userName);
                    return false;
                }

                var user = new UserPrincipal(context)
                {
                    SamAccountName = userName,
                    DisplayName = fullName,
                    Description = description,
                    Enabled = true
                };
                user.SetPassword(password);
                user.Save();

                if (isAdministrator)
                {
                    var adminGroup = GroupPrincipal.FindByIdentity(context, "Administrators")
                        ?? GroupPrincipal.FindByIdentity(context, "Administratorzy");
                    if (adminGroup != null)
                    {
                        adminGroup.Members.Add(user);
                        adminGroup.Save();
                        _logger.Information("Added user {UserName} to administrators group", userName);
                    }
                    else
                    {
                        _logger.Warning("Could not find administrators group for user {UserName}", userName);
                    }
                }

                _logger.Information("Local user added successfully: {UserName}", userName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add local user: {UserName}", userName);
                return false;
            }
        }

        public PasswordPolicy GetPasswordPolicy()
        {
            _logger.Debug("Reading local password policy via NetUserModalsGet");
            var policy = new PasswordPolicy
            {
                // Default safe values in case API fails
                MinimumPasswordLength = 0,
                MaximumPasswordAgeDays = 0,
                MinimumPasswordAgeDays = 0,
                PasswordHistoryLength = 0,
                PasswordComplexityRequired = false,
                ReversibleEncryptionEnabled = false,
                AccountLockoutThreshold = 0,
                AccountLockoutDurationMinutes = 0,
                ResetLockoutCounterAfterMinutes = 0
            };

            try
            {
                // Level 0: basic password params
                int result0 = NetUserModalsGet(null, 0, out IntPtr buffer0);
                if (result0 == 0 && buffer0 != IntPtr.Zero)
                {
                    var info0 = Marshal.PtrToStructure<USER_MODALS_INFO_0>(buffer0);
                    policy.MinimumPasswordLength = info0.min_passwd_len;
                    policy.MaximumPasswordAgeDays = info0.max_passwd_age == int.MaxValue ? 0 : info0.max_passwd_age / 86400;
                    policy.MinimumPasswordAgeDays = info0.min_passwd_age / 86400;
                    policy.PasswordHistoryLength = info0.password_hist_len;
                    NetApiBufferFree(buffer0);
                }
                else if (result0 != 0)
                {
                    _logger.Warning("NetUserModalsGet level 0 returned error code {Result}", result0);
                }

                // Level 3: lockout policy
                int result3 = NetUserModalsGet(null, 3, out IntPtr buffer3);
                if (result3 == 0 && buffer3 != IntPtr.Zero)
                {
                    var info3 = Marshal.PtrToStructure<USER_MODALS_INFO_3>(buffer3);
                    policy.AccountLockoutThreshold = info3.lockout_threshold;
                    policy.AccountLockoutDurationMinutes = info3.lockout_duration == int.MaxValue ? 0 : info3.lockout_duration / 60;
                    policy.ResetLockoutCounterAfterMinutes = info3.lockout_observation_window == int.MaxValue ? 0 : info3.lockout_observation_window / 60;
                    NetApiBufferFree(buffer3);
                }
                else if (result3 != 0)
                {
                    _logger.Warning("NetUserModalsGet level 3 returned error code {Result}", result3);
                }

                // Check complexity via registry (secedit/SceCli equivalent)
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Lsa");
                    var value = key?.GetValue("PasswordComplexity") as int?;
                    if (value.HasValue)
                    {
                        policy.PasswordComplexityRequired = value.Value != 0;
                    }
                    else
                    {
                        // Default: Windows Home typically has complexity OFF
                        policy.PasswordComplexityRequired = false;
                        _logger.Debug("PasswordComplexity registry value not found; defaulting to false (Windows Home default)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to read PasswordComplexity from registry; defaulting to false");
                    policy.PasswordComplexityRequired = false;
                }

                _logger.Information("Password policy read: min length {MinLength}, complexity {Complexity}, lockout threshold {Lockout}",
                    policy.MinimumPasswordLength, policy.PasswordComplexityRequired, policy.AccountLockoutThreshold);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read password policy");
            }

            return policy;
        }

        public bool SetPasswordPolicy(PasswordPolicy policy)
        {
            _logger.Information("Setting password policy...");
            try
            {
                var info0 = new USER_MODALS_INFO_0
                {
                    min_passwd_len = policy.MinimumPasswordLength,
                    max_passwd_age = policy.MaximumPasswordAgeDays <= 0 ? int.MaxValue : policy.MaximumPasswordAgeDays * 86400,
                    min_passwd_age = policy.MinimumPasswordAgeDays * 86400,
                    password_hist_len = policy.PasswordHistoryLength,
                    force_logoff = int.MaxValue // don't change
                };

                int result0 = NetUserModalsSet(null, 0, ref info0, out int _);
                if (result0 != 0)
                {
                    _logger.Warning("NetUserModalsSet level 0 returned error code {Result}", result0);
                    return false;
                }

                var info3 = new USER_MODALS_INFO_3
                {
                    lockout_threshold = policy.AccountLockoutThreshold,
                    lockout_duration = policy.AccountLockoutDurationMinutes <= 0 ? int.MaxValue : policy.AccountLockoutDurationMinutes * 60,
                    lockout_observation_window = policy.ResetLockoutCounterAfterMinutes <= 0 ? int.MaxValue : policy.ResetLockoutCounterAfterMinutes * 60
                };

                int result3 = NetUserModalsSet(null, 3, ref info3, out int _);
                if (result3 != 0)
                {
                    _logger.Warning("NetUserModalsSet level 3 returned error code {Result}", result3);
                    return false;
                }

                _logger.Information("Password policy updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to set password policy");
                return false;
            }
        }

        private UserAccount MapToUserAccount(UserPrincipal user, PrincipalContext context)
        {
            var account = new UserAccount
            {
                UserName = user.SamAccountName ?? string.Empty,
                FullName = user.DisplayName ?? string.Empty,
                Description = user.Description ?? string.Empty,
                Domain = Environment.MachineName,
                IsLocalAccount = true
            };

            try
            {
                var groups = user.GetGroups()
                    .OfType<GroupPrincipal>()
                    .Select(g => g.Name)
                    .Where(n => n != null)
                    .ToList();

                account.GroupsDisplay = string.Join("; ", groups);
                account.IsAdministrator = groups.Any(g =>
                    g?.Equals("Administrators", StringComparison.OrdinalIgnoreCase) == true ||
                    g?.Equals("Administratorzy", StringComparison.OrdinalIgnoreCase) == true);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get groups for user {UserName}", account.UserName);
                account.GroupsDisplay = "N/A";
            }

            try
            {
                if (user.GetUnderlyingObject() is DirectoryEntry entry)
                {
                    account.LastLogon = entry.Properties["LastLogin"]?.Value as DateTime?;
                    account.BadPasswordCount = entry.Properties["BadPasswordAttempts"]?.Value as int? ?? 0;
                    account.NumberOfLogons = entry.Properties["LogonCount"]?.Value as int? ?? 0;
                    account.HomeDirectory = entry.Properties["HomeDirectory"]?.Value as string ?? string.Empty;
                    account.ScriptPath = entry.Properties["ScriptPath"]?.Value as string ?? string.Empty;
                    account.ProfilePath = entry.Properties["Profile"]?.Value as string ?? string.Empty;

                    if (entry.Properties["PasswordAge"]?.Value is int ageSeconds)
                    {
                        account.PasswordAgeDays = ageSeconds / 86400;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to read extended properties for user {UserName}", account.UserName);
            }

            return account;
        }
    }
}
