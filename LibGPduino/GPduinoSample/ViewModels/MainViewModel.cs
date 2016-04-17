using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using LibGPduino;
using LibGPduino.Konashi;
using LibGPduinoTest.Annotations;


namespace LibGPduinoTest.ViewModels
{
    public class TurnModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is ETurnMode)) return null;

            var mode = (ETurnMode) value;

            switch (mode)
            {
                case ETurnMode.RearFixed:
                    return "Rear Fixed";
                case ETurnMode.CommonPhase:
                    return "Common Phase";
                case ETurnMode.ReversePhase:
                    return "Reverse Phase";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    internal class MainViewModel : INotifyPropertyChanged
    {
        #region const value

        private const int DataUnit = 25;
        private const int SpeedUnit = DataUnit;
        private const int SpeedMax = 125;
        private const int TurnUnit = DataUnit;
        private const int TurnMax = 125;

        private const string NameIsReversedServo = "IsReversedServo";
        private const string NameOffsetServo = "OffsetServo";
        private const string NameAmpServo = "AmpServo";

        private static readonly TimeSpan CommandTimerInterval = new TimeSpan(0, 0, 0, 0, 500);

        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly SolidColorBrush InactiveBrush = new SolidColorBrush(Colors.Gray);
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Colors.DeepSkyBlue);

        #endregion

        #region field

        private KonashiInfo _selectedKonashiInfo;
        private bool _isPressedKeyUp;
        private int _speed;
        private int _turnSize;
        private bool _isPressedKeyDown;
        private bool _isPressedKeyLeft;
        private bool _isPressedKeyRight;
        private bool? _driveState = null;
        private bool? _turnState = null;
        private ETurnMode _selectedTurnMode = ETurnMode.RearFixed;
        private bool _isUseRev2Cmd;
        private bool _initialized;
        private readonly bool[] _isReversedServo = new bool[3];
        private readonly int[] _offsetServo = new int[3];
        private readonly int[] _ampServo = new int[3];

        private RelayCommand _commandScan;
        private RelayCommand _commandConnect;
        private RelayCommand _commandSetOffset;
        private RelayCommand _commandSetAmp;
        private RelayCommand _commandLoadSettings;
        private RelayCommand _commandSaveSettings;

        #endregion

        #region implements INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region property

        public ObservableCollection<KonashiInfo> FoundKonashiInfos { get; } = new ObservableCollection<KonashiInfo>();

        public KonashiInfo SelectedKonashiInfos
        {
            get { return _selectedKonashiInfo; }
            set
            {
                if (Equals(value, _selectedKonashiInfo)) return;
                _selectedKonashiInfo = value;
                OnPropertyChanged();
                CommandConnect.RaiseCanExecuteChanged();
            }
        }

        public bool Initialized
        {
            get { return _initialized; }
            set
            {
                if (value == _initialized) return;
                _initialized = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUseRev3Cmd));
                CommandSetOffset.RaiseCanExecuteChanged();
                CommandSetAmp.RaiseCanExecuteChanged();
                CommandLoadSettings.RaiseCanExecuteChanged();
                CommandSaveSettings.RaiseCanExecuteChanged();
            }
        }

        public bool IsUseRev2Cmd
        {
            get { return _isUseRev2Cmd; }
            set
            {
                if (value == _isUseRev2Cmd) return;
                _isUseRev2Cmd = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUseRev3Cmd));
                CommandSetOffset.RaiseCanExecuteChanged();
                CommandSetAmp.RaiseCanExecuteChanged();
                CommandLoadSettings.RaiseCanExecuteChanged();
                CommandSaveSettings.RaiseCanExecuteChanged();
            }
        }

        public bool IsUseRev3Cmd => !IsUseRev2Cmd && Initialized;

        public bool CanSelect => Manager == null;

        public bool IsConnected => Manager != null;

        #region ServoSettings

        public bool IsReversedServo0
        {
            get { return _isReversedServo[0]; }
            set
            {
                if (value == _isReversedServo[0]) return;
                _isReversedServo[0] = value;
                OnPropertyChanged();
                UpdateServoPol(value);
            }
        }

        public bool IsReversedServo1
        {
            get { return _isReversedServo[1]; }
            set
            {
                if (value == _isReversedServo[1]) return;
                _isReversedServo[1] = value;
                OnPropertyChanged();
                UpdateServoPol(value);
            }
        }

        public bool IsReversedServo2
        {
            get { return _isReversedServo[2]; }
            set
            {
                if (value == _isReversedServo[2]) return;
                _isReversedServo[2] = value;
                OnPropertyChanged();
                UpdateServoPol(value);
            }
        }

        public int OffsetServo0
        {
            get { return _offsetServo[0]; }
            set
            {
                if (value == _offsetServo[0]) return;
                _offsetServo[0] = value;
                OnPropertyChanged();
            }
        }

        public int OffsetServo1
        {
            get { return _offsetServo[1]; }
            set
            {
                if (value == _offsetServo[1]) return;
                _offsetServo[1] = value;
                OnPropertyChanged();
            }
        }

        public int OffsetServo2
        {
            get { return _offsetServo[2]; }
            set
            {
                if (value == _offsetServo[2]) return;
                _offsetServo[2] = value;
                OnPropertyChanged();
            }
        }

        public int AmpServo0
        {
            get { return _ampServo[0]; }
            set
            {
                if (value == _ampServo[0]) return;
                _ampServo[0] = value;
                OnPropertyChanged();
            }
        }

        public int AmpServo1
        {
            get { return _ampServo[1]; }
            set
            {
                if (value == _ampServo[1]) return;
                _ampServo[1] = value;
                OnPropertyChanged();
            }
        }

        public int AmpServo2
        {
            get { return _ampServo[2]; }
            set
            {
                if (value == _ampServo[2]) return;
                _ampServo[2] = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region DevieState

        public bool? DriveState
        {
            get { return _driveState; }
            set
            {
                if (value == _driveState) return;
                _driveState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpeedBrush5));
                OnPropertyChanged(nameof(SpeedBrush4));
                OnPropertyChanged(nameof(SpeedBrush3));
                OnPropertyChanged(nameof(SpeedBrush2));
                OnPropertyChanged(nameof(SpeedBrush1));
                OnPropertyChanged(nameof(SpeedBrush0));
                OnPropertyChanged(nameof(SpeedBrushN1));
                OnPropertyChanged(nameof(SpeedBrushN2));
                OnPropertyChanged(nameof(SpeedBrushN3));
                OnPropertyChanged(nameof(SpeedBrushN4));
                OnPropertyChanged(nameof(SpeedBrushN5));
            }
        }

        public int Speed
        {
            get { return _speed; }
            set
            {
                if (value == _speed) return;
                _speed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpeedBrush5));
                OnPropertyChanged(nameof(SpeedBrush4));
                OnPropertyChanged(nameof(SpeedBrush3));
                OnPropertyChanged(nameof(SpeedBrush2));
                OnPropertyChanged(nameof(SpeedBrush1));
                OnPropertyChanged(nameof(SpeedBrush0));
                OnPropertyChanged(nameof(SpeedBrushN1));
                OnPropertyChanged(nameof(SpeedBrushN2));
                OnPropertyChanged(nameof(SpeedBrushN3));
                OnPropertyChanged(nameof(SpeedBrushN4));
                OnPropertyChanged(nameof(SpeedBrushN5));
            }
        }

        public Brush SpeedBrushN5 => GetSpeedColor();
        public Brush SpeedBrushN4 => GetSpeedColor();
        public Brush SpeedBrushN3 => GetSpeedColor();
        public Brush SpeedBrushN2 => GetSpeedColor();
        public Brush SpeedBrushN1 => GetSpeedColor();
        public Brush SpeedBrush0 => GetSpeedColor();
        public Brush SpeedBrush1 => GetSpeedColor();
        public Brush SpeedBrush2 => GetSpeedColor();
        public Brush SpeedBrush3 => GetSpeedColor();
        public Brush SpeedBrush4 => GetSpeedColor();
        public Brush SpeedBrush5 => GetSpeedColor();

        #endregion

        #region Turn State

        public bool? TurnState
        {
            get { return _turnState; }
            set
            {
                if (value == _turnState) return;
                _turnState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TurnBrush5));
                OnPropertyChanged(nameof(TurnBrush4));
                OnPropertyChanged(nameof(TurnBrush3));
                OnPropertyChanged(nameof(TurnBrush2));
                OnPropertyChanged(nameof(TurnBrush1));
                OnPropertyChanged(nameof(TurnBrush0));
                OnPropertyChanged(nameof(TurnBrushN1));
                OnPropertyChanged(nameof(TurnBrushN2));
                OnPropertyChanged(nameof(TurnBrushN3));
                OnPropertyChanged(nameof(TurnBrushN4));
                OnPropertyChanged(nameof(TurnBrushN5));
            }
        }

        public int TurnSize
        {
            get { return _turnSize; }
            set
            {
                if (value == _turnSize) return;
                _turnSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TurnBrush5));
                OnPropertyChanged(nameof(TurnBrush4));
                OnPropertyChanged(nameof(TurnBrush3));
                OnPropertyChanged(nameof(TurnBrush2));
                OnPropertyChanged(nameof(TurnBrush1));
                OnPropertyChanged(nameof(TurnBrush0));
                OnPropertyChanged(nameof(TurnBrushN1));
                OnPropertyChanged(nameof(TurnBrushN2));
                OnPropertyChanged(nameof(TurnBrushN3));
                OnPropertyChanged(nameof(TurnBrushN4));
                OnPropertyChanged(nameof(TurnBrushN5));
            }
        }

        public IEnumerable<ETurnMode> TurnModes => Enum.GetValues(typeof(ETurnMode)).OfType<ETurnMode>();

        public ETurnMode SelectedTurnMode
        {
            get { return _selectedTurnMode; }
            set
            {
                if (value == _selectedTurnMode) return;
                _selectedTurnMode = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTrunModeIndex { get; set; } = 0;

        public Brush TurnBrushN5 => GetTurnColor();
        public Brush TurnBrushN4 => GetTurnColor();
        public Brush TurnBrushN3 => GetTurnColor();
        public Brush TurnBrushN2 => GetTurnColor();
        public Brush TurnBrushN1 => GetTurnColor();
        public Brush TurnBrush0 => GetTurnColor();
        public Brush TurnBrush1 => GetTurnColor();
        public Brush TurnBrush2 => GetTurnColor();
        public Brush TurnBrush3 => GetTurnColor();
        public Brush TurnBrush4 => GetTurnColor();
        public Brush TurnBrush5 => GetTurnColor();

        #endregion

        #region Keyboard State

        public bool IsPressedKeyUp
        {
            get { return _isPressedKeyUp; }
            set
            {
                if (value == _isPressedKeyUp) return;
                _isPressedKeyUp = value;
                OnPropertyChanged();
            }
        }

        public bool IsPressedKeyDown
        {
            get { return _isPressedKeyDown; }
            set
            {
                if (value == _isPressedKeyDown) return;
                _isPressedKeyDown = value;
                OnPropertyChanged();
            }
        }

        public bool IsPressedKeyLeft
        {
            get { return _isPressedKeyLeft; }
            set
            {
                if (value == _isPressedKeyLeft) return;
                _isPressedKeyLeft = value;
                OnPropertyChanged();
            }
        }

        public bool IsPressedKeyRight
        {
            get { return _isPressedKeyRight; }
            set
            {
                if (value == _isPressedKeyRight) return;
                _isPressedKeyRight = value;
                OnPropertyChanged();
            }
        }

        #endregion 

        #region private

        private static CoreDispatcher Dispacher => CoreApplication.MainView.Dispatcher;

        private GPduinoManager Manager { get; set; }

        private KonashiScanner Scanner { get; } = new KonashiScanner();

        private DispatcherTimer ScanTimer { get; } = new DispatcherTimer();

        private DispatcherTimer CommandTimer { get; } = new DispatcherTimer();

        private SemaphoreSlim SemaphoreConnection { get; } = new SemaphoreSlim(1, 1);

        #endregion

        #region command

        public RelayCommand CommandScan => _commandScan ?? (_commandScan = new RelayCommand(Scan, () => CanSelect));

        public RelayCommand CommandConnect => _commandConnect ?? (_commandConnect = new RelayCommand(SetConnection, () => SelectedKonashiInfos != null));

        public RelayCommand CommandSetOffset
            => _commandSetOffset ?? (_commandSetOffset = new RelayCommand(SetServoOffset, CanSetServoSettings));

        public RelayCommand CommandSetAmp
            => _commandSetAmp ?? (_commandSetAmp = new RelayCommand(SetServoAmp, CanSetServoSettings));

        public RelayCommand CommandLoadSettings
            =>
                _commandLoadSettings ??
                (_commandLoadSettings = new RelayCommand(LoadServoSettings, () => CanSetServoSettings("1")));

        public RelayCommand CommandSaveSettings
            =>
                _commandSaveSettings ??
                (_commandSaveSettings = new RelayCommand(SaveServoSettings, () => CanSetServoSettings("1")));

        #endregion

        #endregion

        #region constructor & destructor

        public MainViewModel()
        {
            Window.Current.CoreWindow.KeyDown += OnKeyDown;
            Window.Current.CoreWindow.KeyUp += OnKeyUp;

            Scanner.FoundKonashi += async (s, info) =>
            {
                await Dispacher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (FoundKonashiInfos.All(i => i.Address != info.Address))
                        FoundKonashiInfos.Add(info);
                });
            };

            ScanTimer.Interval = new TimeSpan(0, 0, 10);
            ScanTimer.Tick += (s, e) =>
            {
                if (Scanner.IsScanning) ScanStop();
            };

            CommandTimer.Interval = CommandTimerInterval;
            CommandTimer.Tick += OnCommandTimerTick;
        }

        ~MainViewModel()
        {
            Window.Current.CoreWindow.KeyDown -= OnKeyDown;
            Window.Current.CoreWindow.KeyUp -= OnKeyUp;
            
            Manager?.Dispose();
        }

        #endregion

        #region method

        #region scan konashi

        private void Scan()
        {
            if (Scanner.IsScanning)
            {
                ScanStop();
            }
            else
            {
                ScanStart();
            }
        }

        private void ScanStart()
        {
            if (IsConnected)
            {
                Disconnect();
            }

            FoundKonashiInfos.Clear();

            Scanner.StartScan();
            ScanTimer.Start();
        }

        private void ScanStop()
        {
            ScanTimer.Stop();
            Scanner.StopScan();
        }

        #endregion

        #region connect/disconnect konashi

        private void SetConnection()
        {
            if (Manager != null)
            {
                Disconnect();
            }
            else if (SelectedKonashiInfos != null)
            {
                Connect();
            }

            CommandScan.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedTurnMode));
        }

        private async void Connect()
        {
            if (SelectedKonashiInfos == null) return;

            await SemaphoreConnection.WaitAsync();
            Manager = new GPduinoManager();
            Manager.LoadedServoSettings += OnLoadedServoSettings;
            OnPropertyChanged(nameof(CanSelect));
            await Manager.Init(SelectedKonashiInfos.Konashi, SelectedKonashiInfos.Battery);
            OnPropertyChanged(nameof(IsConnected));
            CommandTimer.Start();
            SemaphoreConnection.Release();
            Initialized = true;
            OnPropertyChanged(nameof(SpeedBrush0));
            OnPropertyChanged(nameof(TurnBrush0));
        }

        private async void Disconnect()
        {
            Initialized = false;
            await SemaphoreConnection.WaitAsync();
            CommandTimer.Stop();
            Manager?.Dispose();
            Manager = null;
            OnPropertyChanged(nameof(CanSelect));
            OnPropertyChanged(nameof(IsConnected));
            SemaphoreConnection.Release();
            OnPropertyChanged(nameof(SpeedBrush0));
            OnPropertyChanged(nameof(TurnBrush0));
        }

        #endregion

        #region GPduino event

        private async void OnLoadedServoSettings(object sender, EventArgs e)
        {   
            for (var i = 0; i < Manager.ServoSetting.Length; i++)
            {
                _isReversedServo[i] = !Manager.ServoSetting[i].Porarity;
                _offsetServo[i] = Manager.ServoSetting[i].Offset;
                _ampServo[i] = Manager.ServoSetting[i].Amplitude;
                await Dispacher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    OnPropertyChanged(NameIsReversedServo + i);
                    OnPropertyChanged(NameOffsetServo + i);
                    OnPropertyChanged(NameAmpServo + i);
                });
            }
        }

        #endregion

        #region Keyboard event

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsConnected) return;
            
            switch (e.VirtualKey)
            {
                case VirtualKey.Up:
                    if (!IsPressedKeyUp)
                    {
                        IsPressedKeyUp = true;
                        SetDriveCommand(true);
                    }
                    break;
                case VirtualKey.Down:
                    if (!IsPressedKeyDown)
                    {
                        IsPressedKeyDown = true;
                        SetDriveCommand(false);
                    }
                    break;
                case VirtualKey.Left:
                    if (!IsPressedKeyLeft)
                    {
                        IsPressedKeyLeft = true;
                        SetTurnCommand(true);
                    }
                    break;
                case VirtualKey.Right:
                    if (!IsPressedKeyRight)
                    {
                        IsPressedKeyRight = true;
                        SetTurnCommand(false);
                    }
                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (!IsConnected) return;

            switch (e.VirtualKey)
            {
                case VirtualKey.A:
                    if (DriveState != null && Speed < SpeedMax)
                    {
                        Speed += SpeedUnit;
                        SetDriveCommand(DriveState);
                    }
                    break;
                case VirtualKey.S:
                    if (DriveState != null && Speed > SpeedUnit)
                    {
                        Speed -= SpeedUnit;
                        SetDriveCommand(DriveState);
                    }
                    break;
                case VirtualKey.D:
                    if (TurnState != null && TurnSize < TurnMax)
                    {
                        TurnSize += TurnUnit;
                        SetTurnCommand(TurnState);
                    }
                    break;
                case VirtualKey.F:
                    if (TurnState != null && TurnSize > TurnUnit)
                    {
                        TurnSize -= TurnUnit;
                        SetTurnCommand(TurnState);
                    }
                    break;
                case VirtualKey.Up:
                    if (DriveState == true)
                    {
                        SetDriveCommand(IsPressedKeyDown ? (bool?)false : null);
                    }
                    IsPressedKeyUp = false;
                    break;
                case VirtualKey.Down:
                    if (DriveState == false)
                    {
                        SetDriveCommand(IsPressedKeyUp ? (bool?)true : null);
                    }
                    IsPressedKeyDown = false;
                    break;
                case VirtualKey.Left:
                    if (TurnState == true)
                    {
                        SetTurnCommand(IsPressedKeyRight ? (bool?)false : null);
                    }
                    IsPressedKeyLeft = false;
                    break;
                case VirtualKey.Right:
                    if (TurnState == false)
                    {
                        SetTurnCommand(IsPressedKeyLeft ? (bool?)true : null);
                    }
                    IsPressedKeyRight = false;
                    break;
            }
        }

        #endregion

        #region send drive/turn command

        private async void SetDriveCommand(bool? next)
        {
            if (Manager == null) return;

            await SemaphoreConnection.WaitAsync();

            if (next == null)
            {
                Speed = 0;
            }
            else if (next != DriveState)
            {
                Speed = SpeedUnit;
            }

            DriveState = next;

            var speed = 0;
            if (DriveState == true) speed = Speed;
            else if (DriveState == false) speed = 0 - Speed;
            await Manager.SendDriveCommand(speed);
            SemaphoreConnection.Release();
        }

        private async void SetTurnCommand(bool? next)
        {
            if (Manager == null) return;

            await SemaphoreConnection.WaitAsync();

            if (next == null)
            {
                TurnSize = 0;
            }
            else if (next != TurnState)
            {
                TurnSize = TurnUnit;
            }

            TurnState = next;

            var turn = 0;
            if (TurnState == true) turn = TurnSize;
            else if (TurnState == false) turn = 0 - TurnSize;
            if (IsUseRev2Cmd)
            {
                await Manager.SendTurnCommandRev2(turn);
            }
            else
            {
                await Manager.SendTurnCommand(turn, SelectedTurnMode);
            }
            SemaphoreConnection.Release();
        }

        #endregion

        #region interval timer event to send command

        private async void OnCommandTimerTick(object sender, object value)
        {
            await Dispacher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (IsPressedKeyUp && (DriveState == true || !IsPressedKeyDown))
                {
                    SetDriveCommand(true);
                }
                else if (IsPressedKeyDown && (DriveState == false || !IsPressedKeyUp))
                {
                    SetDriveCommand(false);
                }

                if (IsPressedKeyLeft && (TurnState == true || !IsPressedKeyRight))
                {
                    SetTurnCommand(true);
                }
                else if (IsPressedKeyRight && (TurnState == false || !IsPressedKeyLeft))
                {
                    SetTurnCommand(false);
                }
            });
        }

        #endregion

        #region update drive/turn state

        private Brush GetSpeedColor([CallerMemberName] string name = null)
        {
            return GetStateColor(name, "SpeedBrush", DriveState, Speed);
        }

        private Brush GetTurnColor([CallerMemberName] string name = null)
        {
            return GetStateColor(name, "TurnBrush", TurnState, TurnSize);
        }

        private Brush GetStateColor(string name, string prefix, bool? mode, int size)
        {
            if (Manager == null) return InactiveBrush;

            if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix)) return TransparentBrush;

            var valueStr = name.Replace(prefix, "").Replace("N", "-");

            int value;

            if (!int.TryParse(valueStr, out value)) return TransparentBrush;

            size = size / DataUnit;

            if (mode == false) size *= -1;
            else if (mode == null) size = 0;

            if (size * value < 0) return InactiveBrush;
            if (Math.Abs(size) < Math.Abs(value)) return InactiveBrush;

            return ActiveBrush;
        }

        #endregion

        #region Update servo settings

        private async void UpdateServoPol(bool value, [CallerMemberName] string name = null)
        {
            if (!Initialized || string.IsNullOrEmpty(name) || !name.StartsWith(NameIsReversedServo))
            {
                return;
            }

            name = name.Replace(NameIsReversedServo, "");

            int idx;

            if (!int.TryParse(name, out idx)) return;
            
            if (idx > 0 && IsUseRev2Cmd) return;

            if (IsUseRev2Cmd && idx == 0)
            {
                await Manager.SendServoSetPolarityRev2(value);
            }
            else
            {
                await Manager.SendServoSetPolarity(idx, !value);
            }

        }

        private int ConvertToServoIndex(object parameter)
        {
            var idxStr = parameter as string;

            if (string.IsNullOrEmpty(idxStr)) return -1;

            int idx;
            if (!int.TryParse(idxStr, out idx)) return -1;

            return idx;
        }

        private bool CanSetServoSettings(object parameter)
        {
            if (!Initialized) return false;

            var idx = ConvertToServoIndex(parameter);

            return idx == 0 || (idx > 0 && IsUseRev3Cmd);
        }

        private async void SetServoOffset(object parameter)
        {
            if (!Initialized) return;
            var idx = ConvertToServoIndex(parameter);

            if (idx < 0 || _offsetServo.Length <= idx) return;

            if (IsUseRev2Cmd && idx == 0)
            {
                await Manager.SendServoSetOffsetRev2(_offsetServo[idx]);
            }
            else
            {
                await Manager.SendServoSetOffset(idx, _offsetServo[idx]);
            }
        }

        private async void SetServoAmp(object parameter)
        {
            if (!Initialized) return;
            var idx = ConvertToServoIndex(parameter);

            if (idx < 0 || _ampServo.Length <= idx) return;

            if (IsUseRev2Cmd && idx == 0)
            {
                await Manager.SendServoSetAmplitudeRev2(_ampServo[idx]);
            }
            else
            {
                await Manager.SendServoSetAmplitude(idx, _ampServo[idx]);
            }
        }

        private async void LoadServoSettings()
        {
            if (!Initialized) return;

            await Manager.LoadServoSettings();
        }

        private async void SaveServoSettings()
        {
            if (!Initialized) return;

            await Manager.SaveServoSettings();
        }

        #endregion

        #endregion
    }
}
