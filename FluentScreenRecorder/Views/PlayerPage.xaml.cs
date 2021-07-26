using FluentScreenRecorder.Dialogs;
using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Views
{
    public sealed partial class PlayerPage : Page
    {
        private StorageFile videoFile;

        public PlayerPage(StorageFile file = null)
        {            
        }

        public PlayerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)   
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(UserLayout);
            var tBar = CoreApplication.GetCurrentView().TitleBar;
            tBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;
            if (e.Parameter is StorageFile file)
            {
                videoFile = file;
                VideoPlayer.Source = MediaSource.CreateFromStorageFile(file);                
            }            
        }

        public void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            var bar = sender as CoreApplicationViewTitleBar;
            RightPanel.Margin = new Thickness(0, 0, bar.SystemOverlayRightInset, 0);
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
                }
            }
            else
            {
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                if (modeSwitched)
                {
                    ExitOverlayIcon.Visibility = Visibility.Collapsed;
                    GoToOverlayIcon.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
