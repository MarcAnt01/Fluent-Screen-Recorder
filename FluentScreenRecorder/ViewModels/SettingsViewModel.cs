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
        public uint Width
        {
            get => Get(nameof(Width), (uint)1920);
            set => Set(nameof(Width), value);
        }

        public uint Height
        {
            get => Get(nameof(Height), (uint)1080);
            set => Set(nameof(Height), value);
        }

        public uint Bitrate
        {
            get => Get(nameof(Bitrate), (uint)18000000);
            set => Set(nameof(Bitrate), value);
        }

        public uint FrameRate
        {
            get => Get(nameof(FrameRate), (uint)60);
            set => Set(nameof(FrameRate), value);
        }

        public bool IntAudio
        {
            get => Get(nameof(IntAudio), true);
            set => Set(nameof(IntAudio), value);
        }

        public bool ExtAudio
        {
            get => Get(nameof(ExtAudio), false);
            set => Set(nameof(ExtAudio), value);
        }

        public bool Gallery
        {
            get => Get(nameof(Gallery), true);
            set => Set(nameof(Gallery), value);
        }

        public bool SystemPlayer
        {
            get => Get(nameof(SystemPlayer), false);
            set => Set(nameof(SystemPlayer), value);
        }

        public bool ShowOnTop
        {
            get => Get(nameof(ShowOnTop), false);
            set => Set(nameof(ShowOnTop), value);
        }

        public bool Timer
        {
            get => Get(nameof(Timer), false);
            set => Set(nameof(Timer), value);
        }

        public bool AutoResizeWindow
        {
            get => Get(nameof(AutoResizeWindow), true);
            set => Set(nameof(AutoResizeWindow), value);
        }

        /// <summary>
        /// Gets an app setting.
        /// </summary>
        /// <param name="setting">Setting name.</param>
        /// <param name="defaultValue">Default setting value.</param>
        /// <returns>App setting value.</returns>
        private T Get<T>(string setting, T defaultValue)
        {
            // Get app settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // Check if the setting exists
            if (localSettings.Values[setting] == null)
            {
                localSettings.Values[setting] = defaultValue;
            }

            object val = localSettings.Values[setting];

            // Return the setting if type matches
            if (val is not T)
            {
                throw new ArgumentException("Type mismatch for \"" + setting + "\" in local store. Got " + val.GetType());
            }
            return (T)val;
        }

        /// <summary>
        /// Sets an app setting.
        /// </summary>
        /// <param name="setting">Setting name.</param>
        /// <param name="newValue">New setting value.</param>
        private void Set<T>(string setting, T newValue)
        {
            // Try to get the setting, if types don't match, it'll throw an exception
            _ = Get(setting, newValue);

            // Get app settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[setting] = newValue;
        }
    }
}
