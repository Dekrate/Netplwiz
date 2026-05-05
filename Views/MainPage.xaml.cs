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

            _logger.Information("MainPage initialized");
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
                var success = await Task.Run(() => ViewModel.DeleteUser(user.UserName));
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

        private async void OnRequestResetPassword(object? sender, UserAccount user)
        {
            _logger.Information("Showing reset password dialog for {UserName}", user.UserName);
            var dialog = new ResetPasswordDialog(user.UserName);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newPassword = dialog.NewPassword;
                _logger.Information("Resetting password for {UserName}", user.UserName);
                var success = await Task.Run(() => ViewModel.SetUserPassword(user.UserName, newPassword));
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
