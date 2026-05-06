namespace Netplwiz.Models
{
    public class UserAccount
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string GroupsDisplay { get; set; } = string.Empty;
        public bool IsAdministrator { get; set; }
        public bool IsLocalAccount { get; set; } = true;
        public bool PasswordRequired { get; set; } = true;
        public bool PasswordChangeable { get; set; } = true;
        public bool PasswordNeverExpires { get; set; }
        public bool AccountDisabled { get; set; }
        public bool AccountLockedOut { get; set; }

        // Extended properties populated via DirectoryEntry
        public System.DateTime? LastLogon { get; set; }
        public int BadPasswordCount { get; set; }
        public int NumberOfLogons { get; set; }
        public string HomeDirectory { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public int PasswordAgeDays { get; set; }
    }
}
