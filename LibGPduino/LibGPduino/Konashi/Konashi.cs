using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace LibGPduino.Konashi
{
    public enum EKonashiUartBaudrate
    {
        UartRate9K6 = 0x0028,
        UartRate19K2 = 0x0050,
        UartRate38K4 = 0x00a0,
        UartRate57K6 = 0x00f0,
        UartRate76K8 = 0x0140,
        UartRate115K2 = 0x01e0,
    }

    public class KonashiUartReceivedEventArgs : EventArgs
    {
        public int Length { get; }
        public byte[] Data { get; }

        public KonashiUartReceivedEventArgs(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                Length = 0;
                Data = null;
            }
            else
            {
                Length = value[0];
                Data = new byte[Length];
                Array.Copy(value, 1, Data, 0, Length);
            }
        }
    }

    public static class Konashi
    {
        public const int UartMaxLength = 18;

        public static byte[] BoolToByteArray(bool isEnable)
        {
            return new[] {(byte) (isEnable ? 1 : 0)};
        }

        public static byte[] ToBayteArray(this EKonashiUartBaudrate rate)
        {
            if (!rate.IsDefined<EKonashiUartBaudrate>()) return null;

            var value = (int) rate;
            return new[] {(byte) ((value >> 8) & 0xff), (byte) (value & 0xff)};
        }

        public static byte[] ToByateArray(this int src, int length)
        {
            if (length <= 0 || length > 4) return null;
            var array = new byte[length];

            for (var i = 0; i < length; i++)
            {
                array[i] = (byte) ((src >> ((length - 1 - i)*8)) & 0xff);
            }

            return array;
        }

        public static bool IsDefined<T>(this Enum value) where T : struct
        {
            if (value == null) return false;

            if (typeof (T) != value.GetType()) return false;

            return Enum.IsDefined(typeof (T), value);
        }

        public static bool HasRead(this GattCharacteristic characteristic)
        {
            return characteristic.HasProperty(GattCharacteristicProperties.Read);
        }

        public static bool HasWriteWithoutResponse(this GattCharacteristic characteristic)
        {
            return characteristic.HasProperty(GattCharacteristicProperties.WriteWithoutResponse);
        }

        public static bool HasNotify(this GattCharacteristic characteristic)
        {
            return characteristic.HasProperty(GattCharacteristicProperties.Notify);
        }

        private static bool HasProperty(this GattCharacteristic characteristic, GattCharacteristicProperties property)
        {
            if (!property.IsDefined<GattCharacteristicProperties>()) return false;

            return characteristic.CharacteristicProperties.HasFlag(property);
        }
    }
}
