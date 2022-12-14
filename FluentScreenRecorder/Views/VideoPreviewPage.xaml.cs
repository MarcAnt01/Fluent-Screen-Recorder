using System;
using System.Threading.Tasks;
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
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Views
{
    public sealed partial class VideoPreviewPage : Page
    {
        public static VideoPreviewPage Current;
        public static MediaSource Source;
        public static StorageFile TempFile;
        
        public StorageFile _tempFile;

        public VideoPreviewPage(StorageFile file = null)
        {
            InitializeComponent();

            Current = this;

            if (file != null)
            {
                _tempFile = file;
                TempFile = file;
            }                
            PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);
            Source = (MediaSource)PreviewPlayer.Source;

            ShowSaved();                       
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);            
        }

        public VideoPreviewPage()
        {
            InitializeComponent();

            Current = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is StorageFile file)
            {
                _tempFile = file;
                TempFile = file;
                PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);
                Source = (MediaSource)PreviewPlayer.Source;

                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
            }            
            base.OnNavigatedTo(e);            
        }
        
        public async Task ShowSaved()
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            SavedNotif.Show(4000);                
        }

        private async void CustomMediaTransportControls_SaveAs(object sender, EventArgs e)
        {
            if (_tempFile != null)
               await MainPage.SaveAs(_tempFile);
            else
               await MainPage.SaveAs(TempFile);
        }

        private void CustomMediaTransportControls_Share(object sender, EventArgs e)
        {
            DataTransferManager.ShowShareUI();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = _tempFile.Name;
            request.Data.SetStorageItems(new StorageFile[] { _tempFile });
        }


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewPlayer.Source = null;
        }

        private async void CustomMediaTransportControls_Delete(object sender, EventArgs e)
        {
            if (_tempFile != null)
               await MainPage.Delete(_tempFile);
            else
               await MainPage.Delete(TempFile);
            MainPage.Current.PreviewFrame.Visibility = Visibility.Collapsed;

            PreviewPlayer.Source = null;

            // To avoid re-navigating from settings page back to the
            // video preview page when we already exited it.
            Current = null;
            TempFile = null;
        }

        private async void CustomMediaTransportControls_OpenFolder2(object sender, EventArgs e)
        {
            var folderLocation = await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder");
            var options = new FolderLauncherOptions();
            options.ItemsToSelect.Add(_tempFile);
            await Launcher.LaunchFolderAsync(folderLocation, options);
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var folderLocation = await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder");
            var options = new FolderLauncherOptions();
            options.ItemsToSelect.Add(_tempFile);
            await Launcher.LaunchFolderAsync(folderLocation, options);
        }
    }
}
