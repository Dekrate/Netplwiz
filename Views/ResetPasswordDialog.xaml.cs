using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netplwiz.Helpers;
using System;

namespace Netplwiz.Views
{
    public partial class ResetPasswordDialog : ContentDialog
    {
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<ResetPasswordDialog>();
        private readonly string _userName;

        public string NewPassword { get; private set; } = string.Empty;

        public ResetPasswordDialog(string userName)
        {
            _userName = userName;
            _logger.Information("ResetPasswordDialog initializing for user: {UserName}", userName);
            this.InitializeComponent();
            IsPrimaryButtonEnabled = false;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            var newPass = NewPasswordBox.Password;
            var confirmPass = ConfirmPasswordBox.Password;

            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(newPass) && newPass == confirmPass;
            NewPassword = IsPrimaryButtonEnabled ? newPass : string.Empty;

            _logger.Debug("Password changed - match: {Match}", IsPrimaryButtonEnabled);
        }
    }
}
