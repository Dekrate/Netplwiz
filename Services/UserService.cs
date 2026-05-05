using Netplwiz.Helpers;
using Netplwiz.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
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
    }

    public class UserService : IUserService
    {
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

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                _logger.Warning("Password change blocked - empty password for: {UserName}", userName);
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

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.Warning("Add user blocked - empty password for: {UserName}", userName);
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

            return account;
        }
    }
}
