using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SMW_Data.View;

namespace SMW_Data
{
    public partial class MainWindow : Window
    {
        public SolidColorBrush CurrentBackgroundColor { get; set; }
        public SolidColorBrush CurrentTextColor { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CurrentBackgroundColor = (SolidColorBrush)GridMain.Background;
            CurrentTextColor = (SolidColorBrush)Label_LevelDeathCount.Foreground;
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
            //put something here to pull in current colors?
            settingsWindow.ShowDialog();
            if (settingsWindow.SettingsOK)
            {
                GridMain.Background = settingsWindow.ChangeBackgroundColor;
                Label_LevelDeathCount.Foreground = settingsWindow.ChangeTextColor;
                Label_TotalDeathCount.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_LevelDeathCount.Foreground = settingsWindow.ChangeTextColor;
                TextBlock_TotalDeathCount.Foreground = settingsWindow.ChangeTextColor;
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

        private void Button_AddLevelDeaths_Click(object sender, RoutedEventArgs e)
        {
            int LevelDeathCount;
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount += Int32.Parse(TextBox_AddLevelDeaths.Text);
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void Button_AddTotalDeaths_Click(object sender, RoutedEventArgs e)
        {
            int TotalDeathCount;
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount += Int32.Parse(TextBox_AddTotalDeaths.Text);
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
        }

        private void ButtonLevelPlus_Click(object sender, RoutedEventArgs e)
        {
            int LevelDeathCount;
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount++;
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void ButtonLevelMinus_Click(object sender, RoutedEventArgs e)
        {
            int LevelDeathCount;
            LevelDeathCount = Int32.Parse(TextBlock_LevelDeathCount.Text);
            LevelDeathCount--;
            TextBlock_LevelDeathCount.Text = LevelDeathCount.ToString();
            CounterRange();
        }

        private void ButtonTotalPlus_Click(object sender, RoutedEventArgs e)
        {
            int TotalDeathCount;
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount++;
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
        }

        private void ButtonTotalMinus_Click(object sender, RoutedEventArgs e)
        {
            int TotalDeathCount;
            TotalDeathCount = Int32.Parse(TextBlock_TotalDeathCount.Text);
            TotalDeathCount--;
            TextBlock_TotalDeathCount.Text = TotalDeathCount.ToString();
            CounterRange();
        }
    }
}

// Need to force postive integers only in the text box for adding lives