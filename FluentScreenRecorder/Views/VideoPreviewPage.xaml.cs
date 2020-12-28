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
        private readonly StorageFile _tempFile;

        public VideoPreviewPage(StorageFile file)
        {
            this.InitializeComponent();

            _tempFile = file;
            PreviewPlayer.Source = MediaSource.CreateFromStorageFile(file);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (await MainPage.Save(_tempFile))
            {
                Window.Current.Close();
            }
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (await MainPage.SaveAs(_tempFile))
            {
                Window.Current.Close();
            }
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (await MainPage.Delete(_tempFile))
            {
                Window.Current.Close();
            }
        }
    }
}