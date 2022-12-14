using FluentScreenRecorder.Models;
using FluentScreenRecorder.Dialogs;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System.Linq;

namespace FluentScreenRecorder.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            SetupTitleBar();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;

            BitrateComboBox.SelectedItem = App.RecorderHelper.Bitrates.FirstOrDefault(b => b.Bitrate == App.Settings.Bitrate);

            if (App.Settings.IntAudio)
                AudioModeComboBox.SelectedIndex = 1;
            else if (App.Settings.ExtAudio)
                AudioModeComboBox.SelectedIndex = 2;
            else
                AudioModeComboBox.SelectedIndex = 0;
        }

        private void SetupTitleBar(CoreApplicationViewTitleBar coreAppTitleBar = null)
        {
            var coreTitleBar = coreAppTitleBar ?? CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Get the size of the caption controls area and back button 
            // (returned in logical pixels), and move your content around as necessary.
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);

            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(UserLayout);

            // Register a handler for when the size of the overlaid caption control changes.
            // For example, when the app moves to a screen with a different DPI.
            coreTitleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;
        }

        public void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            SetupTitleBar(sender);
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(412, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                if (modeSwitched)
                {
                    GoToOverlayIcon.Visibility = Visibility.Collapsed;
                    ExitOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new()
                    {
                        Content = Strings.Resources.ExitOverlay
                    };
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.ExitOverlay);
                }
            }
            else
            {
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                if (modeSwitched)
                {
                    ExitOverlayIcon.Visibility = Visibility.Collapsed;
                    GoToOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new()
                    {
                        Content = Strings.Resources.GoToOverlay
                    };
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
                
                if (VideoPreviewPage.Current != null && VideoPreviewPage.Source != null)
                {
                    VideoPreviewPage.Current.PreviewPlayer.Source = VideoPreviewPage.Source;
                    VideoPreviewPage.Current._tempFile = VideoPreviewPage.TempFile;
                }
            }
        }

        private async void SystemMic_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"ms-settings:sound";
            var uri = new Uri(uriToLaunch);
            await Launcher.LaunchUriAsync(uri);
        }

        private void BitrateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.Bitrate = (e.AddedItems[0] as BitrateItem).Bitrate;
        }

        private void AudioMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (AudioModeComboBox.SelectedIndex)
            {
                case 0:
                    App.Settings.IntAudio = false;
                    App.Settings.ExtAudio = false;
                    break;
                case 1:
                    App.Settings.IntAudio = true;
                    App.Settings.ExtAudio = false;
                    break;
                case 2:
                    App.Settings.IntAudio = false;
                    App.Settings.ExtAudio = true;
                    break;
            }
        }

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format(" {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 650)
            {
                Column1.Width = new(0.4, GridUnitType.Star);
                AboutFooter.Visibility = Visibility.Collapsed;
                MainGrid.ColumnSpacing = 30;
            }
            else if (e.NewSize.Width <= 650)
            {
                Column1.Width = new(0);
                AboutFooter.Visibility = Visibility.Visible;
                MainGrid.ColumnSpacing = 0;
            }
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new LicensesDialog();
            await dialog.ShowAsync();
        }

        private void OnEscInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
