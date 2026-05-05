using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Netplwiz.Helpers;
using System;
using Windows.Storage;
using Windows.UI;

namespace Netplwiz
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;

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
            _logger.Information("OnLaunched started");

            window = new Window();

            // Set Mica Alt backdrop by default, then load saved settings
            SetBackdrop(BackdropType.MicaAlt);
            LoadSettings();

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
            _logger.Information("MainPage navigated, activating window");
            window.Activate();

            _logger.Information("Application launched with {Backdrop} backdrop", CurrentBackdrop);
        }

        public static BackdropType CurrentBackdrop { get; private set; } = BackdropType.MicaAlt;
        public static ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

        public static void SetBackdrop(BackdropType type)
        {
            var appWindow = ((App)Current).window;
            if (appWindow == null)
            {
                CurrentBackdrop = type;
                return;
            }

            CurrentBackdrop = type;
            appWindow.SystemBackdrop = type switch
            {
                BackdropType.Mica => new MicaBackdrop { Kind = MicaKind.Base },
                BackdropType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropType.Transparent => null,
                _ => new MicaBackdrop { Kind = MicaKind.BaseAlt }
            };

            SaveSetting("Backdrop", type.ToString());
        }

        public static void SetTheme(ElementTheme theme)
        {
            if (Current is App app && app.window?.Content is FrameworkElement root)
            {
                CurrentTheme = theme;
                root.RequestedTheme = theme;
                SaveSetting("Theme", theme.ToString());
            }
        }

        public static void LoadSettings()
        {
            if (LoadSetting("Theme") is string themeStr &&
                Enum.TryParse<ElementTheme>(themeStr, out var theme))
            {
                SetTheme(theme);
            }

            if (LoadSetting("Backdrop") is string backdropStr &&
                Enum.TryParse<BackdropType>(backdropStr, out var backdrop))
            {
                SetBackdrop(backdrop);
            }
        }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Netplwiz", "settings.json");

        private static void SaveSetting(string key, string value)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var settings = File.Exists(SettingsPath)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsPath)) ?? new()
                    : new Dictionary<string, string>();
                settings[key] = value;
                File.WriteAllText(SettingsPath, System.Text.Json.JsonSerializer.Serialize(settings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save setting: {ex.Message}");
            }
        }

        private static string? LoadSetting(string key)
        {
            try
            {
                if (!File.Exists(SettingsPath)) return null;
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsPath));
                return settings?.TryGetValue(key, out var value) == true ? value : null;
            }
            catch
            {
                return null;
            }
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
