using FluentScreenRecorder.Dialogs;
using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Views
{
    public sealed partial class PlayerPage : Page
    {
        private StorageFile videoFile;

        public PlayerPage(StorageFile file = null)
        {
            InitializeComponent();
            SetupTitleBar();
        }

        public PlayerPage()
        {
            InitializeComponent();
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
                ToolTip toolTip = new()
                {
                    Content = Strings.Resources.GoToOverlay
                };
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
            }
            else
            {
                ExitOverlayIcon.Visibility = Visibility.Visible;
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
                ToolTip toolTip = new()
                {
                    Content = Strings.Resources.ExitOverlay
                };
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
            await videoFile.DeleteAsync();
            Frame.Navigate(typeof(MainPage));            
        }

        private async void CustomMediaTransportControls_InfoTap(object sender, EventArgs e)
        {
            var frameRate = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameRate" });
            var width = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameWidth" });
            var height = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameHeight" });
            ContentDialog dialog = new VideoInfoDialog(frameRate, width, height);
            await dialog.ShowAsync();
        }

        private void CustomMediaTransportControls_Shared(object sender, EventArgs e)
        {
            DataTransferManager.ShowShareUI();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = videoFile.Name;
            request.Data.SetStorageItems(new StorageFile[] { videoFile });
        }

        private async void CustomMediaTransportControls_OpenFolder(object sender, EventArgs e)
        {
            var folderLocation = await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder");
            var options = new FolderLauncherOptions();
            options.ItemsToSelect.Add(videoFile);
            await Launcher.LaunchFolderAsync(folderLocation, options);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Source = null;
        }

        private void OnEscInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
