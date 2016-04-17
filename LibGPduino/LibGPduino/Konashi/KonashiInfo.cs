using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace LibGPduino.Konashi
{
    public class KonashiInfo
    {
        public ulong Address { get; }
        public DeviceInformation Konashi { get; }
        public DeviceInformation Battery { get; }

        public string DeviceName => Konashi?.Name;

        public string MacAddress
        {
            get
            {
                return string.Join(":",
                    Enumerable.Range(0, 6).Reverse().Select(i => ((Address >> 8*i) & 0xff).ToString("X2")));
            }
        }

        public KonashiInfo(ulong address, DeviceInformation konashi, DeviceInformation battery)
        {
            Address = address;
            Konashi = konashi;
            Battery = battery;
        }
    }
}
