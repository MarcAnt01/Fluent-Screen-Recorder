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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Save();
            await appwindow.CloseAsync(); 
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SaveAs();
            await appwindow.CloseAsync();
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Share();
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Cancel();
            await appwindow.CloseAsync();
        }
    }
}