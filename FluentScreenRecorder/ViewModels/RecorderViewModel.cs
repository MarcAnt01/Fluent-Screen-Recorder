using FluentScreenRecorder.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.ViewManagement;

namespace FluentScreenRecorder.ViewModels
{
    public class RecorderViewModel : INotifyPropertyChanged
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
            get => _size;
            set
            {
                if (_size == value || _size.IsEmpty) return;

                if (ApplicationView.GetForCurrentView().TryResizeView(value))
                    _size = value;

                PropertyChanged?.Invoke(this, new(nameof(Size)));
            }
        }

        public int GetResolutionIndex(uint width, uint height)
        {
            for (var i = 0; i < App.RecViewModel.Resolutions.Count; i++)
            {
                var resolution = App.RecViewModel.Resolutions[i];
                if (resolution.Resolution.Width == width &&
                    resolution.Resolution.Height == height)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetBitrateIndex(uint bitrate)
        {
            for (var i = 0; i < App.RecViewModel.Bitrates.Count; i++)
            {
                if (App.RecViewModel.Bitrates[i].Bitrate == bitrate)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetFrameRateIndex(uint frameRate)
        {
            for (var i = 0; i < App.RecViewModel.Framerates.Count; i++)
            {
                if (App.RecViewModel.Framerates[i].FrameRate == frameRate)
                {
                    return i;
                }
            }
            return -1;
        }

        public static T ParseEnumValue<T>(string input)
        {
            return (T)Enum.Parse(typeof(T), input, false);
        }
    }
}
