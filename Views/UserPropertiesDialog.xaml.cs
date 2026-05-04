using Microsoft.UI.Xaml.Controls;
using Netplwiz.Helpers;
using Netplwiz.Models;
using Netplwiz.Services;
using Netplwiz.ViewModels;
using System;
using System.Threading.Tasks;

namespace Netplwiz.Views
{
    public partial class UserPropertiesDialog : ContentDialog
    {
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<UserPropertiesDialog>();

        public UserPropertiesDialog(UserAccount user)
        {
            _logger.Information("UserPropertiesDialog initializing for user: {UserName}", user.UserName);
            this.InitializeComponent();
            this.DataContext = new UserPropertiesViewModel(user, new UserService());
            _logger.Information("UserPropertiesDialog initialized");
        }

        public static async Task ShowAsync(UserAccount user)
        {
            var dialog = new UserPropertiesDialog(user);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var vm = (UserPropertiesViewModel)dialog.DataContext;
                await vm.SaveAsync();
            }
        }
    }
}
