using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netplwiz.Helpers;
using Netplwiz.Models;
using Netplwiz.ViewModels;
using System;
using System.Threading.Tasks;

namespace Netplwiz.Views
{
    public partial class MainPage : Page
    {
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<MainPage>();

        public MainPage()
        {
            _logger.Information("MainPage initializing");
            this.InitializeComponent();

            // Subscribe to ViewModel events
            ViewModel.RequestUserProperties += OnRequestUserProperties;
            ViewModel.RequestRemoveUser += OnRequestRemoveUser;
            ViewModel.RequestResetPassword += OnRequestResetPassword;
            ViewModel.RequestAddLocalUser += OnRequestAddLocalUser;
            ViewModel.RequestUserDetails += OnRequestUserDetails;
            ViewModel.RequestEditPasswordPolicy += OnRequestEditPasswordPolicy;

            // Initialize navigation
            MainNavigation.SelectedItem = UsersNavItem;
            UpdateContent(UsersNavItem);

            _logger.Information("MainPage initialized");
        }

        private void MainNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                UpdateContent(item);
            }
        }

        private void UpdateContent(NavigationViewItem item)
        {
            var vm = ViewModel;
            if (item == UsersNavItem)
            {
                var view = new UsersView { DataContext = vm };
                view.RequestAddLocalUser += (_, _) => OnRequestAddLocalUser(this, EventArgs.Empty);
                view.RequestAddViaSettings += (_, _) => vm.OpenAddUserSettingsCommand.Execute(null);
                MainNavigation.Content = view;
            }
            else if (item == AdvancedNavItem)
            {
                MainNavigation.Content = new AdvancedView { DataContext = vm };
            }
        }

        private async void OnRequestAddLocalUser(object? sender, EventArgs e)
        {
            _logger.Information("Showing add local user dialog");
            var policy = ViewModel.GetPasswordPolicy();
            var dialog = new AddUserDialog(policy);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _logger.Information("Adding local user {UserName}", dialog.NewUserName);
                var success = ViewModel.CreateLocalUser(dialog.NewUserName, dialog.NewPassword, dialog.NewFullName, dialog.NewDescription, dialog.IsAdministrator);
                if (success)
                {
                    _logger.Information("Local user {UserName} added successfully", dialog.NewUserName);
                    ViewModel.StatusMessage = $"Użytkownik {dialog.NewUserName} został dodany";
                    await ViewModel.LoadUsersAsync();
                }
                else
                {
                    _logger.Warning("Failed to add local user {UserName}", dialog.NewUserName);
                    ViewModel.StatusMessage = "Nie udało się dodać użytkownika";
                }
            }
        }

        private MainViewModel ViewModel => (MainViewModel)(this.DataContext ?? throw new InvalidOperationException("DataContext is null"));

        private async void OnRequestUserProperties(object? sender, UserAccount user)
        {
            _logger.Information("Showing properties dialog for {UserName}", user.UserName);
            var dialog = new UserPropertiesDialog(user);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _logger.Information("Properties saved for {UserName}", user.UserName);
                await ViewModel.LoadUsersAsync();
            }
        }

        private async void OnRequestRemoveUser(object? sender, UserAccount user)
        {
            _logger.Information("Showing remove user confirmation for {UserName}", user.UserName);

            var dialog = new ContentDialog
            {
                Title = "Konta użytkowników",
                Content = $"Została wybrana opcja usunięcia użytkownika {user.Domain}\\{user.UserName} z listy użytkowników tego komputera. Użytkownik {user.Domain}\\{user.UserName} nie będzie mógł już korzystać z tego komputera.\n\nCzy na pewno chcesz usunąć użytkownika {user.Domain}\\{user.UserName}?",
                PrimaryButtonText = "Tak",
                CloseButtonText = "Nie",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _logger.Information("Confirming deletion of user {UserName}", user.UserName);
                var success = ViewModel.DeleteUser(user.UserName);
                if (success)
                {
                    _logger.Information("User {UserName} deleted successfully", user.UserName);
                    await ViewModel.LoadUsersAsync();
                }
                else
                {
                    _logger.Warning("Failed to delete user {UserName}", user.UserName);
                }
            }
        }

        private async void OnRequestUserDetails(object? sender, UserAccount user)
        {
            _logger.Information("Showing user details dialog for {UserName}", user.UserName);
            var dialog = new UserDetailsDialog(user);
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }

        private async void OnRequestEditPasswordPolicy(object? sender, EventArgs e)
        {
            _logger.Information("Showing edit password policy dialog");
            var policy = ViewModel.GetPasswordPolicy();
            var dialog = new AccountPolicyDialog(policy);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newPolicy = dialog.GetPolicy();
                _logger.Information("Saving password policy");
                var success = ViewModel.SetPasswordPolicy(newPolicy);
                if (success)
                {
                    _logger.Information("Password policy updated successfully");
                    ViewModel.StatusMessage = "Polityka haseł została zaktualizowana";
                }
                else
                {
                    _logger.Warning("Failed to update password policy");
                    ViewModel.StatusMessage = "Nie udało się zaktualizować polityki haseł (wymagane uprawnienia administratora)";
                }
            }
        }

        private async void OnRequestResetPassword(object? sender, UserAccount user)
        {
            _logger.Information("Showing reset password dialog for {UserName}", user.UserName);
            var policy = ViewModel.GetPasswordPolicy();
            var dialog = new ResetPasswordDialog(user.UserName, policy);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newPassword = dialog.NewPassword;
                _logger.Information("Resetting password for {UserName}", user.UserName);
                var success = ViewModel.SetUserPassword(user.UserName, newPassword);
                if (success)
                {
                    _logger.Information("Password reset successfully for {UserName}", user.UserName);
                    ViewModel.StatusMessage = "Hasło zostało zresetowane";
                }
                else
                {
                    _logger.Warning("Failed to reset password for {UserName}", user.UserName);
                    ViewModel.StatusMessage = "Nie udało się zresetować hasła";
                }
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("Showing Settings dialog");
            var dialog = new SettingsDialog();
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                App.SetTheme(dialog.SelectedTheme);
                App.SetBackdrop(dialog.SelectedBackdrop);
                _logger.Information("Applied theme {Theme} and backdrop {Backdrop}", dialog.SelectedTheme, dialog.SelectedBackdrop);
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.Information("Showing About dialog");
            var dialog = new AboutDialog();
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();
        }
    }
}
