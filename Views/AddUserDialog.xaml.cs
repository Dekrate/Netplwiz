using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netplwiz.Models;

namespace Netplwiz.Views
{
    public sealed partial class AddUserDialog : ContentDialog
    {
        public string NewUserName => UserNameTextBox.Text.Trim();
        public string NewFullName => FullNameTextBox.Text.Trim();
        public string NewPassword => PasswordBox.Password;
        public string NewDescription => DescriptionTextBox.Text.Trim();
        public bool IsAdministrator => AdminCheckBox.IsChecked ?? false;

        public PasswordPolicy? PasswordPolicy { get; set; }

        public AddUserDialog(PasswordPolicy? policy = null)
        {
            this.InitializeComponent();
            PasswordPolicy = policy;
            if (policy != null)
            {
                PolicyHintTextBlock.Text = "Wymagania hasła: " + policy.GetPolicyDescription();
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                ShowError("Nazwa użytkownika jest wymagana.");
                args.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowError("Hasło jest wymagane.");
                args.Cancel = true;
                return;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowError("Hasła nie są zgodne.");
                args.Cancel = true;
                return;
            }

            if (PasswordPolicy != null && !PasswordPolicy.IsPasswordValid(PasswordBox.Password, out string? error))
            {
                ShowError(error ?? "Hasło nie spełnia wymagań polityki.");
                args.Cancel = true;
                return;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
