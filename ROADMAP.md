# Netplwiz Roadmap

## Completed

### UI Modernization
- [x] Replace TabView with NavigationView (PaneDisplayMode=Top) in MainPage
- [x] Split MainPage into UsersView and AdvancedView UserControls
- [x] Replace TabView with NavigationView in UserPropertiesDialog
- [x] Split UserPropertiesDialog into General and Groups views
- [x] Add rounded corners and consistent button styles

### User Management Features
- [x] Add local user creation dialog (AddUserDialog)
- [x] DropDownButton with MenuFlyout for Add user options
- [x] AddLocalUser via System.DirectoryServices.AccountManagement
- [x] Admin checkbox for adding users to Administrators group

### Quality & Reliability
- [x] Remove Task.Run from COM/STA-bound operations (deadlock fix)
- [x] Add SelfContained + BuiltInComInteropSupport for self-contained builds
- [x] Add try-catch in CreateLocalUser for graceful error handling
- [x] Comprehensive unit tests (40 tests, all passing)
- [x] E2E command flow tests with mocked services
- [x] Security tests for protected accounts

---

## Planned Enhancements

### High Priority

#### 1. Extended User Properties Dialog
**Goal**: Display and edit all available user attributes from `UserPrincipal`.

**Fields to add** (read-only where not editable on Home):
- `AccountExpirationDate` — DateTimePicker for account expiry
- `LastLogon` / `LastPasswordSet` — Read-only statistics
- `BadLogonCount` — Read-only security metric
- `PasswordNeverExpires` — ToggleSwitch
- `UserCannotChangePassword` — ToggleSwitch
- `AccountLockoutTime` / `IsAccountLockedOut` — Status + Unlock button
- `HomeDirectory` / `HomeDrive` — Text inputs (Pro/Enterprise full support)
- `ScriptPath` — Text input for logon script
- `PermittedWorkstations` — Comma-separated list (Pro/Enterprise)
- `SmartcardLogonRequired` — ToggleSwitch

**Best Practices**:
- Disable fields that are unsupported on Home edition (detect via `Registry` or `Environment.OSVersion`)
- Show InfoBadge/Tooltip explaining why a field is disabled
- Use `ReadOnly` visual state for computed fields

#### 2. Password Policy Display
**Goal**: Show current system password policy in AdvancedView.

**Data to show**:
- Minimum password length
- Password complexity requirements
- Maximum password age
- Password history count
- Account lockout threshold / duration

**Implementation**:
- P/Invoke to `NetUserModalsGet` (level 0 or 3)
- Cache results, refresh on view load
- Show warning badge if policy is weak

#### 3. Group Management
**Goal**: Full CRUD for local groups beyond just membership.

**Features**:
- List all local groups with member count
- Create new local group
- Delete empty local groups
- Add/remove members from group detail view
- Show group SID and description

**Best Practices**:
- Use `GroupPrincipal` from `System.DirectoryServices.AccountManagement`
- Confirmation dialog for destructive operations
- Block deletion of built-in groups (Administrators, Users, etc.)

#### 4. Logon Hours Editor
**Goal**: Visual editor for `PermittedLogonTimes` bitmap.

**UI**:
- Grid with days (rows) and hours (columns)
- Click/drag to select allowed time blocks
- "Allow all" / "Deny all" shortcuts
- Visual preview of effective policy

**Best Practices**:
- Display warning on Home edition that policy is not enforced
- Store as 21-byte bitmap (same as Win32 `NetUserSetInfo`)
- Validate at least one hour is allowed

---

### Medium Priority

#### 5. User Rights Assignment Viewer
**Goal**: Display Local Security Policy user rights per account.

**Data**:
- Log on as a service
- Deny log on locally
- Change system time
- Shut down the system
- etc.

**Implementation**:
- P/Invoke `LsaEnumerateAccountRights` / `LsaAddAccountRights`
- Read-only view first, editing behind confirmation
- Requires elevation — detect and prompt

#### 6. Audit Policy Integration
**Goal**: Show audit settings for logon events.

**Features**:
- Audit logon successes/failures status
- Last N audit events (Event Log)
- Export audit log to CSV

**Implementation**:
- `System.Diagnostics.Eventing.Reader.EventLogReader`
- Filter by Security log, Event IDs 4624/4625/4648

#### 7. Profile & Home Directory Management
**Goal**: Manage roaming profiles and home folders.

**Fields**:
- Profile path (UNC or local)
- Home directory (local path or mapped drive)
- Logon script path

**Validation**:
- Test path existence
- UNC path format validation
- Warn if network path is unreachable

---

### Low Priority / Nice to Have

#### 8. Bulk Operations
**Goal**: Select multiple users and perform batch actions.

**Features**:
- Multi-select ListView
- Bulk delete (with confirmation)
- Bulk disable/enable
- Bulk password reset (same password + force change)
- Export user list to CSV/JSON

#### 9. Import Users
**Goal**: Create users from CSV/JSON file.

**Format**:
```csv
UserName,FullName,Password,Description,IsAdmin,Groups
testuser,Test User,Pass123!,Dev account,true,Users;Developers
```

**Validation**:
- Duplicate user detection
- Password policy compliance check
- Rollback on partial failure

#### 10. Theme & Accessibility Polish
**Goal**: Professional UI parity with Windows Settings.

**Tasks**:
- High contrast theme support
- Keyboard navigation (Tab order, accelerators)
- Screen reader labels on all interactive elements
- Minimum touch target size (44x44px)
- Reduced motion support

#### 11. Background Health Checks
**Goal**: Proactive warnings about security misconfigurations.

**Checks**:
- Administrator account without password
- Guest account enabled
- Users with PasswordNeverExpires + weak password
- Accounts inactive for > 90 days
- Non-admin users in Administrators group

**UI**:
- Badge on AdvancedView icon when issues found
- Expandable list with "Fix" action buttons
- Log all findings to Serilog

---

## Architecture Decisions

### COM Threading Model
- **Rule**: Never use `Task.Run` for `DirectoryServices` operations
- **Why**: COM requires STA; thread pool is MTA → deadlock
- **Fix**: Call synchronously on UI thread or use `STAThread` with `Thread` (not `Task`)

### Elevation Strategy
- **Rule**: Request elevation only when needed, not at launch
- **Implementation**: Use `ShellExecute` with `runas` verb for subprocess
- **UX**: Show shield icon on actions requiring elevation; gray out if not elevated

### Testing Strategy
- **Unit Tests**: Mock `IUserService`, test ViewModel logic in isolation
- **Integration Tests**: Real `UserService` against test local accounts (CI caveat)
- **E2E Tests**: Full command flow with mocked services
- **Rule**: Every user-facing feature must have ≥ 3 test cases (happy path, error, edge)

### Error Handling
- **Rule**: Catch at service layer, log with Serilog, return `bool` to ViewModel
- **UI**: Show `InfoBar` or `TeachingTip` for transient errors; dialog for blocking errors
- **Never**: Swallow exceptions silently; always log full stack trace

### Security Boundaries
- **Protected Accounts**: Administrator, Guest, SYSTEM accounts cannot be deleted
- **Validation**: Check `IsProtectedAccount` before delete/set-password
- **Audit**: Log all destructive operations with timestamp and user context

---

## Notes

- Home edition limitation: Some policies (logon hours, workstation restrictions) can be set via API but are not enforced by the OS in workgroup mode. UI should indicate this.
- Self-contained build: Required for running without .NET runtime installed. Trimming is enabled in Release; COM interop support must be explicit.
- Localization: Current PL/EN. All new UI strings must be added to `Resources.resw` files before commit.
