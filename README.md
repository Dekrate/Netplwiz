# Netplwiz Modern

A modern, WinUI 3-based reimagining of the classic Windows `netplwiz` (User Accounts) control panel applet. Built with native Windows App SDK, Mica material, and MVVM architecture.

## Features

- **Modern UI** – Fluent Design with Mica Alt backdrop, rounded corners, and native WinUI 3 controls
- **User Management** – List local users, edit properties, reset passwords, delete accounts
- **Advanced Tools** – Launch Credential Manager and Local Users and Groups (lusrmgr.msc) directly
- **Secure Logon** – Toggle Ctrl+Alt+Delete requirement from within the app
- **Logging** – Structured logging with Serilog to `%LocalAppData%\Netplwiz\logs`
- **Administrator Elevation** – Runs with `requireAdministrator` via application manifest
- **Polish & English** – Localized UI strings (RESW) with an included Python sync script

## Requirements

- Windows 10 version 19041 (20H1) or later
- Windows 11 recommended for full Mica backdrop support
- .NET 10 SDK
- Visual Studio 2022 17.10+ (or VS Code with C# Dev Kit)

## Building

```powershell
dotnet build
```

## Running

Because the app modifies local user accounts and registry settings, it must run elevated:

```powershell
dotnet run
```

The `app.manifest` requests `requireAdministrator` automatically.

## Project Structure

```
Netplwiz/
├── Models/                 # UserAccount and domain models
├── Services/               # IUserService, UserService (DirectoryServices)
├── ViewModels/             # MainViewModel, UserPropertiesViewModel
├── Views/                  # MainPage, UserPropertiesDialog, ResetPasswordDialog, AboutDialog
├── Helpers/                # AppLogger, ObjectToVisibilityConverter
├── Strings/                # pl-PL and en-US RESW resources
├── scripts/                # Python translation sync script
└── Netplwiz.Tests/         # Unit tests (MSTest + Moq)
```

## Tests

```powershell
dotnet test
```

## Contributing

Contributions are welcome. Please open an issue or pull request on GitHub.

## Disclaimer

This software is provided "as is", without warranty of any kind. Use at your own risk.
Netplwiz Modern performs administrative operations on local user accounts, passwords, and system registry settings.
The authors and contributors are **not responsible** for any damage, data loss, locked accounts, or system misconfiguration resulting from the use of this application.
Always ensure you have backups and understand the consequences of user management actions before proceeding.

## License

MIT License – see [LICENSE.md](LICENSE.md).
