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

            MainPage.Current.SettingsButton.Visibility = Visibility.Collapsed;

            if (file != null)
            {
                _tempFile = file;
            }                
            PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);

            ApplicationView.GetForCurrentView().TryResizeView(new(500, 500));

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);            
        }

        public VideoPreviewPage()
        {
            InitializeComponent();

            MainPage.Current.SettingsButton.Visibility = Visibility.Collapsed;

            ApplicationView.GetForCurrentView().TryResizeView(new(500, 500));
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



        private async void CustomMediaTransportControls_SaveAs(object sender, EventArgs e)
        {
            await MainPage.SaveAs(_tempFile);
            await MainPage.Current.LoadThumbanails();
            Frame.Visibility = Visibility.Collapsed;
            MainPage.Current.SettingsButton.Visibility = Visibility.Visible;
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        

      


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewPlayer.Source = null;
        }

        private void CustomMediaTransportControls2_Deleted(object sender, EventArgs e)
        {

        }

        private void CustomMediaTransportControls2_SaveAs(object sender, EventArgs e)
        {
        }

        private void CustomMediaTransportControls2_Shared(object sender, EventArgs e)
        {

        }
    }
}