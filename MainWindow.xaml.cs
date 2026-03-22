using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eos
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string HomeUrl = "https://www.microsoft.com";
        public MainWindow()
        {
            InitializeComponent();
            InitializeBrowserAsync();
        }

        private async void InitializeBrowserAsync()
        {
            await Browser.EnsureCoreWebView2Async(null);
            Browser.CoreWebView2.Navigate(HomeUrl);
            AddressBar.Text = HomeUrl;

        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CoreWebView2 != null && Browser.CoreWebView2.CanGoBack)
            {
                Browser.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CoreWebView2 != null && Browser.CoreWebView2.CanGoForward)
            {
                Browser.CoreWebView2.GoForward();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Browser?.CoreWebView2?.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CoreWebView2 != null)
            {
                Browser.CoreWebView2.Navigate(HomeUrl);
                AddressBar.Text = HomeUrl;
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Browser.CoreWebView2 != null)
            {
                string url = AddressBar.Text.Trim();

                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                Browser.CoreWebView2.Navigate(url);
            }
        }

        private void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (Browser.CoreWebView2 != null)
            {
                AddressBar.Text = Browser.Source?.ToString();
                Title = Browser.CoreWebView2.DocumentTitle + " - Eos";
            }
        }
    }
}