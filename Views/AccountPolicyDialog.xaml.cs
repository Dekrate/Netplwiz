using Microsoft.UI.Xaml.Controls;
using Netplwiz.Models;

namespace Netplwiz.Views
{
    public sealed partial class AccountPolicyDialog : ContentDialog
    {
        public AccountPolicyDialog(PasswordPolicy policy)
        {
            this.InitializeComponent();
            MinLengthBox.Value = policy.MinimumPasswordLength;
            MaxAgeBox.Value = policy.MaximumPasswordAgeDays;
            MinAgeBox.Value = policy.MinimumPasswordAgeDays;
            HistoryBox.Value = policy.PasswordHistoryLength;
            ComplexityCheck.IsChecked = policy.PasswordComplexityRequired;
            LockoutThresholdBox.Value = policy.AccountLockoutThreshold;
            LockoutDurationBox.Value = policy.AccountLockoutDurationMinutes;
        }

        public PasswordPolicy GetPolicy()
        {
            return new PasswordPolicy
            {
                MinimumPasswordLength = (int)MinLengthBox.Value,
                MaximumPasswordAgeDays = (int)MaxAgeBox.Value,
                MinimumPasswordAgeDays = (int)MinAgeBox.Value,
                PasswordHistoryLength = (int)HistoryBox.Value,
                PasswordComplexityRequired = ComplexityCheck.IsChecked == true,
                AccountLockoutThreshold = (int)LockoutThresholdBox.Value,
                AccountLockoutDurationMinutes = (int)LockoutDurationBox.Value,
                ResetLockoutCounterAfterMinutes = (int)LockoutDurationBox.Value
            };
        }
    }
}
