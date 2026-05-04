using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netplwiz.Helpers;
using System;
using Windows.UI;

namespace Netplwiz
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window window = Window.Current;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AppLogger.Initialize();
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();

            // Set Mica Alt backdrop by default
            SetBackdrop(BackdropType.MicaAlt);

            // Extend content into title bar for Mica effect
            window.ExtendsContentIntoTitleBar = true;
            if (window.AppWindow != null)
            {
                var titleBar = window.AppWindow.TitleBar;
                titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(50, 255, 255, 255);
            }

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(Views.MainPage), e.Arguments);
            window.Activate();

            _logger.Information("Application launched with {Backdrop} backdrop", CurrentBackdrop);
        }

        public static BackdropType CurrentBackdrop { get; private set; } = BackdropType.MicaAlt;

        public static void SetBackdrop(BackdropType type)
        {
            var appWindow = ((App)Current).window;
            if (appWindow == null) return;

            CurrentBackdrop = type;
            appWindow.SystemBackdrop = type switch
            {
                BackdropType.Mica => new MicaBackdrop(),
                BackdropType.MicaAlt => new MicaBackdrop(),
                BackdropType.Transparent => null,
                _ => new MicaBackdrop()
            };
        }

        private readonly Serilog.ILogger _logger = AppLogger.Logger.ForContext<App>();

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }

    public enum BackdropType
    {
        Mica,
        MicaAlt,
        Transparent
    }
}
