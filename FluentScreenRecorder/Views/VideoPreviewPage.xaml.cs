using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
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
    public sealed partial class VideoPreviewPage : Page
    {
        private StorageFile _tempFile;

        public VideoPreviewPage(StorageFile file = null)
        {
            InitializeComponent();
            SetupTitleBar();

            if (file != null)
            {
                _tempFile = file;
            }                
            PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);            
        }

        public VideoPreviewPage()
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

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {                       
            if (_tempFile != null)
            {
                DataRequest request = e.Request;
                request.Data.Properties.Title = _tempFile.Name;
                request.Data.SetStorageItems(new StorageFile[] { _tempFile });
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is StorageFile file)
            {
                _tempFile = file;
                PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);

                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
            }
            base.OnNavigatedTo(e);            
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.Save(_tempFile);
            Frame.Navigate(typeof(MainPage));
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.SaveAs(_tempFile);
            Frame.Navigate(typeof(MainPage));
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.Delete(_tempFile);
            Frame.Navigate(typeof(MainPage));
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
            PreviewPlayer.Source = null;
        }
    }
       
}