using FluentScreenRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;

namespace FluentScreenRecorder.ViewModels
{
    public class RecorderViewModel
    {
        public IDirect3DDevice Device;
        public CaptureEncoder.Encoder Encoder;
        public List<ResolutionItem> Resolutions;
        public List<BitrateItem> Bitrates;
        public List<FrameRateItem> Framerates;

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
