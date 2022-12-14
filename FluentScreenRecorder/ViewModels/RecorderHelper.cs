using FluentScreenRecorder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace FluentScreenRecorder.ViewModels
{
    public class RecorderHelper : INotifyPropertyChanged
    {
        public IDirect3DDevice Device;
        public CaptureEncoder.Encoder Encoder;
        public List<ResolutionItem> Resolutions;
        public List<BitrateItem> Bitrates;
        public List<FrameRateItem> Framerates;

        private Size _size;

        public event PropertyChangedEventHandler PropertyChanged;

        public Size Size
        {
            get
            {
                if (_size != null)
                    _size = new(Window.Current.Bounds.Width, Window.Current.Bounds.Height);

                return _size;
            }
        }

        public bool Initialized { get; set; }

        public bool IsRecording { get; set; }

        public void SetAppSize(Size size, bool save = true)
        {
            if (_size == size || _size.IsEmpty) return;

            if (ApplicationView.GetForCurrentView().TryResizeView(size))
            {
                _size = size;

                if (save)
                    App.Settings.Size = size;

                PropertyChanged?.Invoke(this, new(nameof(Size)));
            }
        }

        public static T ParseEnumValue<T>(string input)
        {
            return (T)Enum.Parse(typeof(T), input, false);
        }
    }
}
