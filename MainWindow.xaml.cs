using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SMW_Data.View;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.IO.Ports;
using System.Windows.Media.Imaging;
using System.Windows.Forms.VisualStyles;

namespace SMW_Data
{
    public partial class MainWindow : Window
    {
        public SolidColorBrush CurrentBackgroundColor { get; set; }
        public SolidColorBrush CurrentTextColor { get; set; }
        public FontFamily CurrentFontTitle { get; set; } = new FontFamily("Segoe UI");
        public FontFamily CurrentFontAuthor { get; set; } = new FontFamily("Segoe UI");
        public int SelectedLevelAccuracyIndex { get; set; } = 0;
        public int SelectedTotalAccuracyIndex { get; set; } = 0;
        public int SelectedDeathImageIndex { get; set; } = 2;

        public BitmapImage NewDeathImage;

        public string LevelAccuracy;
        public string TotalAccuracy;

        private static int TotalDeathCount;
        private static int LevelDeathCount;
        public bool DeathState;

        private static int SwitchesActivated = 0;
        public bool GreenSwitchActivated = false;
        public bool YellowSwitchActivated = false;
        public bool BlueSwitchActivated = false;
        public bool RedSwitchActivated = false;

        public int ExitCountCurrent;

        private bool isFirstMessageReceived = false;
        private List<string> receivedDataBuffer = new List<string>();
        private string message;
        private string fullMessage;
        private int expectedDataLength = 14;

        static WebSocket ws;
        private DispatcherTimer timerSNES;
        private DispatcherTimer timer;
        private DateTime startTimeLevel;
        private DateTime startTimeTotal;
        private TimeSpan currentTimeLevel = TimeSpan.Zero;
        private TimeSpan currentTimeLastLevel = TimeSpan.Zero;
        private TimeSpan currentTimeTotal = TimeSpan.Zero;
        private TimeSpan elapsedTotal = TimeSpan.Zero;
        private TimeSpan elapsedLevel = TimeSpan.Zero;

        static private string device = null;
        static private String[] devices = null;
        private TaskCompletionSource<bool> deviceListReceived = new TaskCompletionSource<bool>();
        private bool deviceListProcessed;

        public string MemoryAddressValue_DeathCheck;
        static readonly int MemoryAddress_DeathCheck = 0x7E0071;
        static readonly int adjMemoryAddress_DeathCheck = 0xF50000 + (MemoryAddress_DeathCheck - 0x7E0000);
        static readonly string AdjustedMemoryAddress_DeathCheck = adjMemoryAddress_DeathCheck.ToString("X");

        public string MemoryAddressValue_ExitCounter;
        static readonly int MemoryAddress_ExitCounter = 0x7E1F2E;
        static readonly int adjMemoryAddress_ExitCounter = 0xF50000 + (MemoryAddress_ExitCounter - 0x7E0000);
        static readonly string AdjustedMemoryAddress_ExitCounter = adjMemoryAddress_ExitCounter.ToString("X");
        private int previousExitCounterValue = 0;

        public string MemoryAddressValue_SwitchesActivated;
        static readonly int MemoryAddress_SwitchesActivated = 0x7E1F27;
        static readonly int adjMemoryAddress_SwitchesActivated = 0xF50000 + (MemoryAddress_SwitchesActivated - 0x7E0000);
        static readonly string AdjustedMemoryAddress_SwitchesActivated = adjMemoryAddress_SwitchesActivated.ToString("X");

        public string MemoryAddressValue_InGame;
        static readonly int MemoryAddress_InGame = 0x7E1F15;
        static readonly int adjMemoryAddress_InGame = 0xF50000 + (MemoryAddress_InGame - 0x7E0000);
        static readonly string AdjustedMemoryAddress_InGame = adjMemoryAddress_InGame.ToString("X");

        public MainWindow()
        {
            InitializeComponent();
            CurrentBackgroundColor = (SolidColorBrush)GridMain.Background;
            CurrentTextColor = (SolidColorBrush)Label_LevelDeathCount.Foreground;

            CurrentFontTitle = (FontFamily)Label_Hack.FontFamily;
            CurrentFontAuthor = (FontFamily)Label_Creator.FontFamily;

            LevelAccuracy = "Milliseconds (0.00)";
            TotalAccuracy = "Milliseconds (0.00)";

            Image_MarioDeath1.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMW.png"));
            Image_MarioDeath2.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMW.png"));

            TextBlock_SwitchCount.Visibility = Visibility.Hidden;
            InitializeWebSocket();
        }

        private async void InitializeWebSocket()
        {
            if (ws != null)
            {
                ws.Close();
                ws = null;
            }

            ws = new WebSocket("ws://localhost:8080");

            ws.OnOpen += async (sender, e) =>
            {
                var deviceListRequest = new
                {
                    Opcode = "DeviceList",
                    Space = "SNES"
                };
                ws.Send(JsonConvert.SerializeObject(deviceListRequest));

                deviceListProcessed = await deviceListReceived.Task;
                if (deviceListProcessed && !string.IsNullOrEmpty(device))
                {
                    AttachDevice();
                    _ = Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        TextBlock_Connection.Text = "Connected to: " + device;
                        TextBlock_Footer.Text = "Connected to WebSocket";
                    });
                }
                else
                {
                    _ = Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        TextBlock_Connection.Text = "No Device Found";
                        TextBlock_Footer.Text = "Not Connected to WebSocket";
                    });
                }

                timerSNES = new DispatcherTimer();
                timerSNES.Interval = TimeSpan.FromMilliseconds(16); // 16ms is approximately 60fps
                timerSNES.Tick += Timer_Tick;
                timerSNES.Start();
            };

            ws.OnMessage += (sender, e) =>
            {
                if (isFirstMessageReceived)
                {
                    message = BitConverter.ToString(e.RawData).Replace("-", "");
                    receivedDataBuffer.Add(message);
                    fullMessage = string.Join("", receivedDataBuffer);

                    if (fullMessage.Length < expectedDataLength)
                    {
                        return;
                    }

                    if (fullMessage.Length == expectedDataLength)
                    {
                        ProcessMemoryAddressResponse_DeathCheck(fullMessage);
                        ProcessMemoryAddressResponse_ExitCounter(fullMessage);
                        ProcessMemoryAddressResponse_InGame(fullMessage);
                        ProcessMemoryAddressResponse_Switches(fullMessage);
                        receivedDataBuffer.Clear();
                    }
                }
                else
                {
                    isFirstMessageReceived = true;
                    var messageCheckForDevice = JsonConvert.DeserializeObject<dynamic>(e.Data);
                    devices = messageCheckForDevice.Results.ToObject<string[]>();
                    if (devices != null && devices.Length > 0)
                    {
                        device = devices[0].ToString();
                        deviceListReceived.SetResult(true);
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            TextBlock_Connection.Text = "Connected to: " + device;
                        });
                    }
                    else
                    {
                        deviceListReceived.SetResult(false);
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            TextBlock_Connection.Text = "No Device Found";
                            TextBlock_Footer.Text = "Not Connected to WebSocket";
                        });
                    }
                }
            };

            ws.OnError += (sender, e) =>
            {
                //MessageBox.Show("WebSocket error: " + e.Message);
            };

            ws.OnClose += (sender, e) =>
            {
                if (e.Code == (ushort)CloseStatusCode.Normal)
                {
                    //MessageBox.Show("WebSocket closed normally.");
                }
                else
                {
                    //MessageBox.Show($"WebSocket closed with code {e.Code}: {e.Reason}");
                }
            };

            try
            {
                ws.Connect();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("WebSocket connection failed: " + ex.Message);
                TextBlock_Footer.Text = "Not Connected to WebSocket";
                TextBlock_Connection.Text = "No Device Found";
            }
        }

        public void AttachDevice()
        {
            if (!string.IsNullOrEmpty(device))
            {
            var attachRequest = new
                {
                    Opcode = "Attach",
                    Space = "SNES",
                    Operands = new[] { device }
                };
                ws.Send(JsonConvert.SerializeObject(attachRequest));
            }
            else
            {
                //Console.WriteLine("Device not available yet.");
            }
        }

        private void Button_Connect_Click(object sender, RoutedEventArgs e)
        {
            isFirstMessageReceived = false;
            deviceListProcessed = false;
            devices = null;
            device = null;
            timerSNES.Stop();
            deviceListReceived = new TaskCompletionSource<bool>();
            InitializeWebSocket();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                SendGetAddressRequest(ws,
                    AdjustedMemoryAddress_SwitchesActivated,
                    AdjustedMemoryAddress_DeathCheck,
                    AdjustedMemoryAddress_ExitCounter,
                    AdjustedMemoryAddress_InGame);
            }
            catch
            {
                TextBlock_Footer.Text = "Not Connected to WebSocket";
                TextBlock_Connection.Text = "No Device Found";
            }
        }

        private static void SendGetAddressRequest(WebSocket ws,
            string MemoryAddressValue_SwitchesActivated,
            string MemoryAddressValue_DeathCheck,
            string MemoryAddressValue_ExitCounter,
            string MemoryAddressValue_InGame)
        {
            var getAddressRequest = new
            {
                Opcode = "GetAddress",
                Space = "SNES",
                Operands = new[] {
                    MemoryAddressValue_SwitchesActivated, "4",
                    MemoryAddressValue_DeathCheck, "1",
                    MemoryAddressValue_ExitCounter , "1",
                    MemoryAddressValue_InGame, "1"
                }
            };
            ws.Send(JsonConvert.SerializeObject(getAddressRequest));
        }

        private void ProcessMemoryAddressResponse_Switches(string rawData)
        {
            //MessageBox.Show(BitConverter.ToString(rawData).Replace("-", "").Substring(12, 2));
            string MemoryAddressValue_GreenSwitchActivated = rawData.Substring(0, 2);
            if (MemoryAddressValue_GreenSwitchActivated != "00" && GreenSwitchActivated == false)
            {
                GreenSwitchActivated = true;
                SwitchesActivated++;
            }

            string MemoryAddressValue_YellowSwitchActivated = rawData.Substring(2, 2);
            if (MemoryAddressValue_YellowSwitchActivated != "00" && YellowSwitchActivated == false)
            {
                YellowSwitchActivated = true;
                SwitchesActivated++;
            }

            string MemoryAddressValue_BlueSwitchActivated = rawData.Substring(4, 2);
            if (MemoryAddressValue_BlueSwitchActivated != "00" && BlueSwitchActivated == false)
            {
                BlueSwitchActivated = true;
                SwitchesActivated++;
            }

            string MemoryAddressValue_RedSwitchActivated = rawData.Substring(6, 2);
            if (MemoryAddressValue_RedSwitchActivated != "00" && RedSwitchActivated == false)
            {
                RedSwitchActivated = true;
                SwitchesActivated++;
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBlock_SwitchCount.Text = "+" + SwitchesActivated.ToString();
            });
        }

        private void ProcessMemoryAddressResponse_DeathCheck(string rawData)
        {
            string MemoryAddressValue_DeathCheck = rawData.Substring(8, 2);
            if ((MemoryAddressValue_DeathCheck != "09") && (DeathState == true))
            {
                DeathState = false;
            }

            if ((MemoryAddressValue_DeathCheck == "09") && (DeathState == false))
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
                    LevelDeathCount++;
                    TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();

                    TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
                    TotalDeathCount++;
                    TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
                    CounterRange();
                });
                DeathState = true;
            }
        }

        private void ProcessMemoryAddressResponse_ExitCounter(string rawData)
        {
            string MemoryAddressValue_ExitCounter = rawData.Substring(10, 2);
            ExitCountCurrent = Convert.ToInt32(MemoryAddressValue_ExitCounter, 16);

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBlock_ExitCountCurrent.Text = ExitCountCurrent.ToString();
            });

            if (ExitCountCurrent != previousExitCounterValue)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
                    LevelDeathCount = 0;
                    TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
                    CounterRange();

                    TextBlock_LastLevelTime.Text = TextBlock_LevelTime.Text;

                });

                previousExitCounterValue = ExitCountCurrent;
                startTimeLevel = DateTime.Now;
            }
        }

        private void ProcessMemoryAddressResponse_InGame(string rawData)
        {
            string MemoryAddressValue_InGame = rawData.Substring(12, 2);
            if (MemoryAddressValue_InGame != "02")
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    TextBlock_ExitCountCurrent.Text = "??";
                    TextBlock_SwitchCount.Text = "+0";
                    SwitchesActivated = 0;
                    GreenSwitchActivated = false;
                    YellowSwitchActivated = false;
                    BlueSwitchActivated = false;
                    RedSwitchActivated = false;
                });
            }
        }

        private void CounterRange()
        {
            if (Int32.Parse(TextBlock_LevelDeathCount.Text) >= 999999)
            {
                TextBlock_LevelDeathCount.Text = "999999";
            }
            else if (Int32.Parse(TextBlock_LevelDeathCount.Text) <= 0)
            {
                TextBlock_LevelDeathCount.Text = "0";
            }

            if (Int32.Parse(TextBlock_TotalDeathCount.Text) >= 999999)
            {
                TextBlock_TotalDeathCount.Text = "999999";
            }
            else if (Int32.Parse(TextBlock_TotalDeathCount.Text) <= 0)
            {
                TextBlock_TotalDeathCount.Text = "0";
            }
        }

        private void MenuItem_Click_Colors(object sender, RoutedEventArgs e)
        {
            ColorWindow colorWindow = new(this);
            colorWindow.ShowDialog();
            if (colorWindow.ColorOK)
            {
                GridMain.Background = colorWindow.ChangeBackgroundColor;
                GridHackName.Background = colorWindow.ChangeBackgroundColor;
                GridCreators.Background = colorWindow.ChangeBackgroundColor;
                Label_Hack.Foreground = colorWindow.ChangeTextColor;
                Label_Creator.Foreground = colorWindow.ChangeTextColor;
                Label_LevelDeathCount.Foreground = colorWindow.ChangeTextColor;
                Label_TotalDeathCount.Foreground = colorWindow.ChangeTextColor;
                TextBlock_LevelDeathCount.Foreground = colorWindow.ChangeTextColor;
                TextBlock_TotalDeathCount.Foreground = colorWindow.ChangeTextColor;
                Label_ExitCount.Foreground = colorWindow.ChangeTextColor;
                TextBlock_ExitCountCurrent.Foreground = colorWindow.ChangeTextColor;
                TextBlock_ExitCountSlash.Foreground = colorWindow.ChangeTextColor;
                TextBlock_ExitCountTotal.Foreground = colorWindow.ChangeTextColor;
                TextBlock_SwitchCount.Foreground = colorWindow.ChangeTextColor;
                Label_LevelTime.Foreground = colorWindow.ChangeTextColor;
                Label_LastLevelTime.Foreground = colorWindow.ChangeTextColor;
                Label_TotalTime.Foreground = colorWindow.ChangeTextColor;
                TextBlock_LevelTime.Foreground = colorWindow.ChangeTextColor;
                TextBlock_LastLevelTime.Foreground = colorWindow.ChangeTextColor;
                TextBlock_TotalTime.Foreground = colorWindow.ChangeTextColor;

                CurrentBackgroundColor = (SolidColorBrush)colorWindow.NewBackgroundColor;
                CurrentTextColor = (SolidColorBrush)colorWindow.NewTextColor;
            }
        }

        private void MenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_SetLevel_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_LevelDeathCount.Text = TextBox_LevelDeaths.Text;
        }

        private void Button_SetTotal_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_TotalDeathCount.Text = TextBox_TotalDeaths.Text;
        }

        private void TextBox_LevelDeaths_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void TextBox_TotalDeaths_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void ButtonLevelPlus_Click(object sender, RoutedEventArgs e)
        {
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount++;
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void ButtonLevelMinus_Click(object sender, RoutedEventArgs e)
        {
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount--;
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void ButtonLevelZero_Click(object sender, RoutedEventArgs e)
        {
            LevelDeathCount = 0;
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
        }

        private void ButtonTotalPlus_Click(object sender, RoutedEventArgs e)
        {
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount++;
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
        }

        private void ButtonTotalMinus_Click(object sender, RoutedEventArgs e)
        {
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount--;
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
        }

        private void ButtonTotalZero_Click(object sender, RoutedEventArgs e)
        {
            TotalDeathCount = 0;
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
        }

        private void Button_TimersStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (Button_TimersStartStop.Content.ToString().Contains("Start"))
            {
                Button_TimersStartStop.Content = "Stop Timer";
                SolidColorBrush redColor = new SolidColorBrush(Colors.Red);
                Button_TimersStartStop.Background = redColor;

                Button_TimerResetAll.Visibility = Visibility.Hidden;
                Button_TimerResetLevel.Visibility = Visibility.Hidden;
                Button_TimerResetLastLevel.Visibility = Visibility.Hidden;
                Button_TimerResetTotal.Visibility = Visibility.Hidden;
                Button_TimersSetAll.Visibility = Visibility.Hidden;
                Button_TimersSetLevel.Visibility = Visibility.Hidden;
                Button_TimersSetLastLevel.Visibility = Visibility.Hidden;
                Button_TimersSetTotal.Visibility = Visibility.Hidden;
                Label_TimeUnits.Visibility = Visibility.Hidden;
                Label_Level.Visibility = Visibility.Hidden;
                Label_LastLevel.Visibility = Visibility.Hidden;
                Label_Total.Visibility = Visibility.Hidden;
                TextBox_LevelHours.Visibility = Visibility.Hidden;
                TextBox_LevelMinutes.Visibility = Visibility.Hidden;
                TextBox_LevelSeconds.Visibility = Visibility.Hidden;
                TextBox_LevelMilliseconds.Visibility = Visibility.Hidden;
                TextBox_LastLevelHours.Visibility = Visibility.Hidden;
                TextBox_LastLevelMinutes.Visibility = Visibility.Hidden;
                TextBox_LastLevelSeconds.Visibility = Visibility.Hidden;
                TextBox_LastLevelMilliseconds.Visibility = Visibility.Hidden;
                TextBox_TotalHours.Visibility = Visibility.Hidden;
                TextBox_TotalMinutes.Visibility = Visibility.Hidden;
                TextBox_TotalSeconds.Visibility = Visibility.Hidden;
                TextBox_TotalMilliseconds.Visibility = Visibility.Hidden;

                GetCurrentTimeTotal();
                GetCurrentTimeLevel();

                if (currentTimeTotal == TimeSpan.Zero)
                {
                    startTimeTotal = DateTime.Now;
                    startTimeLevel = startTimeTotal;
                }
                else
                {
                    startTimeTotal = DateTime.Now - currentTimeTotal;
                    startTimeLevel = DateTime.Now - currentTimeLevel;
                }

                elapsedLevel = DateTime.Now - startTimeLevel;
                elapsedTotal = DateTime.Now - startTimeTotal;

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1);
                timer.Tick += Timer_Main_Tick;
                timer.Start();
            }
            else
            {
                Button_TimersStartStop.Content = "Start Timer";
                SolidColorBrush limeColor = new SolidColorBrush(Colors.Lime);
                Button_TimersStartStop.Background = limeColor;

                Button_TimerResetAll.Visibility = Visibility.Visible;
                Button_TimerResetLevel.Visibility = Visibility.Visible;
                Button_TimerResetLastLevel.Visibility = Visibility.Visible;
                Button_TimerResetTotal.Visibility = Visibility.Visible;
                Button_TimersSetAll.Visibility = Visibility.Visible;
                Button_TimersSetLevel.Visibility = Visibility.Visible;
                Button_TimersSetLastLevel.Visibility = Visibility.Visible;
                Button_TimersSetTotal.Visibility = Visibility.Visible;
                Label_TimeUnits.Visibility = Visibility.Visible;
                Label_Level.Visibility = Visibility.Visible;
                Label_LastLevel.Visibility = Visibility.Visible;
                Label_Total.Visibility = Visibility.Visible;
                TextBox_LevelHours.Visibility = Visibility.Visible;
                TextBox_LevelMinutes.Visibility = Visibility.Visible;
                TextBox_LevelSeconds.Visibility = Visibility.Visible;
                TextBox_LevelMilliseconds.Visibility = Visibility.Visible;
                TextBox_LastLevelHours.Visibility = Visibility.Visible;
                TextBox_LastLevelMinutes.Visibility = Visibility.Visible;
                TextBox_LastLevelSeconds.Visibility = Visibility.Visible;
                TextBox_LastLevelMilliseconds.Visibility = Visibility.Visible;
                TextBox_TotalHours.Visibility = Visibility.Visible;
                TextBox_TotalMinutes.Visibility = Visibility.Visible;
                TextBox_TotalSeconds.Visibility = Visibility.Visible;
                TextBox_TotalMilliseconds.Visibility = Visibility.Visible;

                elapsedTotal = DateTime.Now - startTimeTotal;
                elapsedLevel = DateTime.Now - startTimeLevel;

                timer.Stop();

                

                //store currentTimeLevel and currentTimeTotal
                //On restart, initialize the timer with these stored values instead of resetting them
                //After changing the timer accuracy, adjust the stored times (currentTimeLevel and currentTimeTotal) to match the new accuracy level.

            }
        }

        private void Timer_Main_Tick(object sender, EventArgs e)
        {
            GetTimes();
        }

        private void GetTimes()
        {
            TimeSpan elapsedTotal = DateTime.Now - startTimeTotal;
            TimeSpan elapsedLevel = DateTime.Now - startTimeLevel;

            if (TotalAccuracy == "Milliseconds (0.00)")
            {
                if (Convert.ToInt32(elapsedTotal.ToString(@"dd")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss\.ff");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"hh")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss\.ff");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"mm")) != 0)
                {
                    int totalMinutes = (int)elapsedTotal.TotalMinutes;
                    TextBlock_TotalTime.Text = totalMinutes + ":" + elapsedTotal.ToString(@"ss\.ff");
                }
                else
                {
                    int totalSeconds = (int)elapsedTotal.TotalSeconds;
                    TextBlock_TotalTime.Text = totalSeconds + "." + elapsedTotal.ToString(@"ff");
                }
            }

            else if (TotalAccuracy == "Milliseconds (0.0)")
            {
                if (Convert.ToInt32(elapsedTotal.ToString(@"dd")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss\.f");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"hh")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss\.f");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"mm")) != 0)
                {
                    int totalMinutes = (int)elapsedTotal.TotalMinutes;
                    TextBlock_TotalTime.Text = totalMinutes + ":" + elapsedTotal.ToString(@"ss\.f");
                }
                else
                {
                    int totalSeconds = (int)elapsedTotal.TotalSeconds;
                    TextBlock_TotalTime.Text = totalSeconds + elapsedTotal.ToString(@"\.f");
                }
            }

            else if (TotalAccuracy == "Seconds")
            {
                if (Convert.ToInt32(elapsedTotal.ToString(@"dd")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"hh")) != 0)
                {
                    int totalHours = (int)elapsedTotal.TotalHours;
                    TextBlock_TotalTime.Text = totalHours + ":" + elapsedTotal.ToString(@"mm\:ss");
                }
                else if (Convert.ToInt32(elapsedTotal.ToString(@"mm")) != 0)
                {
                    int totalMinutes = (int)elapsedTotal.TotalMinutes;
                    TextBlock_TotalTime.Text = totalMinutes + ":" + elapsedTotal.ToString(@"ss");
                }
                else
                {
                    int totalSeconds = (int)elapsedTotal.TotalSeconds;
                    TextBlock_TotalTime.Text = totalSeconds.ToString();
                }
            }

            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                if (Convert.ToInt32(elapsedLevel.ToString(@"dd")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss\.ff");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"hh")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss\.ff");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"mm")) != 0)
                {
                    int levelMinutes = (int)elapsedLevel.TotalMinutes;
                    TextBlock_LevelTime.Text = levelMinutes + ":" + elapsedLevel.ToString(@"ss\.ff");
                }
                else
                {
                    int levelSeconds = (int)elapsedLevel.TotalSeconds;
                    TextBlock_LevelTime.Text = levelSeconds + "." + elapsedLevel.ToString(@"ff");
                }
            }

            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                if (Convert.ToInt32(elapsedLevel.ToString(@"dd")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss\.f");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"hh")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss\.f");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"mm")) != 0)
                {
                    int levelMinutes = (int)elapsedLevel.TotalMinutes;
                    TextBlock_LevelTime.Text = levelMinutes + ":" + elapsedLevel.ToString(@"ss\.f");
                }
                else
                {
                    int levelSeconds = (int)elapsedLevel.TotalSeconds;
                    TextBlock_LevelTime.Text = levelSeconds + elapsedLevel.ToString(@"\.f");
                }
            }

            else if (LevelAccuracy == "Seconds")
            {
                if (Convert.ToInt32(elapsedLevel.ToString(@"dd")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"hh")) != 0)
                {
                    int levelHours = (int)elapsedLevel.TotalHours;
                    TextBlock_LevelTime.Text = levelHours + ":" + elapsedLevel.ToString(@"mm\:ss");
                }
                else if (Convert.ToInt32(elapsedLevel.ToString(@"mm")) != 0)
                {
                    int levelMinutes = (int)elapsedLevel.TotalMinutes;
                    TextBlock_LevelTime.Text = levelMinutes + ":" + elapsedLevel.ToString(@"ss");
                }
                else
                {
                    int levelSeconds = (int)elapsedLevel.TotalSeconds;
                    TextBlock_LevelTime.Text = levelSeconds.ToString();
                }
            }
        }

        private void GetCurrentTimeTotal()
        {
            if (TotalAccuracy == "Milliseconds (0.00)")
            {
                switch (TextBlock_TotalTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 5:     // <1min
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 7:     // <10min
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 8:     // <1hr
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 10:    // <10hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 11:    // <100hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(6, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(7, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(8, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 2, 2)));
                        break;
                }
            }
            else if (TotalAccuracy == "Milliseconds (0.0)")
            {
                switch (TextBlock_TotalTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 5:     // <1min
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 7:     // <10min
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 8:     // <1hr
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 10:    // <10hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 11:    // <100hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(6, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(7, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                    default:    //>=1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(8, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(TextBlock_TotalTime.Text.Length - 1, 1)));
                        break;
                }
            }
            else if (TotalAccuracy == "Seconds")
            {
                switch (TextBlock_TotalTime.Text.Length)
                {
                    case 1:     // <10s
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1)));
                        break;
                    case 2:     // <1min
                        currentTimeTotal = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2)));
                        break;
                    case 4:     // <10min
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2)));
                        break;
                    case 5:     // <1hr
                        currentTimeTotal = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2)));
                        break;
                    case 7:    // <10hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2)));
                        break;
                    case 8:    // <100hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(6, 2)));
                        break;
                    case 9:    // <1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(7, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeTotal = TimeSpan.FromHours(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_TotalTime.Text.ToString().Substring(8, 2)));
                        break;
                }
            }
        }

        private void GetCurrentTimeLastLevel()
        {
            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                switch (TextBlock_LastLevelTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 5:     // 1min
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 7:     // <10min
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 8:     // <1hr
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 10:    // <10hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 11:    // <100hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(6, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(7, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(8, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 2, 2)));
                        break;
                }
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                switch (TextBlock_LastLevelTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 5:     // 1min
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 7:     // <10min
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 8:     // <1hr
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 10:    // <10hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 11:    // <100hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(6, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(7, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(8, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(TextBlock_LastLevelTime.Text.Length - 1, 1)));
                        break;
                }
            }
            else if (LevelAccuracy == "Seconds")
            {
                switch (TextBlock_LastLevelTime.Text.Length)
                {
                    case 1:     // <10s
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1)));
                        break;
                    case 2:     // 1min
                        currentTimeLastLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2)));
                        break;
                    case 4:     // <10min
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2)));
                        break;
                    case 5:     // <1hr
                        currentTimeLastLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2)));
                        break;
                    case 7:    // <10hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2)));
                        break;
                    case 8:    // <100hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(6, 2)));
                        break;
                    case 9:    // <1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(7, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLastLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LastLevelTime.Text.ToString().Substring(8, 2)));
                        break;
                }
            }
        }

        private void GetCurrentTimeLevel()
        {
            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                switch (TextBlock_LevelTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 5:     // 1min
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 7:     // <10min
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 8:     // <1hr
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 10:    // <10hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 11:    // <100hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(6, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(7, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(8, 2))) +
                            10 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 2, 2)));
                        break;
                }
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                switch (TextBlock_LevelTime.Text.Length)
                {
                    case 4:     // <10s
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 5:     // 1min
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 7:     // <10min
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 8:     // <1hr
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 10:    // <10hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 11:    // <100hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(6, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    case 12:    // <1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(7, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(8, 2))) +
                            100 * TimeSpan.FromMilliseconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(TextBlock_LevelTime.Text.Length - 1, 1)));
                        break;
                }
            }
            else if (LevelAccuracy == "Seconds")
            {
                switch (TextBlock_LevelTime.Text.Length)
                {
                    case 1:     // <10s
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1)));
                        break;
                    case 2:     // 1min
                        currentTimeLevel = TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2)));
                        break;
                    case 4:     // <10min
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2)));
                        break;
                    case 5:     // <1hr
                        currentTimeLevel = TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2)));
                        break;
                    case 7:    // <10hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 1))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(2, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2)));
                        break;
                    case 8:    // <100hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 2))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(3, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(6, 2)));
                        break;
                    case 9:    // <1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 3))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(4, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(7, 2)));
                        break;
                    default:    //>=1000hrs
                        currentTimeLevel = TimeSpan.FromHours(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(0, 4))) +
                            TimeSpan.FromMinutes(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(5, 2))) +
                            TimeSpan.FromSeconds(Convert.ToInt32(TextBlock_LevelTime.Text.ToString().Substring(8, 2)));
                        break;
                }
            }
        }

        private void Button_TimerResetLevel_Click(object sender, RoutedEventArgs e)
        {
            startTimeLevel = DateTime.Now;

            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                TextBlock_LevelTime.Text = "0.00";
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                TextBlock_LevelTime.Text = "0.0";
            }
            else if (LevelAccuracy == "Seconds")
            {
                TextBlock_LevelTime.Text = "0";
            }
        }

        private void Button_TimerResetLastLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                TextBlock_LastLevelTime.Text = "0.00";
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                TextBlock_LastLevelTime.Text = "0.0";
            }
            else if (LevelAccuracy == "Seconds")
            {
                TextBlock_LastLevelTime.Text = "0";
            }
        }

        private void Button_TimerResetTotal_Click(object sender, RoutedEventArgs e)
        {
            startTimeTotal = DateTime.Now;
            startTimeLevel = DateTime.Now;

            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                TextBlock_LevelTime.Text = "0.00";
                TextBlock_LastLevelTime.Text = "0.00";
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                TextBlock_LevelTime.Text = "0.0";
                TextBlock_LastLevelTime.Text = "0.0";
            }
            else if (LevelAccuracy == "Seconds")
            {
                TextBlock_LevelTime.Text = "0";
                TextBlock_LastLevelTime.Text = "0";
            }

            if (TotalAccuracy == "Milliseconds (0.00)")
            {
                TextBlock_TotalTime.Text = "0.00";
            }
            else if (TotalAccuracy == "Milliseconds (0.0)")
            {
                TextBlock_TotalTime.Text = "0.0";
            }
            else if (TotalAccuracy == "Seconds")
            {
                TextBlock_TotalTime.Text = "0";
            }
        }

        private void Button_TimerResetAll_Click(object sender, RoutedEventArgs e)
        {
            Button_TimerResetLevel_Click(this, new RoutedEventArgs());
            Button_TimerResetLastLevel_Click(this, new RoutedEventArgs());
            Button_TimerResetTotal_Click(this, new RoutedEventArgs());
        }

        private void Button_TimersSetLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                if (Convert.ToInt32(TextBox_LevelHours.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelHours.Text + ":" + TextBox_LevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LevelSeconds.Text.PadLeft(2, '0') + "." + TextBox_LevelMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LevelMinutes.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelMinutes.Text + ":" + TextBox_LevelSeconds.Text.PadLeft(2, '0') + "." + TextBox_LevelMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LevelSeconds.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelSeconds.Text + "." + TextBox_LevelMilliseconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_LevelTime.Text = "0." + TextBox_LevelMilliseconds.Text;
                }
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                if (Convert.ToInt32(TextBox_LevelHours.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelHours.Text + ":" + TextBox_LevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LevelSeconds.Text.PadLeft(2, '0') + "." + int.Parse(TextBox_LevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_LevelMinutes.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelMinutes.Text + ":" + TextBox_LevelSeconds.Text + "." + int.Parse(TextBox_LevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_LevelSeconds.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelSeconds.Text + "." + int.Parse(TextBox_LevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else
                {
                    TextBlock_LevelTime.Text = "0." + int.Parse(TextBox_LevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
            }
            else if (LevelAccuracy == "Seconds")
            {
                if (Convert.ToInt32(TextBox_LevelHours.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelHours.Text + ":" + TextBox_LevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LevelSeconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LevelMinutes.Text) != 0)
                {
                    TextBlock_LevelTime.Text = TextBox_LevelMinutes.Text + ":" + TextBox_LevelSeconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_LevelTime.Text = TextBox_LevelSeconds.Text;
                }
            }
        }

        private void Button_TimersSetLastLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelAccuracy == "Milliseconds (0.00)")
            {
                if (Convert.ToInt32(TextBox_LastLevelHours.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelHours.Text + ":" + TextBox_LastLevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LastLevelSeconds.Text.PadLeft(2, '0') + "." + TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LevelMinutes.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelMinutes.Text + ":" + TextBox_LastLevelSeconds.Text.PadLeft(2, '0') + "." + TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LevelSeconds.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelSeconds.Text + "." + TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_LastLevelTime.Text = "0." + TextBox_LastLevelMilliseconds.Text;
                }
            }
            else if (LevelAccuracy == "Milliseconds (0.0)")
            {
                if (Convert.ToInt32(TextBox_LastLevelHours.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelHours.Text + ":" + TextBox_LastLevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LastLevelSeconds.Text.PadLeft(2, '0') + "." + int.Parse(TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_LastLevelMinutes.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelMinutes.Text + ":" + TextBox_LastLevelSeconds.Text + "." + int.Parse(TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_LastLevelSeconds.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelSeconds.Text + "." + int.Parse(TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else
                {
                    TextBlock_LastLevelTime.Text = "0." + int.Parse(TextBox_LastLevelMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
            }
            else if (LevelAccuracy == "Seconds")
            {
                if (Convert.ToInt32(TextBox_LastLevelHours.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelHours.Text + ":" + TextBox_LastLevelMinutes.Text.PadLeft(2, '0') + ":" + TextBox_LastLevelSeconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_LastLevelMinutes.Text) != 0)
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelMinutes.Text + ":" + TextBox_LastLevelSeconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_LastLevelTime.Text = TextBox_LastLevelSeconds.Text;
                }
            }
        }

        private void Button_TimersSetTotal_Click(object sender, RoutedEventArgs e)
        {
            if (TotalAccuracy == "Milliseconds (0.00)")
            {
                if (Convert.ToInt32(TextBox_TotalHours.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalHours.Text + ":" + TextBox_TotalMinutes.Text.PadLeft(2, '0') + ":" + TextBox_TotalSeconds.Text.PadLeft(2, '0') + "." + TextBox_TotalMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_TotalMinutes.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalMinutes.Text + ":" + TextBox_TotalSeconds.Text.PadLeft(2, '0') + "." + TextBox_TotalMilliseconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_TotalSeconds.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalSeconds.Text + "." + TextBox_TotalMilliseconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_TotalTime.Text = "0." + TextBox_TotalMilliseconds.Text;
                }
            }
            else if (TotalAccuracy == "Milliseconds (0.0)")
            {
                if (Convert.ToInt32(TextBox_TotalHours.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalHours.Text + ":" + TextBox_TotalMinutes.Text.PadLeft(2, '0') + ":" + TextBox_TotalSeconds.Text.PadLeft(2, '0') + "." + int.Parse(TextBox_TotalMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_TotalMinutes.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalMinutes.Text + ":" + TextBox_TotalSeconds.Text + "." + int.Parse(TextBox_TotalMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else if (Convert.ToInt32(TextBox_TotalSeconds.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalSeconds.Text + "." + int.Parse(TextBox_TotalMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
                else
                {
                    TextBlock_TotalTime.Text = "0." + int.Parse(TextBox_TotalMilliseconds.Text.PadLeft(2, '0').Substring(0, 1));
                }
            }
            else if (TotalAccuracy == "Seconds")
            {
                if (Convert.ToInt32(TextBox_TotalHours.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalHours.Text + ":" + TextBox_TotalMinutes.Text.PadLeft(2, '0') + ":" + TextBox_TotalSeconds.Text.PadLeft(2, '0');
                }
                else if (Convert.ToInt32(TextBox_TotalMinutes.Text) != 0)
                {
                    TextBlock_TotalTime.Text = TextBox_TotalMinutes.Text + ":" + TextBox_TotalSeconds.Text.PadLeft(2, '0');
                }
                else
                {
                    TextBlock_TotalTime.Text = TextBox_TotalSeconds.Text;
                }
            }
        }

        private void Button_TimersSetAll_Click(object sender, RoutedEventArgs e)
        {
            Button_TimersSetLevel_Click(this, new RoutedEventArgs());
            Button_TimersSetLastLevel_Click(this, new RoutedEventArgs());
            Button_TimersSetTotal_Click(this, new RoutedEventArgs());
        }

        private void Button_SetTotalExits_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_ExitCountTotal.Text = TextBox_ExitCountTotal_Manual.Text;
        }
        private void Button_SetCurrentExits_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_ExitCountCurrent.Text = TextBox_ExitCountCurrent_Manual.Text;
        }
        private async void Button_UpdateHackInfo_Click(object sender, RoutedEventArgs e)
        {
            string hackName = TextBox_HackName.Text;
            string[] hackData = await SMWCentralAPICall(hackName);

            if (hackData[0] == "Cannot find Hack Name")
            {
                MessageBox.Show("Cannot find Hack Name");
            }
            else
            {
                Label_Hack.Content = hackData[0];
                Label_Creator.Content = "By: " + hackData[1];
                TextBlock_ExitCountTotal.Text = hackData[2];
            }
        }

        private void CheckBox_ShowSwitchExits_Checked(object sender, RoutedEventArgs e)
        {
            TextBlock_SwitchCount.Visibility = Visibility.Visible;
        }

        private void CheckBox_ShowSwitchExits_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBlock_SwitchCount.Visibility = Visibility.Hidden;
        }

        private async void Button_GetHackData_Click(object sender, RoutedEventArgs e)
        {
            string hackName = TextBox_HackName.Text;
            string[] hackData = await SMWCentralAPICall2(hackName);

            if (hackData[0] == "Cannot find Hack Name")
            {
                MessageBox.Show("Cannot find Hack Name");
            }
            else
            {
                MessageBox.Show($"Hack Name: {hackData[0]} \n" +
                    $"Hack ID: {hackData[1]} \n" +
                    $"Hack Section: {hackData[2]} \n" +
                    $"Date Submitted: {hackData[3]} \n" +
                    $"Moderated: {hackData[4]} \n" +
                    $"Authors: {hackData[5]} \n" +
                    $"Tags: {hackData[6]} \n" +
                    $"Rating: {hackData[7]} \n" +
                    $"Downloads: {hackData[8]} \n" +
                    $"Length: {hackData[9]} \n" +
                    $"Difficulty: {hackData[10]} \n\n" +
                    $"Description: \n{hackData[11]}");
            }
        }

        static async Task<string[]> SMWCentralAPICall(string hackName)
        {
            List<string> hackData = new List<string>();
            hackData.Add("Cannot find Hack Name");

            string lengthText = "??";
            string apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&u=0&f[name]={hackName}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();  // Read and parse the JSON response
                    JObject jsonObject = JObject.Parse(jsonContent);                  // Parse the JSON into a JObject

                    if (jsonObject["data"] != null)                                   // Check if the JSON response contains data
                    {
                        JArray data = (JArray)jsonObject["data"];
                        foreach (JToken item in data)
                        {
                            string name = item["name"].ToString();
                            if (name.ToLower() == hackName.ToLower())
                            {
                                hackData.Clear();
                                string hack_Name = name;

                                string hackAuthorsArray = item["authors"].ToString();                   //user[]
                                string[] authorsArray = JArray.Parse(hackAuthorsArray).Select(author => author["name"].ToString()).ToArray();
                                string hackAuthors = string.Join(", ", authorsArray);

                                string length = item["fields"]["length"].ToString();
                                lengthText = length.Replace(" exit(s)", string.Empty).Trim();
                                hackData.AddRange(new string[] { hack_Name, hackAuthors, lengthText });
                                break;
                            }
                        }
                    }
                }
            }

            if (lengthText == "??")
            {
                apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&u=1&f[name]={hackName}";

                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();  // Read and parse the JSON response
                        JObject jsonObject = JObject.Parse(jsonContent);                  // Parse the JSON into a JObject

                        if (jsonObject["data"] != null)                                   // Check if the JSON response contains data
                        {
                            JArray data = (JArray)jsonObject["data"];
                            foreach (JToken item in data)
                            {
                                string name = item["name"].ToString();
                                if (name.ToLower() == hackName.ToLower())
                                {
                                    hackData.Clear();
                                    string hack_Name = name;

                                    string hackAuthorsArray = item["authors"].ToString();                   //user[]
                                    string[] authorsArray = JArray.Parse(hackAuthorsArray).Select(author => author["name"].ToString()).ToArray();
                                    string hackAuthors = string.Join(", ", authorsArray);

                                    string length = item["fields"]["length"].ToString();
                                    lengthText = length.Replace(" exit(s)", string.Empty).Trim();
                                    hackData.AddRange(new string[] { hack_Name, hackAuthors, lengthText });
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return hackData.ToArray();
        }

        static async Task<string[]> SMWCentralAPICall2(string hackName)
        {
            List<string> hackData = new List<string>();
            hackData.Add("Cannot find Hack Name");

            string apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&u=0&f[name]={hackName}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();  // Read and parse the JSON response
                    JObject jsonObject = JObject.Parse(jsonContent);                  // Parse the JSON into a JObject

                    if (jsonObject["data"] != null)                                   // Check if the JSON response contains data
                    {
                        JArray data = (JArray)jsonObject["data"];
                        foreach (JToken item in data)
                        {
                            string name = item["name"].ToString();
                            if (name.ToLower() == hackName.ToLower())
                            {
                                hackData.Clear();
                                string hack_Name = name;
                                string hackID = item["id"].ToString();                                  //int
                                string hackSection = item["section"].ToString();                        //string

                                string hackTimeUNIX = item["time"].ToString();
                                string hackTime = null;
                                if (long.TryParse(hackTimeUNIX, out long unixTimestamp))
                                {
                                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                                    hackTime = dateTime.ToString();
                                }

                                string hackModerated = item["moderated"].ToString();                    //bool

                                string hackAuthorsArray = item["authors"].ToString();                   //user[]
                                string[] authorsArray = JArray.Parse(hackAuthorsArray).Select(author => author["name"].ToString()).ToArray();
                                string hackAuthors = string.Join(", ", authorsArray);

                                string hackTagsArray = item["tags"].ToString();                         //string[]
                                string[] tagsArray = JArray.Parse(hackTagsArray).Select(tag => tag.ToString()).ToArray();
                                string hackTags = string.Join(", ", tagsArray);

                                string hackRating = item["rating"].ToString();                          //number | null

                                string hackDownloads = item["downloads"].ToString();                    //number

                                string hackLength = item["fields"]["length"].ToString();                //string

                                string hackDifficulty = item["fields"]["difficulty"].ToString();        //string

                                string hackDescriptionMessy = item["fields"]["description"].ToString(); //string
                                var doc = new HtmlDocument();
                                doc.LoadHtml(hackDescriptionMessy);
                                doc.DocumentNode.SelectNodes("//br")?.ToList().ForEach(br => br.Remove());
                                string hackDescription = doc.DocumentNode.InnerText;

                                hackData.AddRange(new string[] { hack_Name, hackID, hackSection, hackTime, hackModerated, hackAuthors, hackTags, hackRating, hackDownloads, hackLength, hackDifficulty, hackDescription });
                                break;
                            }
                        }
                    }
                }
            }

            if (hackData[0] == "Cannot find Hack Name")
            {
                apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&u=1&f[name]={hackName}";

                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();  // Read and parse the JSON response
                        JObject jsonObject = JObject.Parse(jsonContent);                  // Parse the JSON into a JObject

                        if (jsonObject["data"] != null)                                   // Check if the JSON response contains data
                        {
                            JArray data = (JArray)jsonObject["data"];
                            foreach (JToken item in data)
                            {
                                string name = item["name"].ToString();
                                if (name.ToLower() == hackName.ToLower())
                                {
                                    hackData.Clear();
                                    string hackID = item["id"].ToString();                                  //int
                                    string hackSection = item["section"].ToString();                        //string

                                    string hackTimeUNIX = item["time"].ToString();
                                    string hackTime = null;
                                    if (long.TryParse(hackTimeUNIX, out long unixTimestamp))
                                    {
                                        DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                                        hackTime = dateTime.ToString();
                                    }

                                    string hackModerated = item["moderated"].ToString();                    //bool

                                    string hackAuthorsArray = item["authors"].ToString();                   //user[]
                                    string[] authorsArray = JArray.Parse(hackAuthorsArray).Select(author => author["name"].ToString()).ToArray();
                                    string hackAuthors = string.Join(", ", authorsArray);

                                    string hackTagsArray = item["tags"].ToString();                         //string[]
                                    string[] tagsArray = JArray.Parse(hackTagsArray).Select(tag => tag.ToString()).ToArray();
                                    string hackTags = string.Join(", ", tagsArray);

                                    string hackRating = item["rating"].ToString();                          //number | null

                                    string hackDownloads = item["downloads"].ToString();                    //number

                                    string hackLength = item["fields"]["length"].ToString();                //string

                                    string hackDifficulty = item["fields"]["difficulty"].ToString();        //string

                                    string hackDescriptionMessy = item["fields"]["description"].ToString(); //string
                                    var doc = new HtmlDocument();
                                    doc.LoadHtml(hackDescriptionMessy);
                                    doc.DocumentNode.SelectNodes("//br")?.ToList().ForEach(br => br.Remove());
                                    string hackDescription = doc.DocumentNode.InnerText;

                                    hackData.AddRange(new string[] { hackID, hackSection, hackTime, hackModerated, hackAuthors, hackTags, hackRating, hackDownloads, hackLength, hackDifficulty, hackDescription });
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return hackData.ToArray();
        }

        private void TextBox_HackName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox_HackName.Text == "[Enter Hack Name Here]")
            {
                TextBox_HackName.Clear();
                TextBox_HackName.Focus();
                TextBox_HackName.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void MenuItem_Click_Timers(object sender, RoutedEventArgs e)
        {
            TimerWindow timerWindow = new(this);
            timerWindow.ShowDialog();
            if (timerWindow.TimerOK)
            {

                switch (timerWindow.ComboBoxLevelAccuracy.SelectedIndex)
                {
                    case 0:
                        LevelAccuracy = "Milliseconds (0.00)";
                        break;
                    case 1:
                        LevelAccuracy = "Milliseconds (0.0)";
                        break;
                    case 2:
                        LevelAccuracy = "Seconds";
                        break;
                }

                switch (timerWindow.ComboBoxTotalAccuracy.SelectedIndex)
                {
                    case 0:
                        TotalAccuracy = "Milliseconds (0.00)";
                        break;
                    case 1:
                        TotalAccuracy = "Milliseconds (0.0)";
                        break;
                    case 2:
                        TotalAccuracy = "Seconds";
                        break;
                }
                
/*                GetCurrentTimeTotal();
                GetCurrentTimeLastLevel();
                GetCurrentTimeLevel();
                if (currentTimeTotal == TimeSpan.Zero)
                {
                    startTimeTotal = DateTime.Now;
                    startTimeLevel = startTimeTotal;
                }
                else
                {
                    TimeSpan startTimeTotal = elapsedTotal;
                    TimeSpan startTimeLevel = elapsedLevel;
                }*/

                //GetTimes();
            }
        }

        private void MenuItem_Click_DeathImage(object sender, RoutedEventArgs e)
        {
            DeathImageWindow deathImageWindow = new(this);
            deathImageWindow.ShowDialog();
            if (deathImageWindow.DeathImageOK)
            {
                switch (deathImageWindow.ComboBoxDeathImage.SelectedIndex)
                {

                    case 0:
                        NewDeathImage = new BitmapImage(new Uri("pack://application:,,,/images/SMB1.png"));
                        break;
                    case 1:
                        NewDeathImage = new BitmapImage(new Uri("pack://application:,,,/images/SMB3.png"));
                        break;
                    case 2:
                        NewDeathImage = new BitmapImage(new Uri("pack://application:,,,/images/SMW.png"));
                        break;
                    case 3:
                        NewDeathImage = new BitmapImage(new Uri("pack://application:,,,/images/Paper Mario.png"));
                        break;
                }
                Image_MarioDeath1.Source = NewDeathImage;
                Image_MarioDeath2.Source = NewDeathImage;
            }
        }

        private void Button_ManualSplit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
                LevelDeathCount = 0;
                TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
                CounterRange();

                TextBlock_LastLevelTime.Text = TextBlock_LevelTime.Text;
                if (LevelAccuracy == "Milliseconds (0.00)")
                {
                    TextBlock_LevelTime.Text = "0.00";
                }
                else if (LevelAccuracy == "Milliseconds (0.0)")
                {
                    TextBlock_LevelTime.Text = "0.0";
                }
                else if (LevelAccuracy == "Seconds")
                {
                    TextBlock_LevelTime.Text = "0";
                }
            });

            previousExitCounterValue = ExitCountCurrent;
            startTimeLevel = DateTime.Now;
        }

        private void MenuItem_Click_Fonts(object sender, RoutedEventArgs e)
        {
            FontsWindow fontsWindow = new(this);
            fontsWindow.ShowDialog();
            if (fontsWindow.FontsOK)
            {
                CurrentFontTitle = fontsWindow.NewFontTitle;
                CurrentFontAuthor = fontsWindow.NewFontAuthor;

                Label_Hack.FontFamily = CurrentFontTitle;
                Label_Creator.FontFamily = CurrentFontAuthor;
            }
        }
    }
}