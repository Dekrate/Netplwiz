namespace Netplwiz.Models
{
    public class PasswordPolicy
    {
        public int MinimumPasswordLength { get; set; }
        public int MaximumPasswordAgeDays { get; set; }
        public int MinimumPasswordAgeDays { get; set; }
        public int PasswordHistoryLength { get; set; }
        public bool PasswordComplexityRequired { get; set; }
        public bool ReversibleEncryptionEnabled { get; set; }
        public int AccountLockoutThreshold { get; set; }
        public int AccountLockoutDurationMinutes { get; set; }
        public int ResetLockoutCounterAfterMinutes { get; set; }

        public bool IsPasswordValid(string password, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Hasło jest wymagane.";
                return false;
            }

            if (password.Length < MinimumPasswordLength)
            {
                errorMessage = $"Hasło musi mieć co najmniej {MinimumPasswordLength} znaków.";
                return false;
            }

            if (PasswordComplexityRequired && !MeetsComplexity(password))
            {
                errorMessage = "Hasło musi spełniać wymagania złożoności: wielka litera, mała litera, cyfra lub znak specjalny (min. 3 kategorie).";
                return false;
            }

            return true;
        }

        private static bool MeetsComplexity(string password)
        {
            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            int categories = 0;
            if (hasUpper) categories++;
            if (hasLower) categories++;
            if (hasDigit) categories++;
            if (hasSpecial) categories++;

            return categories >= 3;
        }

        public string GetPolicyDescription()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (MinimumPasswordLength > 0)
                parts.Add($"min. {MinimumPasswordLength} znaków");
            if (PasswordComplexityRequired)
                parts.Add("wymagana złożoność");
            if (MaximumPasswordAgeDays > 0)
                parts.Add($"ważność max. {MaximumPasswordAgeDays} dni");
            if (AccountLockoutThreshold > 0)
                parts.Add($"blokada po {AccountLockoutThreshold} próbach");

            return parts.Count > 0 ? string.Join(", ", parts) : "Brak ograniczeń";
        }
    }
}
