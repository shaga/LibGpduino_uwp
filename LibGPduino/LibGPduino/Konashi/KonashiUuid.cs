using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace LibGPduino.Konashi
{
    class KonashiUuid
    {
        private const string UuidBase = "-03fb-40da-98a7-b0def65c2d4b";

        public static readonly string BatteryServiceSelector = GattDeviceService.GetDeviceSelectorFromShortId(0x180F);

        public static readonly Guid KonashiService = Guid.Parse("229bff00" + UuidBase);

        public static readonly string KonashiServiceSelector = GattDeviceService.GetDeviceSelectorFromUuid(KonashiService);

        public static readonly Guid PioSetting = Guid.Parse("229b3000" + UuidBase);
        public static readonly Guid PioPullup = Guid.Parse("229b3001" + UuidBase);
        public static readonly Guid PioOutput = Guid.Parse("229b3002" + UuidBase);
        public static readonly Guid PioInput = Guid.Parse("229b3003" + UuidBase);
        public static readonly Guid PwmConfig = Guid.Parse("229b3004" + UuidBase);
        public static readonly Guid PwmParam = Guid.Parse("229b3005" + UuidBase);
        public static readonly Guid PwmDuty = Guid.Parse("229b3006" + UuidBase);
        public static readonly Guid AnalogDrive = Guid.Parse("229b3007" + UuidBase);
        public static readonly Guid AnalogRead0 = Guid.Parse("229b3008" + UuidBase);
        public static readonly Guid AnalogRead1 = Guid.Parse("229b3009" + UuidBase);
        public static readonly Guid AnalogRead2 = Guid.Parse("229b300a" + UuidBase);
        public static readonly Guid I2CConfig = Guid.Parse("229b300b" + UuidBase);
        public static readonly Guid I2CStartStartStop = Guid.Parse("229b300c" + UuidBase);
        public static readonly Guid I2CWrite = Guid.Parse("229b300d" + UuidBase);
        public static readonly Guid I2CReadParam = Guid.Parse("229b300e" + UuidBase);
        public static readonly Guid I2CRead = Guid.Parse("229b300f" + UuidBase);
        public static readonly Guid UartConfig = Guid.Parse("229b3010" + UuidBase);
        public static readonly Guid UartBaudRate = Guid.Parse("229b3011" + UuidBase);
        public static readonly Guid UartTx = Guid.Parse("229b3012" + UuidBase);
        public static readonly Guid UartRx = Guid.Parse("229b3013" + UuidBase);
        public static readonly Guid HardwareReset = Guid.Parse("229b3014" + UuidBase);
        public static readonly Guid HardwareLowBatery = Guid.Parse("229b3015" + UuidBase);
        public static readonly Guid SpiConfig = Guid.Parse("229b3016" + UuidBase);
        public static readonly Guid SpiData = Guid.Parse("229b3017" + UuidBase);
        public static readonly Guid SpiNotify = Guid.Parse("229b3018" + UuidBase);
    }
}
