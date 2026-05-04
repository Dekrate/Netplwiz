using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netplwiz.Helpers;
using Netplwiz.ViewModels;
using System;

namespace Netplwiz.Views
{
    public partial class MainPage : Page
    {
        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<MainPage>();

        public MainPage()
        {
            _logger.Information("MainPage initializing");
            this.InitializeComponent();
            _logger.Information("MainPage initialized");
        }

        private MainViewModel ViewModel => (MainViewModel)(this.DataContext ?? throw new InvalidOperationException("DataContext is null"));

        private void SecureLogonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _logger.Debug("SecureLogon checkbox checked");
            ViewModel.ToggleSecureLogonCommand.Execute(null);
        }

        private void SecureLogonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _logger.Debug("SecureLogon checkbox unchecked");
            ViewModel.ToggleSecureLogonCommand.Execute(null);
        }
    }
}
