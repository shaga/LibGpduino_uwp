using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace LibGPduino.Konashi
{
    public class KonashiScanner
    {
        /// <summary>
        /// event found konashi
        /// </summary>
        public event EventHandler<KonashiInfo> FoundKonashi;

        /// <summary>
        /// event finished scan
        /// </summary>
        public event EventHandler FinishedScan;

        private DeviceInformationCollection PairedKonashi { get; set; }
        private DeviceInformationCollection PairedBattery { get; set; }

        private BluetoothLEAdvertisementWatcher Watcher { get; }

        public bool IsScanning => Watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        public KonashiScanner()
        {
            Watcher = new BluetoothLEAdvertisementWatcher();
            Watcher.Received += OnFoundKonashi;
            Watcher.Stopped += (s, e) => FinishedScan?.Invoke(this, EventArgs.Empty);

            InitPairedDevice();
        }

        /// <summary>
        /// start scan
        /// </summary>
        public void StartScan()
        {
            if (IsScanning) return;

            Watcher.Start();
        }

        /// <summary>
        /// stop scan
        /// </summary>
        public void StopScan()
        {
            if (!IsScanning) return;

            Watcher.Stop();
        }

        private async void InitPairedDevice()
        {
            PairedKonashi = await DeviceInformation.FindAllAsync(KonashiUuid.KonashiServiceSelector);
            PairedBattery = await DeviceInformation.FindAllAsync(KonashiUuid.BatteryServiceSelector);
        }

        private void OnFoundKonashi(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs e)
        {
            var address = $"_{e.BluetoothAddress.ToString("x12")}";

            var konashi = PairedKonashi?.FirstOrDefault(i => i.Id.Contains(address));
            var battery = PairedBattery?.FirstOrDefault(i => i.Id.Contains(address));

            if (konashi != null && battery != null)
            {
                FoundKonashi?.Invoke(this, new KonashiInfo(e.BluetoothAddress, konashi, battery));
            }
        }
    }
}
