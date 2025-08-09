using System.Windows;

namespace SteamEcho.App.Views
{
    public partial class MessageDialog : Window
    {
        public MessageDialog(string message, string title)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = title;
            MessageText.Text = message;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}