using Microsoft.UI.Xaml.Controls;
using Netplwiz.Models;
using System;

namespace Netplwiz.Views
{
    public sealed partial class UserDetailsDialog : ContentDialog
    {
        public UserDetailsDialog(UserAccount user)
        {
            this.InitializeComponent();
            BindUser(user);
        }

        private void BindUser(UserAccount user)
        {
            UserNameText.Text = user.UserName;
            FullNameText.Text = string.IsNullOrEmpty(user.FullName) ? "—" : user.FullName;
            DescriptionText.Text = string.IsNullOrEmpty(user.Description) ? "—" : user.Description;
            DomainText.Text = user.Domain;
            GroupsText.Text = string.IsNullOrEmpty(user.GroupsDisplay) ? "—" : user.GroupsDisplay;
            IsAdminText.Text = user.IsAdministrator ? "Tak" : "Nie";
            IsLocalText.Text = user.IsLocalAccount ? "Tak" : "Nie";
            PasswordRequiredText.Text = user.PasswordRequired ? "Tak" : "Nie";
            PasswordNeverExpiresText.Text = user.PasswordNeverExpires ? "Tak" : "Nie";
            AccountDisabledText.Text = user.AccountDisabled ? "Tak" : "Nie";
            AccountLockedOutText.Text = user.AccountLockedOut ? "Tak" : "Nie";
            LastLogonText.Text = user.LastLogon.HasValue ? user.LastLogon.Value.ToString("g") : "—";
            BadPasswordCountText.Text = user.BadPasswordCount.ToString();
            NumberOfLogonsText.Text = user.NumberOfLogons.ToString();
            PasswordAgeText.Text = user.PasswordAgeDays > 0 ? $"{user.PasswordAgeDays} dni" : "—";
            HomeDirectoryText.Text = string.IsNullOrEmpty(user.HomeDirectory) ? "—" : user.HomeDirectory;
            ScriptPathText.Text = string.IsNullOrEmpty(user.ScriptPath) ? "—" : user.ScriptPath;
            ProfilePathText.Text = string.IsNullOrEmpty(user.ProfilePath) ? "—" : user.ProfilePath;
        }
    }
}
