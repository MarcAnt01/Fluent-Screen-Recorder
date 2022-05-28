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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.Save(_tempFile);
            await MainPage.Current.LoadThumbanails();
            Frame.Visibility = Visibility.Collapsed;
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.SaveAs(_tempFile);
            await MainPage.Current.LoadThumbanails();
            Frame.Visibility = Visibility.Collapsed;
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewPlayer.Source = null;
        }
    }
       
}