using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Netplwiz.Helpers;
using Netplwiz.Models;
using Netplwiz.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Netplwiz.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<MainViewModel>();

        [ObservableProperty]
        private ObservableCollection<UserAccount> _users = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveUserCommand), nameof(PropertiesCommand), nameof(ResetPasswordCommand))]
        private UserAccount? _selectedUser;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _secureLogonRequired;

        [ObservableProperty]
        private int _selectedTabIndex;

        public MainViewModel() : this(new UserService()) { }

        public MainViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger.Information("MainViewModel created");
            _ = LoadUsersAsync();
            SecureLogonRequired = _userService.IsSecureLogonRequired();
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            _logger.Information("Loading users...");
            IsLoading = true;
            StatusMessage = "Ładowanie użytkowników...";

            try
            {
                var users = await Task.Run(() => _userService.GetLocalUsers());
                Users.Clear();
                foreach (var user in users.OrderBy(u => u.UserName))
                {
                    Users.Add(user);
                }
                StatusMessage = $"Załadowano {users.Count} użytkowników";
                _logger.Information("Users loaded: {Count}", users.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd ładowania: {ex.Message}";
                _logger.Error(ex, "Failed to load users");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddUser()
        {
            _logger.Information("Add user requested");
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
                _logger.Error(ex, "Failed to open add user dialog");
                StatusMessage = $"Błąd: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteUserAction))]
        private void RemoveUser()
        {
            if (SelectedUser == null) return;
            _logger.Information("Remove user requested: {UserName}", SelectedUser.UserName);
            // Dialog will be shown by view
        }

        [RelayCommand(CanExecute = nameof(CanExecuteUserAction))]
        private void Properties()
        {
            if (SelectedUser == null) return;
            _logger.Information("Properties requested for: {UserName}", SelectedUser.UserName);
            // Navigation to properties dialog will be handled by view
        }

        [RelayCommand(CanExecute = nameof(CanExecuteUserAction))]
        private void ResetPassword()
        {
            if (SelectedUser == null) return;
            _logger.Information("Reset password requested for: {UserName}", SelectedUser.UserName);
            // Dialog will be shown by view
        }

        [RelayCommand]
        private void ManagePasswords()
        {
            _logger.Information("Manage passwords requested");
            try
            {
                _userService.OpenCredentialManager();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open credential manager");
                StatusMessage = $"Błąd: {ex.Message}";
            }
        }

        [RelayCommand]
        private void AdvancedUserManagement()
        {
            _logger.Information("Advanced user management requested");
            try
            {
                _userService.OpenAdvancedUserManagement();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open advanced user management");
                StatusMessage = $"Błąd: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ToggleSecureLogon()
        {
            _logger.Information("Secure logon toggled to: {Value}", SecureLogonRequired);
            try
            {
                _userService.SetSecureLogonRequired(SecureLogonRequired);
                StatusMessage = SecureLogonRequired
                    ? "Wymagane naciśnięcie Ctrl+Alt+Del"
                    : "Naciśnięcie Ctrl+Alt+Del nie jest wymagane";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to toggle secure logon");
                StatusMessage = $"Błąd: {ex.Message}";
                SecureLogonRequired = !SecureLogonRequired;
            }
        }

        private bool CanExecuteUserAction() => SelectedUser != null;
    }
}
