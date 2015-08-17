using System.Diagnostics;
using System.Windows;

namespace XapEditor
{
    public partial class AboutWindow : Window
    {
        public AboutWindow(string closeButtonText = "")
        {
            InitializeComponent();
            if (closeButtonText.Length > 0) btnClose.Content = closeButtonText;
        }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://forum.xda-developers.com/donatetome.php?u=4274526"));
            Close();
        }
    }
}
