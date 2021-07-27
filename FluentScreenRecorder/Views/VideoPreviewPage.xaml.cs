using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FluentScreenRecorder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoPreviewPage : Page   
    
    {
        private StorageFile _tempFile;

        public VideoPreviewPage(StorageFile file = null)
        {
            this.InitializeComponent();
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
            }
            else
            {
                ExitOverlayIcon.Visibility = Visibility.Visible;
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
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
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.SaveAs(_tempFile);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.Delete(_tempFile);
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