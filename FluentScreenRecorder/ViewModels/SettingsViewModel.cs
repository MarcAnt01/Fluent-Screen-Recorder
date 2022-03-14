using FluentScreenRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Storage;

namespace FluentScreenRecorder.ViewModels
{
    public class SettingsViewModel
    {
        public SettingsViewModel()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(Width))) 
            {
                Width = 1920;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(Height)))
            {
                Height = 1080;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(Bitrate)))
            {
                Bitrate = 18000000;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(FrameRate)))
            {
                FrameRate = 60;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(IntAudio)))
            {
                IntAudio = true;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(ExtAudio)))
            {
                ExtAudio = false;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(Gallery)))
            {
                Gallery = true;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(SystemPlayer)))
            {
                SystemPlayer = false;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(ShowOnTop)))
            {
                ShowOnTop = false;
            }
        }

        private uint _width = (uint)ApplicationData.Current.LocalSettings.Values[nameof(Width)];

        public uint Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(Width)] = value;
            }
        }

        private uint _height = (uint)ApplicationData.Current.LocalSettings.Values[nameof(Height)];

        public uint Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(Height)] = value;
            }
        }

        private uint _bitrate = (uint)ApplicationData.Current.LocalSettings.Values[nameof(Bitrate)];

        public uint Bitrate
        {
            get => _bitrate;
            set
            {
                if (_bitrate != value)
                {
                    _bitrate = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(Bitrate)] = value;
            }
        }

        private uint _framerate = (uint)ApplicationData.Current.LocalSettings.Values[nameof(FrameRate)];

        public uint FrameRate
        {
            get => _framerate;
            set
            {
                if (_framerate != value)
                {
                    _framerate = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(FrameRate)] = value;
            }
        }

        private bool _intAudio = (bool)ApplicationData.Current.LocalSettings.Values[nameof(IntAudio)];

        public bool IntAudio
        {
            get => _intAudio;
            set
            {
                if (_intAudio != value)
                {
                    _intAudio = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(IntAudio)] = value;
            }
        }

        private bool _extAudio = (bool)ApplicationData.Current.LocalSettings.Values[nameof(ExtAudio)];

        public bool ExtAudio
        {
            get => _extAudio;
            set
            {
                if (_extAudio != value)
                {
                    _extAudio = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(ExtAudio)] = value;
            }
        }

        private bool _gallery = (bool)ApplicationData.Current.LocalSettings.Values[nameof(Gallery)];

        public bool Gallery
        {
            get => _gallery;
            set
            {
                if (_gallery != value)
                {
                    _gallery = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(Gallery)] = value;
            }
        }

        private bool _systemPlayer = (bool)ApplicationData.Current.LocalSettings.Values[nameof(SystemPlayer)];

        public bool SystemPlayer
        {
            get => _systemPlayer;
            set
            {
                if (_systemPlayer != value)
                {
                    _systemPlayer = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(SystemPlayer)] = value;
            }
        }

        private bool _showOnTop = (bool)ApplicationData.Current.LocalSettings.Values[nameof(ShowOnTop)];

        public bool ShowOnTop
        {
            get => _showOnTop;
            set
            {
                if (_showOnTop != value)
                {
                    _showOnTop = value;
                }
                ApplicationData.Current.LocalSettings.Values[nameof(ShowOnTop)] = value;
            }
        }
    }
}
