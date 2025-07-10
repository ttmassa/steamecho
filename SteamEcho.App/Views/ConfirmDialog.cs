using System.Windows;

namespace SteamEcho.App.Views
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string message, string title)
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}