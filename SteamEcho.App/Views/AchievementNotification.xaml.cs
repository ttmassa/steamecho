using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SteamEcho.App.Views
{
    public partial class AchievementNotification : Window
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        public AchievementNotification(double size)
        {
            InitializeComponent();
            Opacity = 0;
            ContentRendered += AchievementNotificationWindow_ContentRendered;

            // Apply scale based on size (1-10 range -> 0.8-1.2 scale)
            double scale = 0.8 + (size - 1) * 0.4 / 9.0;
            WindowScaleTransform.ScaleX = scale;
            WindowScaleTransform.ScaleY = scale;
        }

        private void AchievementNotificationWindow_ContentRendered(object? sender, EventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - ActualWidth - 10;
            Top = desktopWorkingArea.Bottom - ActualHeight / 2;

            Opacity = 1;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Use Win32 API to force the window to be topmost.
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}