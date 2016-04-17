using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGPduino
{
    /// <summary>
    /// モーター列挙体
    /// </summary>
    public enum EMotor
    {
        Left = 1,
        Right = 2,
    }

    /// <summary>
    /// LED列挙体
    /// </summary>
    public enum ELed
    {
        Left = 1,
        Right = 2,
    }

    /// <summary>
    /// LEDモード列挙体
    /// </summary>
    public enum ELedMode
    {
        Stop = 0,
        MSec1000 = 1,
        MSec500 = 2,
        Periodic = 3,
    }

    /// <summary>
    /// 旋回モード列挙体
    /// </summary>
    public enum ETurnMode
    {
        RearFixed = 0,
        CommonPhase,
        ReversePhase,
    }

    /// <summary>
    /// サーボ設定
    /// </summary>
    public class ServoSetting
    {
        /// <summary>
        /// 極性
        /// true=正転/false=反転
        /// </summary>
        public bool Porarity { get; }

        /// <summary>
        /// オフセット
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 振幅
        /// </summary>
        public int Amplitude { get; }

        public ServoSetting(bool porarity, int offset, int amplitude)
        {
            Porarity = porarity;
            Offset = offset;
            Amplitude = amplitude;
        }
    }

    static class GPduino
    {
        public const int MaxSpeed = 127;
        public const int MinSpeed = -127;

        public const int MaxAngle = 127;
        public const int MinAngle = -127;

        public const int MaxLed = 255;
        public const int MinLed = 0;

        public const int MaxAmplitude = 255;
        public const int MinAmplitude = 0;

        public const char ServoPolNormal = '+';
        public const char ServoPolReverse = '-';

        public static byte SpeedToByte(this int src)
        {
            if (src < MinSpeed) src = MinSpeed;
            else if (src > MaxSpeed) src = MaxSpeed;

            return (byte) (src & 0xff);
        }

        public static byte AngleToByte(this int src)
        {
            if (src < MinAngle) src = MinAngle;
            else if (src > MaxAngle) src = MaxAngle;

            return (byte) (src & 0xff);
        }

        public static byte LedToByte(this int src)
        {
            if (src < MinLed) src = MinLed;
            else if (src > MaxLed) src = MaxLed;

            return (byte) (src & 0xff);
        }

        public static byte AmplitudeToByte(this int src)
        {
            if (src < MinAmplitude) src = MinAmplitude;
            else if (src > MaxAmplitude) src = MaxAmplitude;

            return (byte) (src & 0xff);
        }

        public static char ConvertServoPolarity(this bool polarity)
        {
            return polarity ? ServoPolNormal : ServoPolReverse;
        }

        public static bool ConvertServoPolarity(this char porarity)
        {
            return porarity != ServoPolReverse;
        }
    }
}
