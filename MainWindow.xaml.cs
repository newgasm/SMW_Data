using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SMW_Data.View;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.Linq;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SMW_Data
{
    public partial class MainWindow : Window
    {
        public SolidColorBrush CurrentBackgroundColor { get; set; }
        public SolidColorBrush CurrentTextColor { get; set; }

        private static int TotalDeathCount;
        private static int LevelDeathCount;
        public bool DeathState;

        private static int SwitchesActivated = 0;
        public bool GreenSwitchActivated;
        public bool YellowSwitchActivated;
        public bool BlueSwitchActivated;
        public bool RedSwitchActivated;

        private bool isFirstMessageReceived = false;

        static WebSocket ws;
        private DispatcherTimer timer;

        public string MemoryAddressValue_DeathCheck;
        static readonly int MemoryAddress_DeathCheck = 0x7E0071;
        static readonly int adjMemoryAddress_DeathCheck = 0xF50000 + (MemoryAddress_DeathCheck - 0x7E0000);
        static readonly string AdjustedMemoryAddress_DeathCheck = adjMemoryAddress_DeathCheck.ToString("X");

        public string MemoryAddressValue_KeyExit;
        static readonly int MemoryAddress_KeyExit = 0x7E1435;
        static readonly int adjMemoryAddress_KeyExit = 0xF50000 + (MemoryAddress_KeyExit - 0x7E0000);
        static readonly string AdjustedMemoryAddress_KeyExit = adjMemoryAddress_KeyExit.ToString("X");

        public string MemoryAddressValue_OtherExits;
        static readonly int MemoryAddress_OtherExits = 0x7E1493;
        static readonly int adjMemoryAddress_OtherExits = 0xF50000 + (MemoryAddress_OtherExits - 0x7E0000);
        static readonly string AdjustedMemoryAddress_OtherExits = adjMemoryAddress_OtherExits.ToString("X");

        public string MemoryAddressValue_ExitCounter;
        static readonly int MemoryAddress_ExitCounter = 0x7E1F2E;
        static readonly int adjMemoryAddress_ExitCounter = 0xF50000 + (MemoryAddress_ExitCounter - 0x7E0000);
        static readonly string AdjustedMemoryAddress_ExitCounter = adjMemoryAddress_ExitCounter.ToString("X");

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
            InitializeWebSocket();
        }

        private void InitializeWebSocket()
        {
            ws = new WebSocket("ws://localhost:8080");

            ws.OnOpen += (sender, e) =>
            {
                //MessageBox.Show("WebSocket connected");

                var deviceListRequest = new
                {
                    Opcode = "DeviceList",
                    Space = "SNES"
                };
                ws.Send(JsonConvert.SerializeObject(deviceListRequest));

                var attachRequest = new
                {
                    Opcode = "Attach",
                    Space = "SNES",
                    Operands = new[] { "SD2SNES COM4" }
                };
                ws.Send(JsonConvert.SerializeObject(attachRequest));

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(16); // 16ms is approximately 60fps
                timer.Tick += Timer_Tick;
                timer.Start();

            };

            ws.OnMessage += (sender, e) =>
            {
                if (isFirstMessageReceived)
                {
                    ProcessMemoryAddressResponse_DeathCheck(e.RawData);
                    ProcessMemoryAddressResponse_KeyExit(e.RawData);
                    ProcessMemoryAddressResponse_OtherExits(e.RawData);
                    ProcessMemoryAddressResponse_ExitCounter(e.RawData);
                    ProcessMemoryAddressResponse_InGame(e.RawData);
                    ProcessMemoryAddressResponse_Switches(e.RawData);
                }
                else
                {
                    isFirstMessageReceived = true;
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
                TextBlock_Footer.Text = "Connected to WebSocket";
            }
            catch (Exception ex)
            {
                //MessageBox.Show("WebSocket connection failed: " + ex.Message);
                TextBlock_Footer.Text = "Not Connected to WebSocket";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                SendGetAddressRequest(ws,
                    AdjustedMemoryAddress_SwitchesActivated, 
                    AdjustedMemoryAddress_DeathCheck, 
                    AdjustedMemoryAddress_KeyExit, 
                    AdjustedMemoryAddress_OtherExits, 
                    AdjustedMemoryAddress_ExitCounter, 
                    AdjustedMemoryAddress_InGame);
            }
            catch
            {
                TextBlock_Footer.Text = "Not Connected to WebSocket";
            }
        }

        private static void SendGetAddressRequest(WebSocket ws,
            string MemoryAddressValue_SwitchesActivated, 
            string MemoryAddressValue_DeathCheck, 
            string MemoryAddressValue_KeyExit, 
            string MemoryAddressValue_OtherExits, 
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
                    MemoryAddressValue_KeyExit, "1", 
                    MemoryAddressValue_OtherExits, "1" , 
                    MemoryAddressValue_ExitCounter , "1",
                    MemoryAddressValue_InGame, "1"
                }
            };
            ws.Send(JsonConvert.SerializeObject(getAddressRequest));
        }
  
        private void ProcessMemoryAddressResponse_Switches(byte[] rawData)
        {
            string MemoryAddressValue_GreenSwitchActivated = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 26, 2);
            if (MemoryAddressValue_GreenSwitchActivated != "00" && GreenSwitchActivated == false)
            {
                GreenSwitchActivated = true;
                SwitchesActivated++;
            }

            string MemoryAddressValue_YellowSwitchActivated = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 23, 2);
            if (MemoryAddressValue_YellowSwitchActivated != "00" && YellowSwitchActivated == false)
            {
                YellowSwitchActivated = true;
                SwitchesActivated++;
            }
            
            string MemoryAddressValue_BlueSwitchActivated = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 20, 2);
            if (MemoryAddressValue_BlueSwitchActivated != "00" && BlueSwitchActivated == false)
            {
                BlueSwitchActivated = true;
                SwitchesActivated++;
            }
            
            string MemoryAddressValue_RedSwitchActivated = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 17, 2);
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

        private void ProcessMemoryAddressResponse_DeathCheck(byte[] rawData)
            {
                string MemoryAddressValue_DeathCheck = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 14, 2);
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

        private void ProcessMemoryAddressResponse_KeyExit(byte[] rawData)
        {
            string MemoryAddressValue_KeyExit = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 11, 2);
            if (MemoryAddressValue_KeyExit != "00")
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
                    LevelDeathCount = 0;
                    TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
                    CounterRange();
                });
            }
        }

        private void ProcessMemoryAddressResponse_OtherExits(byte[] rawData)
        {
            string MemoryAddressValue_OtherExits = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 8, 2);
            if (MemoryAddressValue_OtherExits != "00")
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
                    LevelDeathCount = 0;
                    TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
                    CounterRange();
                });
            }
        }

        private void ProcessMemoryAddressResponse_ExitCounter(byte[] rawData)
        {
            string MemoryAddressValue_ExitCounter = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 5, 2);
            int ExitCountCurrent = Convert.ToInt32(MemoryAddressValue_ExitCounter, 16);

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                TextBlock_ExitCountCurrent.Text = ExitCountCurrent.ToString();
            });
        }
        private void ProcessMemoryAddressResponse_InGame(byte[] rawData)
        {
            string MemoryAddressValue_InGame = BitConverter.ToString(rawData).Substring(BitConverter.ToString(rawData).Length - 2);
            if (MemoryAddressValue_InGame != "02")
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    TextBlock_ExitCountCurrent.Text = "??";
                    TextBlock_SwitchCount.Text = "+0";
                    SwitchesActivated = 0;
                });
            }
        }

        private void CounterRange()
        {
            if (Int32.Parse(TextBlock_LevelDeathCount.Text)>= 999999)
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

        private void MenuItem_Click_Settings(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new(this);
            settingsWindow.ShowDialog();
            if (settingsWindow.SettingsOK)
            {
                GridMain.Background = settingsWindow.ChangeBackgroundColor;
                Label_LevelDeathCount.Foreground = settingsWindow.ChangeTextColor;
                Label_TotalDeathCount.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_LevelDeathCount.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_TotalDeathCount.Foreground = settingsWindow.ChangeTextColor;
                Label_ExitCount.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_ExitCountCurrent.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_ExitCountSlash.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_ExitCountTotal.Foreground = settingsWindow.ChangeTextColor;
                Label_HackName.Foreground = settingsWindow.ChangeTextColor;

                CurrentBackgroundColor = (SolidColorBrush)settingsWindow.NewBackgroundColor;
                CurrentTextColor = (SolidColorBrush)settingsWindow.NewTextColor;

            }
        }

        private void MenuItem_Click_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_ResetLevel_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_LevelDeathCount.Text = "0";
        }

        private void Button_ResetTotal_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_TotalDeathCount.Text = "0";
        }

        private void TextBox_AddLevelDeaths_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void TextBox_AddTotalDeaths_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void Button_AddLevelDeaths_Click(object sender, RoutedEventArgs e)
        {
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount += Int32.Parse(TextBox_AddLevelDeaths.Text);
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void Button_AddTotalDeaths_Click(object sender, RoutedEventArgs e)
        {
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount += Int32.Parse(TextBox_AddTotalDeaths.Text);
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
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

        private async void Button_GetExitCount_Click(object sender, RoutedEventArgs e)
        {
            string hackName = TextBox_HackName.Text;
            string lengthText = await SMWCentralAPICall(hackName);
            TextBlock_ExitCountTotal.Text = lengthText;
            if (lengthText == "??")
            {
                MessageBox.Show("Cannot find Hack Name");
            }
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
                MessageBox.Show($"Hack ID: {hackData[0]} \n" +
                    $"Hack Section: {hackData[1]} \n" +
                    $"Date Submitted: {hackData[2]} \n" +
                    $"Moderated: {hackData[3]} \n" +
                    $"Authors: {hackData[4]} \n" +
                    $"Tags: {hackData[5]} \n" +
                    $"Rating: {hackData[6]} \n" +
                    $"Downloads: {hackData[7]} \n" +
                    $"Length: {hackData[8]} \n" +
                    $"Difficulty: {hackData[9]} \n\n" +
                    $"Description: \n{hackData[10]}");
            }
        }

        static async Task<string> SMWCentralAPICall(string hackName)
        {
            string apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&f[name]={hackName}";
            string lengthText = "??";

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
                                string length = item["fields"]["length"].ToString();
                                lengthText = length.Replace(" exit(s)", string.Empty).Trim();
                                break;
                            }
                        }
                    }
                }
            }
            return lengthText;
        }

        static async Task<string[]> SMWCentralAPICall2(string hackName)
        {
            string apiUrl = $"https://www.smwcentral.net/ajax.php?a=getsectionlist&s=smwhacks&f[name]={hackName}";
            List<string> hackData = new List<string>();
            hackData.Add("Cannot find Hack Name");

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
            return hackData.ToArray();
        }

        private void Button_Reconnect_Click(object sender, RoutedEventArgs e)
        {
            InitializeWebSocket();
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
    }
}