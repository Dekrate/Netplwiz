using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Netplwiz.Helpers;
using Netplwiz.Models;
using Netplwiz.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netplwiz.ViewModels
{
    public partial class UserPropertiesViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly string _originalUserName;
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<UserPropertiesViewModel>();

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isStandardUser = true;

        [ObservableProperty]
        private bool _isAdministrator;

        [ObservableProperty]
        private bool _isOtherGroup;

        [ObservableProperty]
        private string _selectedGroup = string.Empty;

        [ObservableProperty]
        private List<string> _availableGroups = new();

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public UserPropertiesViewModel(UserAccount user, IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _originalUserName = user.UserName;

            UserName = user.UserName;
            FullName = user.FullName;
            Description = user.Description;

            _logger.Information("UserPropertiesViewModel created for user: {UserName}", user.UserName);
            LoadGroupsAsync(user);
        }

        private async void LoadGroupsAsync(UserAccount user)
        {
            try
            {
                var groups = await Task.Run(() =>
                {
                    using var context = new System.DirectoryServices.AccountManagement.PrincipalContext(
                        System.DirectoryServices.AccountManagement.ContextType.Machine);
                    var groupPrincipal = new System.DirectoryServices.AccountManagement.GroupPrincipal(context);
                    var searcher = new System.DirectoryServices.AccountManagement.PrincipalSearcher(groupPrincipal);
                    return searcher.FindAll()
                        .OfType<System.DirectoryServices.AccountManagement.GroupPrincipal>()
                        .Select(g => g.Name)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .OrderBy(n => n)
                        .ToList();
                });

                AvailableGroups = groups;
                _logger.Information("Loaded {Count} groups", groups.Count);

                if (user.IsAdministrator)
                {
                    IsAdministrator = true;
                    IsStandardUser = false;
                    IsOtherGroup = false;
                }
                else
                {
                    IsStandardUser = true;
                    IsAdministrator = false;
                    IsOtherGroup = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load groups");
                StatusMessage = $"Błąd ładowania grup: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            _logger.Information("Saving user: {UserName}", _originalUserName);
            IsSaving = true;
            StatusMessage = "Zapisywanie...";

            try
            {
                var updated = new UserAccount
                {
                    UserName = _originalUserName,
                    FullName = FullName,
                    Description = Description
                };

                var success = await Task.Run(() => _userService.UpdateUser(updated));
                if (success)
                {
                    StatusMessage = "Zapisano pomyślnie";
                    _logger.Information("User saved successfully");
                }
                else
                {
                    StatusMessage = "Nie udało się zapisać";
                    _logger.Warning("Failed to save user");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving user");
                StatusMessage = $"Błąd: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        partial void OnIsStandardUserChanged(bool value)
        {
            if (value)
            {
                IsAdministrator = false;
                IsOtherGroup = false;
            }
        }

        partial void OnIsAdministratorChanged(bool value)
        {
            if (value)
            {
                IsStandardUser = false;
                IsOtherGroup = false;
            }
        }

        partial void OnIsOtherGroupChanged(bool value)
        {
            if (value)
            {
                IsStandardUser = false;
                IsAdministrator = false;
            }
        }
    }
}
