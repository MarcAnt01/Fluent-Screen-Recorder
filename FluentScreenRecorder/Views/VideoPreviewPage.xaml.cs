using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
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
        StorageFile _tempFile;        
        public static StorageFile VideoFile;
        public static AppWindow Appwindowref;
        AppWindow appwindow;

        public VideoPreviewPage()
        {
            this.InitializeComponent();           
            _tempFile = VideoFile;
            appwindow = Appwindowref;
            Appwindowref = null;
            VideoFile = null;
            PreviewPlayer.Source = MediaSource.CreateFromStorageFile(_tempFile);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            //move the temp file to Videos Library
            StorageFolder localFolder = KnownFolders.VideosLibrary;
            var newFile = await _tempFile.CopyAsync(localFolder);
            if (newFile == null)
            {
                await _tempFile.DeleteAsync();
            }
            else
            {
                await appwindow.CloseAsync();
            }
        }

        private async void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            StorageFile newFile = await PickVideoAsync();
            if (newFile == null)
            {
                // Throw out the encoded video
                await _tempFile.DeleteAsync();
            }
            else
            {
                //move the file to the location selected with the picker
                await _tempFile.MoveAndReplaceAsync(newFile);
               await appwindow.CloseAsync();
            }
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            await _tempFile.DeleteAsync();
            await appwindow.CloseAsync();
        }
        private async Task<StorageFile> PickVideoAsync()
        {
            var picker = new FileSavePicker();
            var time = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.SuggestedFileName = $"recordedVideo{time}";
            picker.DefaultFileExtension = ".mp4";
            picker.FileTypeChoices.Add("MP4 Video", new List<string> { ".mp4" });

            var file = await picker.PickSaveFileAsync();
            return file;
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            ShareSourceLoad();
        }

        private void ShareSourceLoad()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = _tempFile.Name;
            request.Data.SetStorageItems(new StorageFile[] { _tempFile });        
        }        
    }
}