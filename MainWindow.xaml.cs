using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Eos
{
    public partial class MainWindow : Window
    {
        private const string HomeUrl = "file:///C://Users//choks//LocalDocuments//Eos//Assets//newTab.html";
        private readonly List<TabData> tabs = new();
        private TabData? activeTab;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateNewTab(true, HomeUrl);
        }

        private class TabData
        {
            public string Title { get; set; } = "New Tab";
            public string Url { get; set; } = "";
            public Button? TabButton { get; set; }
            public StackPanel? TabContainer { get; set; }
            public WebView2? WebView { get; set; }
        }

        private void CreateNewTab(bool isFirstTab, string url)
        {
            var tab = new TabData { Title = "New Tab", Url = url };
            tabs.Add(tab);

            // Create WebView2 for the tab
            var webView = new WebView2();
            webView.VerticalAlignment = VerticalAlignment.Stretch;
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.Visibility = Visibility.Collapsed;
            webView.NavigationCompleted += Browser_NavigationCompleted;
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            tab.WebView = webView;
            BrowserArea.Children.Add(webView);

            CreateTabButton(tab);

            if (isFirstTab || activeTab == null)
                SwitchToTab(tab);
            AddressBar.Text = "";
        }

        private async void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess && sender is WebView2 wv)
            {
                // Force navigation immediately after initialization
                if (!string.IsNullOrEmpty(activeTab?.Url))
                {
                    try
                    {
                        wv.Source = new Uri(activeTab.Url);
                    }
                    catch
                    {
                        wv.Source = new Uri(HomeUrl); // Fallback
                    }
                }
                else
                {
                    wv.NavigateToString(HomeUrl); // Use Navigate() for more reliable local file loading
                }
            }
        }


        private void CreateTabButton(TabData tab)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Height = 28,
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };

            var titleLabel = new Label
            {
                Content = tab.Title,
                Padding = new Thickness(12, 6, 6, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            var closeButton = new Button
            {
                Content = "×",
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) => CloseTab(tab);
            stack.MouseLeftButtonDown += (_, _) => SwitchToTab(tab);

            stack.Children.Add(titleLabel);
            stack.Children.Add(closeButton);

            tab.TabContainer = stack;
            TabStrip.Children.Add(stack);
        }

        private void UpdateTabTitle(TabData tab)
        {
            if (tab.TabContainer?.Children[0] is Label titleLabel)
            {
                titleLabel.Content = tab.Title;
            }
        }

        private void CloseTab(TabData tabToClose)
        {
            if (tabs.Count <= 1) return;

            tabs.Remove(tabToClose);

            if (tabToClose.WebView != null)
            {
                tabToClose.WebView.NavigationCompleted -= Browser_NavigationCompleted;
                tabToClose.WebView.CoreWebView2InitializationCompleted -= WebView_CoreWebView2InitializationCompleted;
                tabToClose.WebView.Dispose();
                BrowserArea.Children.Remove(tabToClose.WebView);
            }

            TabStrip.Children.Remove(tabToClose.TabContainer);

            if (activeTab == tabToClose)
            {
                if (tabs.Count > 0)
                    SwitchToTab(tabs[^1]);
                else
                    CreateNewTab(true, HomeUrl);
            }
        }

        private void SwitchToTab(TabData tab)
        {
            if (activeTab == tab)
                return;

            // Hide previous WebView
            if (activeTab?.WebView != null)
                activeTab.WebView.Visibility = Visibility.Collapsed;

            activeTab = tab;

            if (tab.WebView != null)
                tab.WebView.Visibility = Visibility.Visible;

            AddressBar.Text = tab.Url;
        }

        private void NavigateTab(TabData tab, string input)
        {
            if (tab.WebView == null) return;

            var target = NormalizeInput(input);
            tab.Url = target;
            AddressBar.Text = target;
            tab.WebView.Source = new Uri(target);
        }

        private string NormalizeInput(string input)
        {
            input = input.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return HomeUrl;

            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return input;

            if (input.Contains('.') && !input.Contains(' '))
                return "https://" + input;

            return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeTab?.WebView?.CoreWebView2 != null)
                activeTab.WebView.CoreWebView2.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeTab?.WebView?.CoreWebView2 != null)
                activeTab.WebView.CoreWebView2.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeTab?.WebView?.CoreWebView2 != null)
                activeTab.WebView.CoreWebView2.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeTab != null)
                NavigateTab(activeTab, HomeUrl);
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewTab(false, HomeUrl);
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (activeTab == null) return;

                string url = NormalizeInput(AddressBar.Text);
                NavigateTab(activeTab, url);
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
            Keyboard.Focus(AddressBar);
        }

        private void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!(sender is WebView2 wv))
                return;

            if (activeTab == null || activeTab.WebView != wv)
                return;

            if (!e.IsSuccess)   // or: if (e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown)
                return;

            activeTab.Url = wv.Source?.ToString() ?? activeTab.Url;

            Dispatcher.Invoke(() =>
            {
                if (wv.CoreWebView2 != null)
                {
                    string title = wv.CoreWebView2.DocumentTitle;
                    if (!string.IsNullOrEmpty(title) && title != "New Tab")
                    {
                        activeTab.Title = title.Length > 25 ? title[..25] + "…" : title;
                        UpdateTabTitle(activeTab);
                    }
                }
            });
        }

    }
}
