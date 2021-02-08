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
using Keys = System.Windows.Forms.Keys;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace monitorBuddyTray
{
    class AudioMBDeviceManager
    {

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public class AudioDeviceMBState
        {
            public string Id;
            public string Name;
            public bool InRotation = true;
            public bool IsDefault = false;
            public IAudioDevice AudioDevice = null;
        };

        private readonly string kPlayDevicesCategory = "audioplaydevices";
        private readonly string kInputDevicesCategory = "audioinputdevices";
        private readonly string kSoundOnChimeKey = "sndonchime";

        private List<AudioDeviceMBState> _playDevices = new List<AudioDeviceMBState>();
        private List<AudioDeviceMBState> _inputDevices = new List<AudioDeviceMBState>();

        public List<AudioDeviceMBState> AudioPlayDevices { get { return _playDevices; } }
        public List<AudioDeviceMBState> AudioInputDevices { get { return _inputDevices; } }
        public bool PlayChimeOnChange { get; private set; }

        public event EventHandler DevicesChanged;

        void BuildDeviceList(List<IAudioDevice> audioDevices, List<AudioDeviceMBState> deviceList)
        {
            deviceList.Clear();

            foreach (var audioDevice in audioDevices)
            {
                if (!deviceList.Any(x => x.Id == audioDevice.Id))
                {
                    deviceList.Add(new AudioDeviceMBState()
                    {
                        Id = audioDevice.Id,
                        Name = audioDevice.FriendlyName,
                        AudioDevice = audioDevice,
                        IsDefault = audioDevice.IsDefault(Role.Console)
                    });
                }
            }
        }

        public void RefreshDevices()
        {
            BuildDeviceList(AudioController.GetActivePlaybackDevices(), _playDevices);
            BuildDeviceList(AudioController.GetActiveRecordingDevices(), _inputDevices);
            FetchSettings();
            SaveSettings();
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }

        public AudioMBDeviceManager()
        {
            AudioController.DeviceAdded += AudioController_DeviceAdded;
            AudioController.DeviceRemoved += AudioController_DeviceRemoved;
            AudioController.DeviceDefaultChanged += AudioController_DeviceDefaultChanged;
        }
        
        public void ChangeVolume(object source, EventArgs args)
        {
            var dialRotationArgs = args as USBMonitorBuddyDevice.DialRotationEventArgs;
            var volDiff = dialRotationArgs.dialChange;
            var upOrDown = volDiff < 0 ? (byte)Keys.VolumeDown : (byte)Keys.VolumeUp;
            volDiff = Math.Abs(volDiff);
            while (volDiff > 0)
            {
                keybd_event(upOrDown, 0, 0, 0); // change volume
                volDiff--;
            }
        }

        public void ChangePlayDevice(object source, EventArgs args)
        {
            ChangeDevice(AudioController.GetActivePlaybackDevices(), _playDevices);
        }

        public void ChangeRecordDevice(object source, EventArgs args)
        {
            ChangeDevice(AudioController.GetActiveRecordingDevices(), _inputDevices);
        }

        public void ChangeDevice(List<IAudioDevice> activeDevices, List<AudioDeviceMBState> deviceMBStateList)
        {
            if (deviceMBStateList.Count == 0)
                return;

            // refresh devices on state list
            foreach (var device in deviceMBStateList)
            {
                try
                {
                    device.AudioDevice = activeDevices.Where(x => x.Id == device.Id).First();
                }
                catch
                {
                    Console.WriteLine($"Couldn't find device {device.Name} in active device list");
                }
            }

            // get system default
            var mbDeviceStateQueue = new Queue<AudioDeviceMBState>(deviceMBStateList.ToArray());
            AudioDeviceMBState mbDeviceState = null;
            do 
            {
                mbDeviceState = mbDeviceStateQueue.Dequeue();
                mbDeviceStateQueue.Enqueue(mbDeviceState);
            } while (!mbDeviceState.AudioDevice.IsDefault(Role.Console));

            AudioDeviceMBState deviceToSetDefault = mbDeviceStateQueue.First();
            foreach (var device in mbDeviceStateQueue)
            {
                if(device.InRotation)
                {
                    deviceToSetDefault = device;
                    break;
                }
            }

            deviceToSetDefault.AudioDevice.SetAsDefault(Role.Console);

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
                    .AddText($"{(_playDevices == deviceMBStateList ? "Device" : "Input device")} changed: {deviceToSetDefault.AudioDevice.FriendlyName}")
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
        public void SetInRotationState(bool playDevice, string id, bool state)
        {
            var deviceList = playDevice ? _playDevices : _inputDevices;
            var device = deviceList.First(x => x.Id == id);
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

            void FetchDeviceContainer(string containerName, List<AudioDeviceMBState> deviceList)
            {
                var deviceContainer = localSettings.CreateContainer(containerName, Windows.Storage.ApplicationDataCreateDisposition.Always);
                foreach (var setting in deviceContainer.Values)
                {
                    try
                    {
                        var savedDevice = deviceList.First(x => x.Id == setting.Key);
                        if (savedDevice != null)
                        {
                            savedDevice.InRotation = (bool)setting.Value;
                        }
                    }
                    catch { }
                }
            }

            FetchDeviceContainer(kPlayDevicesCategory, _playDevices);
            FetchDeviceContainer(kInputDevicesCategory, _inputDevices);

            var container = localSettings.CreateContainer("preferences", Windows.Storage.ApplicationDataCreateDisposition.Always);
            if (container.Values.ContainsKey("SndOnChime"))
            {
                PlayChimeOnChange = (bool)container.Values[kSoundOnChimeKey];
            }
        }

        private void SaveSettings()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            void SaveDeviceContainer(string containerName, List<AudioDeviceMBState> deviceList)
            {
                var deviceContainer = localSettings.CreateContainer(containerName, Windows.Storage.ApplicationDataCreateDisposition.Always);
                foreach (var device in deviceList)
                {
                    deviceContainer.Values[device.Id] = device.InRotation;
                }
            }

            SaveDeviceContainer(kPlayDevicesCategory, _playDevices);
            SaveDeviceContainer(kInputDevicesCategory, _inputDevices);

            var container = localSettings.CreateContainer("preferences", Windows.Storage.ApplicationDataCreateDisposition.Always);
            container.Values[kSoundOnChimeKey] = PlayChimeOnChange;
         }

        private void AudioController_DeviceDefaultChanged(object sender, DeviceDefaultChangedEvent e)
        {
            // don't care
            // RefreshDevices();
        }

        private void AudioController_DeviceRemoved(object sender, DeviceRemovedEvent e)
        {
            RefreshDevices();
        }

        private void AudioController_DeviceAdded(object sender, DeviceAddedEvent e)
        {
            RefreshDevices();
        }

        public void ChimeOnChangeItem_Checked(object sender, RoutedEventArgs e)
        {
            PlayChimeOnChange = e.RoutedEvent == MenuItem.CheckedEvent;
            SaveSettings();
        }
    }
}
