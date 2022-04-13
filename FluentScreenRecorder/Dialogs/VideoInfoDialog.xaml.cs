using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Dialogs
{
    public sealed partial class VideoInfoDialog : ContentDialog
    {
        public VideoInfoDialog()
        {
            
        }

        public VideoInfoDialog(IDictionary<string, object> frameRate, IDictionary<string, object> width, IDictionary <string, object> height)
        {
            InitializeComponent();
            
            if (frameRate.TryGetValue("System.Video.FrameRate", out object frameRateValue) && frameRateValue is uint framerate)
            {
                Frameblock.Text = $"{Strings.Resources.Framerate}: {framerate / 1000d} fps";
            }

            if (width.TryGetValue("System.Video.FrameWidth", out object frameWidthValue) && frameWidthValue is uint widthvalue)
            {
                Widthblock.Text = $"{Strings.Resources.Width}: {widthvalue}";
            }

            if (height.TryGetValue("System.Video.FrameHeight", out object frameHeightValue) && frameHeightValue is uint heightvalue)
            {
                Heightblock.Text = $"{Strings.Resources.Height}: {heightvalue}";
            }
        }
    }
}
