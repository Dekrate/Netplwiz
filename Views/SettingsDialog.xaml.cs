using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Netplwiz.Views
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Theme
            var currentTheme = App.CurrentTheme;
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag is string tag && tag == currentTheme.ToString())
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
            if (ThemeComboBox.SelectedItem == null)
                ThemeComboBox.SelectedIndex = 2; // Auto

            // Backdrop
            var currentBackdrop = App.CurrentBackdrop;
            foreach (ComboBoxItem item in BackdropComboBox.Items)
            {
                if (item.Tag is string tag && tag == currentBackdrop.ToString())
                {
                    BackdropComboBox.SelectedItem = item;
                    break;
                }
            }
            if (BackdropComboBox.SelectedItem == null)
                BackdropComboBox.SelectedIndex = 1; // MicaAlt
        }

        public ElementTheme SelectedTheme
        {
            get
            {
                if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    if (Enum.TryParse<ElementTheme>(tag, out var theme))
                        return theme;
                }
                return ElementTheme.Default;
            }
        }

        public BackdropType SelectedBackdrop
        {
            get
            {
                if (BackdropComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    if (Enum.TryParse<BackdropType>(tag, out var backdrop))
                        return backdrop;
                }
                return BackdropType.MicaAlt;
            }
        }
    }
}
