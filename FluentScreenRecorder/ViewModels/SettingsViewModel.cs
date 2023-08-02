using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Storage;

namespace FluentScreenRecorder.ViewModels
{
    public class SettingsViewModel
    {
        public uint Width
        {
            get => Get((uint)1920);
            set => Set(value);
        }

        public uint Height
        {
            get => Get((uint)1080);
            set => Set(value);
        }

        public uint Bitrate
        {
            get => Get((uint)18000000);
            set => Set(value);
        }

        public uint FrameRate
        {
            get => Get((uint)60);
            set => Set(value);
        }

        public bool IntAudio
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ExtAudio
        {
            get => Get(false);
            set => Set(value);
        }

        public bool Gallery
        {
            get => Get(true);
            set => Set(value);
        }

        public bool SystemPlayer
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowOnTop
        {
            get => Get(false);
            set => Set(value);
        }

        public bool Timer
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowCursor
        {
            get => Get(true);
            set => Set(value);
        }

        public Size Size
        {
            get => Get<Size>(new(464, 200));
            set => Set(value);
        }

        /// <summary>
        /// Gets an app setting.
        /// </summary>
        /// <param name="setting">Setting name.</param>
        /// <param name="defaultValue">Default setting value.</param>
        /// <returns>App setting value.</returns>
        private T Get<T>(T defaultValue, [CallerMemberName] string setting = null)
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
        private void Set<T>(T newValue, [CallerMemberName] string setting = null)
        {
            // Try to get the setting, if types don't match, it'll throw an exception
            _ = Get(newValue, setting);

            // Get app settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[setting] = newValue;
        }
    }
}
