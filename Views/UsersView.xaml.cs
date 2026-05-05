using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Netplwiz.Views
{
    public sealed partial class UsersView : UserControl
    {
        public event EventHandler? RequestAddLocalUser;
        public event EventHandler? RequestAddViaSettings;

        public UsersView()
        {
            this.InitializeComponent();
        }

        private void AddLocalUser_Click(object sender, RoutedEventArgs e)
        {
            RequestAddLocalUser?.Invoke(this, EventArgs.Empty);
        }

        private void AddViaSettings_Click(object sender, RoutedEventArgs e)
        {
            RequestAddViaSettings?.Invoke(this, EventArgs.Empty);
        }
    }
}
