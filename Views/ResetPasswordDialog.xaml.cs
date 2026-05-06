using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netplwiz.Helpers;
using Netplwiz.Models;
using System;

namespace Netplwiz.Views
{
    public partial class ResetPasswordDialog : ContentDialog
    {
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<ResetPasswordDialog>();
        private readonly string _userName;

        public string NewPassword { get; private set; } = string.Empty;
        public PasswordPolicy? PasswordPolicy { get; set; }

        public ResetPasswordDialog(string userName, PasswordPolicy? policy = null)
        {
            _userName = userName;
            PasswordPolicy = policy;
            _logger.Information("ResetPasswordDialog initializing for user: {UserName}", userName);
            this.InitializeComponent();
            IsPrimaryButtonEnabled = false;

            if (policy != null)
            {
                PolicyHintTextBlock.Text = "Wymagania hasła: " + policy.GetPolicyDescription();
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            var newPass = NewPasswordBox.Password;
            var confirmPass = ConfirmPasswordBox.Password;

            bool match = !string.IsNullOrWhiteSpace(newPass) && newPass == confirmPass;
            bool policyValid = true;
            string? policyError = null;

            if (PasswordPolicy != null && match)
            {
                policyValid = PasswordPolicy.IsPasswordValid(newPass, out policyError);
            }

            IsPrimaryButtonEnabled = match && policyValid;
            NewPassword = IsPrimaryButtonEnabled ? newPass : string.Empty;

            if (!policyValid && policyError != null)
            {
                ErrorTextBlock.Text = policyError;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
            }

            _logger.Debug("Password changed - match: {Match}, policyValid: {PolicyValid}", match, policyValid);
        }
    }
}
