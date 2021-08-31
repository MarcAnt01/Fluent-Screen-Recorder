using FluentScreenRecorder.Dialogs;
using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Views
{
    public sealed partial class PlayerPage : Page
    {
        private StorageFile videoFile;

        public PlayerPage(StorageFile file = null)
        {
            this.InitializeComponent();
            SetupTitleBar();
        }

        public PlayerPage()
        {
            this.InitializeComponent();
            SetupTitleBar();
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

            //Display the right icon for exiting/entering Overlay Mode
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                ExitOverlayIcon.Visibility = Visibility.Collapsed;
                GoToOverlayIcon.Visibility = Visibility.Visible;
                ToolTip toolTip = new ToolTip();
                toolTip.Content = Strings.Resources.GoToOverlay;
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
            }
            else
            {
                ExitOverlayIcon.Visibility = Visibility.Visible;
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
                ToolTip toolTip = new ToolTip();
                toolTip.Content = Strings.Resources.ExitOverlay;
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.ExitOverlay);
            }
        }

        public void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            SetupTitleBar(sender);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)   
        {
            if (e.Parameter is StorageFile file)
            {
                videoFile = file;
                VideoPlayer.Source = MediaSource.CreateFromStorageFile(file);                
            }            
        }

        private async void CustomMediaTransportControls_Deleted(object sender, EventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
            await videoFile.DeleteAsync();
        }

        private async void CustomMediaTransportControls_InfoTap(object sender, EventArgs e)
        {
            var frameRate = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameRate" });
            var width = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameWidth" });
            var height = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameHeight" });
            ContentDialog dialog = new VideoInfoDialog(frameRate, width, height);
            await dialog.ShowAsync();
        }

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                if (modeSwitched)
                {
                    GoToOverlayIcon.Visibility = Visibility.Collapsed;
                    ExitOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new ToolTip();
                    toolTip.Content = Strings.Resources.ExitOverlay;
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
                    ToolTip toolTip = new ToolTip();
                    toolTip.Content = Strings.Resources.GoToOverlay;
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
                }
            }
        }
    }
}
