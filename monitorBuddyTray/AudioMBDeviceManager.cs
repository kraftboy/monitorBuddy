using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AudioEndPoint;
using AudioEndPointControllerWrapper;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Notifications;

namespace monitorBuddyTray
{
    class AudioMBDeviceManager
    {
        public class AudioDeviceMBState
        {
            public string Id;
            public string Name;
            public bool InRotation = true;
            public bool IsDefault = false;
            public IAudioDevice AudioDevice = null;
        };

        private List<AudioDeviceMBState> _devices = new List<AudioDeviceMBState>();

        public List<AudioDeviceMBState> AudioDevices { get { return _devices; } }
        public bool PlayChimeOnChange { get; private set; }
       

        public AudioMBDeviceManager()
        {
            List<IAudioDevice> audioPlaybackDevices = AudioController.GetActivePlaybackDevices();
            foreach(var audioDevice in audioPlaybackDevices)
            {
                if(!_devices.Any(x => x.Id == audioDevice.Id))
                {
                    _devices.Add(new AudioDeviceMBState()
                    {
                        Id = audioDevice.Id,
                        Name = audioDevice.FriendlyName,
                        AudioDevice = audioDevice,
                        IsDefault = audioDevice.IsDefault(Role.Console)
                    });
                }
            }

            AudioController.DeviceAdded += AudioController_DeviceAdded;
            AudioController.DeviceRemoved += AudioController_DeviceRemoved;
            AudioController.DeviceDefaultChanged += AudioController_DeviceDefaultChanged;

            FetchSettings();
            SaveSettings();
        }

        public void ChangeDevice(object source, EventArgs args)
        {
            // get system default
            var defaultDevice = AudioController.GetActivePlaybackDevices().First(x => x.IsDefault(Role.Console));

            // get the first item after the matching entry in our list
            var inRotationDevices = _devices.Where(x => x.InRotation).ToList();
            var nextDeviceIndexer = inRotationDevices.Select((x, y) => new { device = x, i = y })
                                                .First(x => x.device.Id == defaultDevice.Id);
            var nextDevice = inRotationDevices[(nextDeviceIndexer.i+1) % inRotationDevices.Count()];

            var deviceToSetDefault = AudioController.GetActivePlaybackDevices().First(x => x.Id == nextDevice.Id);
            deviceToSetDefault.SetAsDefault(Role.Console);

            try
            {
                ToastAudio toastAudio = new ToastAudio();
                if(PlayChimeOnChange)
                {
                    toastAudio.Src = new Uri("ms-appx:///chime.wav");
                }
                else
                {
                    toastAudio.Silent = true;
                }

                ToastContent toastContent = new ToastContentBuilder()
                    .AddText($"Device changed: {nextDevice.Name}")
                    .AddAudio(toastAudio)
                    .GetToastContent();

                // And create the toast notification
                var toast = new ToastNotification(toastContent.GetXml())
                {
                    Tag = "ChangedDevice",
                    ExpirationTime = DateTime.Now.AddSeconds(10)
                };

                // And then show it
                ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception {e}");
            }
        }
        public void SetInRotationState(string id, bool state)
        {
            var device = _devices.First(x => x.Id == id);
            if(device != null)
            {
                device.InRotation = state;
                SaveSettings();
            }
            else
            {
                throw new Exception($"Device with id: \"{id}\" not found when trying to set state to: {state}");
            }
        }

        private void FetchSettings()
        {

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var container = localSettings.CreateContainer("audiodevices", Windows.Storage.ApplicationDataCreateDisposition.Always);
            foreach (var setting in container.Values)
            {
                var savedDevice = _devices.First(x => x.Id == setting.Key);
                if (savedDevice != null)
                {
                    savedDevice.InRotation = (bool)setting.Value;
                }
            }

            container = localSettings.CreateContainer("preferences", Windows.Storage.ApplicationDataCreateDisposition.Always);
            if (container.Values.ContainsKey("SndOnChime"))
            {
                PlayChimeOnChange = (bool)container.Values["SndOnChime"];
            }
        }

        private void SaveSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var container = localSettings.CreateContainer("audiodevices", Windows.Storage.ApplicationDataCreateDisposition.Always);
            foreach(var device in _devices)
            {
                container.Values[device.Id] = device.InRotation;
            }

            container = localSettings.CreateContainer("preferences", Windows.Storage.ApplicationDataCreateDisposition.Always);
            container.Values["SndOnChime"] = PlayChimeOnChange;
         }

        private void AudioController_DeviceDefaultChanged(object sender, DeviceDefaultChangedEvent e)
        {
            // throw new NotImplementedException();
        }

        private void AudioController_DeviceRemoved(object sender, DeviceRemovedEvent e)
        {
            // throw new NotImplementedException();
        }

        private void AudioController_DeviceAdded(object sender, DeviceAddedEvent e)
        {
            // throw new NotImplementedException();
        }

        public void ChimeOnChangeItem_Unchecked(object sender, RoutedEventArgs e)
        {
            PlayChimeOnChange = false;
            SaveSettings();
        }

        public void ChimeOnChangeItem_Checked(object sender, RoutedEventArgs e)
        {
            PlayChimeOnChange = true;
            SaveSettings();
        }
    }
}
