using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace LibGPduino.Konashi
{
    public class KonashiManager : IDisposable
    {
        public event EventHandler<KonashiUartReceivedEventArgs> ReceivedUartData;

        private GattDeviceService KonashiService { get; set; }
        private GattDeviceService BatteryService { get; set; }

        private Dictionary<Guid,GattCharacteristic> KonashiCharacteristics { get; }

        private SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

        public KonashiManager()
        {
            KonashiCharacteristics = new Dictionary<Guid, GattCharacteristic>();
        }

        ~KonashiManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            KonashiService?.Dispose();
            BatteryService?.Dispose();
        }

        public virtual async Task Init(DeviceInformation konashi, DeviceInformation battery)
        {
            await InitKonashi(konashi);
            await InitBattery(battery);
        }

        public async Task SetUartConfig(bool isEnable)
        {
            var value = Konashi.BoolToByteArray(isEnable);

            await WriteCharacteristic(KonashiUuid.UartConfig, value);
        }

        public async Task SetUartBaudrate(EKonashiUartBaudrate rate)
        {
            var value = rate.ToBayteArray();

            await WriteCharacteristic(KonashiUuid.UartBaudRate, value);
        }

        public async Task WriteUartTx(byte[] data)
        {
            var length = Math.Min(data.Length, Konashi.UartMaxLength);

            var value = new byte[length + 1];

            value[0] = (byte) length;

            Array.Copy(data, 0, value, 1, length);

            await WriteCharacteristic(KonashiUuid.UartTx, value);
        }

        public async Task WriteUartTx(string data)
        {
            await WriteUartTx(Encoding.UTF8.GetBytes(data));
        }

        public async Task<byte[]> ReadUartRx()
        {
            return await ReadCharacteristic(KonashiUuid.UartRx);
        }

        private async Task InitKonashi(DeviceInformation konashi)
        {
            if (konashi == null) return;

            KonashiService?.Dispose();

            KonashiService = await GattDeviceService.FromIdAsync(konashi.Id);

            if (KonashiService == null) return;

            var characteristics = KonashiService.GetAllCharacteristics();

            foreach (var characteristic in characteristics)
            {
                InitCharacteristic(characteristic);
            }
        }

        private async Task InitBattery(DeviceInformation battery)
        {
            if (battery == null) return;

            BatteryService?.Dispose();

            BatteryService = await GattDeviceService.FromIdAsync(battery.Id);

            if (BatteryService == null) return;

            var characteristic = BatteryService.GetCharacteristics(GattCharacteristicUuids.BatteryLevel)?.FirstOrDefault();

            if (characteristic == null) return;

            InitCharacteristic(characteristic);
        }

        private void InitCharacteristic(GattCharacteristic characteristic)
        {
            var uuid = characteristic.Uuid;

            if (KonashiCharacteristics.ContainsKey(uuid)) return;

            if (characteristic.HasNotify())
            {
                characteristic.ValueChanged += OnValueChanged;
            }

            KonashiCharacteristics.Add(uuid, characteristic);
        }

        private async Task<byte[]> ReadCharacteristic(Guid uuid)
        {
            if (!KonashiCharacteristics.ContainsKey(uuid)) return null;

            var characteristic = KonashiCharacteristics[uuid];

            if (!characteristic.HasRead()) return null;

            await Semaphore.WaitAsync();

            var result = await characteristic.ReadValueAsync();

            Semaphore.Release();

            return result.Value.ToArray();
        }

        private async Task WriteCharacteristic(Guid uuid, byte[] value)
        {
            if (!KonashiCharacteristics.ContainsKey(uuid)) return;

            var characteristic = KonashiCharacteristics[uuid];

            if (!characteristic.HasWriteWithoutResponse()) return;

            await Semaphore.WaitAsync();

            await characteristic.WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithoutResponse);

            Semaphore.Release();
        }

        private void OnValueChanged(GattCharacteristic characteristic, GattValueChangedEventArgs e)
        {
            if (characteristic.Uuid == KonashiUuid.UartRx)
            {
                ReceivedUartData?.Invoke(this, new KonashiUartReceivedEventArgs(e.CharacteristicValue.ToArray()));
            }
        }
    }
}
