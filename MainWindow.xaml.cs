using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Timers;

namespace Eos
{
    public partial class MainWindow : Window
    {
        private const string HomeUrl = "https://www.microsoft.com";
        private readonly List<TabData> tabs = new();
        private TabData? activeTab;
        private System.Timers.Timer? titleTimer;  // Add full type

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            StartTitleTimer();
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
        }

        private void CreateNewTab(bool isFirstTab, string url)
        {
            var tab = new TabData
            {
                Title = "New Tab",
                Url = url
            };

            tabs.Add(tab);
            CreateTabButton(tab);

            if (isFirstTab || activeTab == null)
                SwitchToTab(tab);
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

            // Title label (stretches)
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

            // Clean close button
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
            activeTab = tab;
            AddressBar.Text = tab.Url;
            Browser.Source = new Uri(tab.Url);

            foreach (var t in tabs)
            {
                if (t.TabContainer != null)
                {
                    if (t == tab)
                    {
                        // Highlight ENTIRE tab container (not just close button)
                        t.TabContainer.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                        foreach (var child in t.TabContainer.Children)
                        {
                            if (child is Label lbl) lbl.Foreground = Brushes.White;
                            if (child is Button btn) btn.Foreground = Brushes.White;
                        }
                    }
                    else
                    {
                        t.TabContainer.Background = Brushes.White;
                        foreach (var child in t.TabContainer.Children)
                        {
                            if (child is Label lbl) lbl.Foreground = Brushes.Black;
                            if (child is Button btn) btn.Foreground = Brushes.Gray;
                        }
                    }
                }
            }
        }

        private void NavigateTab(TabData tab, string input)
        {
            var target = NormalizeInput(input);
            tab.Url = target;
            AddressBar.Text = target;
            Browser.Source = new Uri(target);
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

        private async void Browser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async(null);
            Browser.Source = new Uri(HomeUrl);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Browser?.CoreWebView2?.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Browser?.CoreWebView2?.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Browser?.CoreWebView2?.Reload();
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
                string url = NormalizeInput(AddressBar.Text);
                if (activeTab != null)
                    activeTab.Url = url;
                Browser.Source = new Uri(url);
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
            Keyboard.Focus(AddressBar);
        }

        private async void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (activeTab != null)
            {
                activeTab.Url = Browser.Source?.ToString() ?? activeTab.Url;

                // Fast title capture - 200ms delay
                await Task.Delay(200);

                Dispatcher.Invoke(() =>
                {
                    if (Browser.CoreWebView2 != null)
                    {
                        string title = Browser.CoreWebView2.DocumentTitle;
                        if (!string.IsNullOrEmpty(title) && title != "New Tab" && title != activeTab.Url)
                        {
                            activeTab.Title = title.Length > 25 ? title[..25] + "…" : title;
                            UpdateTabTitle(activeTab);
                        }
                    }
                });
            }
        }

        private void StartTitleTimer()
        {
            titleTimer?.Stop();
            titleTimer?.Dispose();
            titleTimer = new System.Timers.Timer(1500);  // 1.5s interval - efficient
            titleTimer.Elapsed += TitleTimer_Elapsed;
            titleTimer.Start();
        }

        private void TitleTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (activeTab != null && Browser.CoreWebView2 != null)
                {
                    string title = Browser.CoreWebView2.DocumentTitle;
                    if (!string.IsNullOrEmpty(title) && title != activeTab.Title)
                    {
                        activeTab.Title = title.Length > 25 ? title[..25] + "…" : title;
                        UpdateTabTitle(activeTab);
                    }
                }
            });
        }
    }
}
