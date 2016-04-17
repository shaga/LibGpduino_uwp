using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Markup;
using LibGPduino.Konashi;

namespace LibGPduino
{
    public class GPduinoManager : KonashiManager
    {
        private const char UartStx = '#';
        private const char UartEtx = '$';

        private const string DriveCmdFmt = "#D{0:X2}$";
        private const string TurnCmdFmtRev2 = "#T{0:X2}$";
        private const string TurnCmdFmt = "#T{0:X2}{1:X1}$";
        private const string MotorSepDriveCmdFmt = "#M{0:X1}{1:X2}$";
        private const string MotorAllDriveCmdFmt = "#MA{0:X2}{1:X2}$";
        private const string ServoSepDriveCmdFmtRev2 = "#S{0:x2}$";
        private const string ServoSepDriveCmdFmt = "#S{0:X2}{1:X1}$";
        private const string ServoSetPolFmtRev2 = "#AP{0}$";
        private const string ServoSetPolFmt = "#AP{0}{1:X1}$";
        private const string ServoSetOffsetFmtRev2 = "#AO{0:X2}$";
        private const string ServoSetOffsetFmt = "#AO{0:X2}{1:X1}$";
        private const string ServoSetAmpRev2 = "#AA{0:X2}$";
        private const string ServoSetAmp = "#AA{0:X2}{1:X1}$";
        private const string ServoSetSave = "#AS$";
        private const string ServoSetLoad = "#AL$";
        private const string LedSepDriveFmtRev2 = "#L{0:X1}{1:X2}$";
        private const string LedAllDriveFmtRev2 = "#LA{0:X2}{1:X2}$";
        private const string LedModeFmtRev2 = "#LX{0:X1}$";

        private const string RecvCmdStartBattery = "B";
        private const string RecvCmdStartServoSetting = "AL";
        private const int ServoCountRev3 = 3;
        private const int ServoSettingLen = 5;

        public double BatteryVoltage { get; private set; }

        private string ReceivedBuffer { get; set; }

        public ServoSetting[] ServoSetting { get; }

        public event EventHandler ReadBatteryVoltage;
        public event EventHandler LoadedServoSettings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GPduinoManager() : base()
        {
            ReceivedBuffer = string.Empty;

            ServoSetting = new ServoSetting[ServoCountRev3];
        }

        ~GPduinoManager()
        {
            Dispose();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="konashi">konashiサービスデバイス情報</param>
        /// <param name="battery">バッテリサービスデバイス情報</param>
        /// <returns></returns>
        public override async Task Init(DeviceInformation konashi, DeviceInformation battery)
        {
            await base.Init(konashi, battery);

            await SetUartConfig(true);

            await SetUartBaudrate(EKonashiUartBaudrate.UartRate38K4);

            ReceivedUartData += OnReceivedUartData;
        }

        /// <summary>
        /// Dコマンド送信
        /// </summary>
        /// <param name="speed">スピード</param>
        /// <returns></returns>
        public async Task SendDriveCommand(int speed)
        {
            await WriteCommand(DriveCmdFmt, speed.SpeedToByte());
        }

        /// <summary>
        /// Tコマンド送信(Rev2)
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public async Task SendTurnCommandRev2(int angle)
        {
            await WriteCommand(TurnCmdFmtRev2, angle.AngleToByte());
        }

        /// <summary>
        /// Tコマンド送信
        /// </summary>
        /// <param name="angle">角度</param>
        /// <param name="mode">モード</param>
        /// <returns></returns>
        public async Task SendTurnCommand(int angle, ETurnMode mode)
        {
            await WriteCommand(TurnCmdFmt, angle.AngleToByte(), (byte) mode);
        }

        /// <summary>
        /// Mコマンド(個別)送信
        /// </summary>
        /// <param name="idx">モータインデックス</param>
        /// <param name="speed">スピード</param>
        /// <returns></returns>
        public async Task SendMotorSepCommand(int idx, int speed)
        {
            if (idx < 0 || 1 < idx) return;

            await WriteCommand(MotorSepDriveCmdFmt, (byte) idx, speed.SpeedToByte());
        }

        /// <summary>
        /// Mコマンド(一括)送信
        /// </summary>
        /// <param name="speed1">モータ1スピード</param>
        /// <param name="speed2">モータ2スピード</param>
        /// <returns></returns>
        public async Task SendMotorAllCommand(int speed1, int speed2)
        {
            await WriteCommand(MotorAllDriveCmdFmt, speed1.SpeedToByte(), speed2.SpeedToByte());
        }

        /// <summary>
        /// Sコマンド送信(Rev2)
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public async Task SendServoSepCommandRev2(int angle)
        {
            await WriteCommand(ServoSepDriveCmdFmtRev2, angle.AngleToByte());
        }

        /// <summary>
        /// Sコマンド送信
        /// </summary>
        /// <param name="idx">サーボインデックス</param>
        /// <param name="angle">角度</param>
        /// <returns></returns>
        public async Task SendServoSepCommand(int idx, int angle)
        {
            if (idx < 0 || ServoCountRev3 <= idx) return;
            await WriteCommand(ServoSepDriveCmdFmt, angle.AngleToByte(), (byte) idx);
        }

        /// <summary>
        /// サーボ極性設定(Rev2)
        /// </summary>
        /// <param name="polarity">true=正転/false=反転</param>
        /// <returns></returns>
        public async Task SendServoSetPolarityRev2(bool polarity)
        {
            await WriteCommand(ServoSetPolFmtRev2, polarity.ConvertServoPolarity());
        }

        /// <summary>
        /// サーボ極性設定
        /// </summary>
        /// <param name="idx">サーボインデックス</param>
        /// <param name="polarity">true=正転/false=反転</param>
        /// <returns></returns>
        public async Task SendServoSetPolarity(int idx, bool polarity)
        {
            if (idx < 0 || ServoCountRev3 <= idx) return;
            await WriteCommand(ServoSetPolFmt, polarity.ConvertServoPolarity(), idx);
        }

        /// <summary>
        /// サーボオフセット設定(Rev2)
        /// </summary>
        /// <param name="offset">オフセット値</param>
        /// <returns></returns>
        public async Task SendServoSetOffsetRev2(int offset)
        {

            await WriteCommand(ServoSetOffsetFmtRev2, offset.AngleToByte());
        }

        /// <summary>
        /// サーボオフセット設定
        /// </summary>
        /// <param name="idx">サーボインデックス</param>
        /// <param name="offset">オフセット値</param>
        /// <returns></returns>
        public async Task SendServoSetOffset(int idx, int offset)
        {
            if (idx < 0 || ServoCountRev3 <= idx) return;

            await WriteCommand(ServoSetOffsetFmt, offset.AngleToByte(), idx);
        }

        /// <summary>
        /// サーボ振幅設定(Rev2)
        /// </summary>
        /// <param name="amp">サーボ振幅</param>
        /// <returns></returns>
        public async Task SendServoSetAmplitudeRev2(int amp)
        {
            await WriteCommand(ServoSetAmpRev2, amp.AmplitudeToByte());
        }

        /// <summary>
        /// サーボ振幅設定
        /// </summary>
        /// <param name="idx">サーボインデックス</param>
        /// <param name="amp">サーボ振幅</param>
        /// <returns></returns>
        public async Task SendServoSetAmplitude(int idx, int amp)
        {
            if (idx < 0 || ServoCountRev3 <= idx) return;

            await WriteCommand(ServoSetAmp, amp, idx);

        }

        /// <summary>
        /// サーボ設定書込
        /// </summary>
        /// <returns></returns>
        public async Task SaveServoSettings()
        {
            await WriteCommand(ServoSetSave);
        }

        /// <summary>
        /// サーボ設定読込
        /// </summary>
        /// <returns></returns>
        public async Task LoadServoSettings()
        {
            await WriteCommand(ServoSetLoad);
        }

        /// <summary>
        /// Lコマンド(個別)送信
        /// </summary>
        /// <param name="idx">LEDインデックス</param>
        /// <param name="bright">明るさ</param>
        /// <returns></returns>
        public async Task SendLedSepCommandRev2(int idx, int bright)
        {
            if (idx < 0 || 1 < idx) return;

            await WriteCommand(LedSepDriveFmtRev2, (idx), bright.LedToByte());
        }

        /// <summary>
        /// Lコマンド(一括)送信
        /// </summary>
        /// <param name="bright1">LED1明るさ</param>
        /// <param name="bright2">LED2明るさ</param>
        /// <returns></returns>
        public async Task SendLedAllCommandRev2(int bright1, int bright2)
        {
            await WriteCommand(LedAllDriveFmtRev2, bright1.LedToByte(), bright2.LedToByte());
        }

        /// <summary>
        /// LEDモード設定
        /// </summary>
        /// <param name="mode">モード</param>
        /// <returns></returns>
        public async Task SendLedModeCommandRev2(ELedMode mode)
        {
            if (!mode.IsDefined<ELedMode>()) return;

            await WriteCommand(LedModeFmtRev2, (byte) mode);
        }

        /// <summary>
        /// コマンド送信
        /// </summary>
        /// <param name="fmt">コマンドフォーマット文字列</param>
        /// <param name="parameters">パラメータ</param>
        /// <returns></returns>
        private async Task WriteCommand(string fmt, params object[] parameters)
        {
            var value = string.Format(fmt, parameters);

            await WriteUartTx(value);
        }

        /// <summary>
        /// UART受信イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceivedUartData(object sender, KonashiUartReceivedEventArgs e)
        {
            ReceivedBuffer += Encoding.UTF8.GetString(e.Data);

            var posStx = ReceivedBuffer.LastIndexOf(UartStx);
            var posEtx = ReceivedBuffer.LastIndexOf(UartEtx);

            if (posStx < 0 || (posEtx > 0 && posEtx - posStx < 2))
            {
                // STXなし
                ReceivedBuffer = string.Empty;
                return;
            }

            if (posStx > posEtx)
            {
                // STX以降を次の受信に残す
                ReceivedBuffer = ReceivedBuffer.Substring(posStx);
                return;
            }

            var recvCommand = ReceivedBuffer.Substring(posStx + 1, posEtx - posStx - 1);

            if (recvCommand.StartsWith(RecvCmdStartBattery))
            {
                // バッテリ情報受信
                UpdateBatteryInfo(recvCommand.Substring(RecvCmdStartBattery.Length));
            }
            else if (recvCommand.StartsWith(RecvCmdStartServoSetting))
            {
                // サーボ情報受信
                UpdateServoSettings(recvCommand.Substring(RecvCmdStartServoSetting.Length));
            }

            ReceivedBuffer = string.Empty;
        }

        private void UpdateBatteryInfo(string src)
        {
            var level = Convert.ToInt32(src, 16);
            BatteryVoltage = ((double)level / 1023) * (3.3 / 2);

            ReadBatteryVoltage?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateServoSettings(string src)
        {
            for (var i = 0; i < ServoCountRev3; i++)
            {
                var p = src[i*ServoSettingLen].ConvertServoPolarity();
                var offsetStr = src.Substring(i*ServoSettingLen + 1, 2);
                var offset = Convert.ToSByte(offsetStr, 16);
                var amplitudeStr = src.Substring(i*ServoSettingLen + 3, 2);
                var amplitude = Convert.ToByte(amplitudeStr, 16);

                ServoSetting[i] = new ServoSetting(p, offset, amplitude);
            }

            LoadedServoSettings?.Invoke(this, EventArgs.Empty);
        }
    }
}
