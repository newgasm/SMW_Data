using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;

namespace SMW_Data.View
{
    public partial class SettingsWindow : Window
    {
        public bool SettingsOK { get; set; }
        public System.Windows.Media.Brush ChangeBackgroundColor { get; set; }
        public System.Windows.Media.Brush ChangeTextColor { get; set; }
        public System.Windows.Media.Brush NewBackgroundColor;
        public System.Windows.Media.Brush NewTextColor;
        private readonly MainWindow mainWindow;

        public SettingsWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();

            // Pull in current Background Color
            System.Windows.Media.SolidColorBrush MainBackgroundColor = mainWindow.CurrentBackgroundColor;
            System.Windows.Media.Color color1 = MainBackgroundColor.Color;
            string CurrentBackgroundHexColor = ColorToHexString2(color1);
            TextBoxBackgroundColor.Text = CurrentBackgroundHexColor;
            
            //Change Background Color Button to current color
            System.Windows.Media.Brush hexColorBrush1 = new SolidColorBrush(color1);
            ButtonBackgroundColor.Background = hexColorBrush1;
            NewBackgroundColor = hexColorBrush1;

            // Pull in current Text Color
            System.Windows.Media.SolidColorBrush MainTextColor = mainWindow.CurrentTextColor;
            System.Windows.Media.Color color2 = MainTextColor.Color;
            string CurrentTextHexColor = ColorToHexString2(color2);
            TextBoxTextColor.Text = CurrentTextHexColor;

            //Change Text Color Button to current color
            System.Windows.Media.Brush hexColorBrush2 = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color2.A, color2.R, color2.G, color2.B));
            ButtonTextColor.Background = hexColorBrush2;
            NewTextColor = hexColorBrush2;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            SettingsOK = true;
            ChangeBackgroundColor = NewBackgroundColor;
            ChangeTextColor = NewTextColor;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static string ColorToHexString2(System.Windows.Media.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string ColorToHexString(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
        private bool IsValidHexColor(string color)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
        }

        private void ButtonBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            WinForms.ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string backgroundColor = ColorToHexString(colorDialog.Color);
                TextBoxBackgroundColor.Text = backgroundColor;
                System.Windows.Media.Brush backgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                ButtonBackgroundColor.Background = backgroundBrush;
                NewBackgroundColor = backgroundBrush;
            }
            else
            {
            }
        }

        private void ButtonTextColor_Click(object sender, RoutedEventArgs e)
        {
            WinForms.ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string textColor = ColorToHexString(colorDialog.Color);
                TextBoxTextColor.Text = textColor;
                System.Windows.Media.Brush textBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                ButtonTextColor.Background = textBrush;
                NewTextColor = textBrush;
            }
            else
            {
            }
        }

        private void TextBoxBackgroundColor_LostFocus(object sender, RoutedEventArgs e)
        {
            string backgroundColor = TextBoxBackgroundColor.Text.Trim();

            if (IsValidHexColor(backgroundColor))
            {
                string backgroundColorA = "FF";
                string backgroundColorR = backgroundColor.Substring(1, 2);
                string backgroundColorG = backgroundColor.Substring(3, 2);
                string backgroundColorB = backgroundColor.Substring(5, 2);

                byte backgroundByteA = byte.Parse(backgroundColorA, System.Globalization.NumberStyles.HexNumber);
                byte backgroundByteR = byte.Parse(backgroundColorR, System.Globalization.NumberStyles.HexNumber);
                byte backgroundByteG = byte.Parse(backgroundColorG, System.Globalization.NumberStyles.HexNumber);
                byte backgroundByteB = byte.Parse(backgroundColorB, System.Globalization.NumberStyles.HexNumber);

                System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(backgroundByteA, backgroundByteR, backgroundByteG, backgroundByteB);
                System.Windows.Media.Brush backgroundBrush = new SolidColorBrush(color);

                ButtonBackgroundColor.Background = backgroundBrush;
                NewBackgroundColor = backgroundBrush;
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid color format. Please use #RRGGBB format.");
            }
        }

        private void TextBoxTextColor_LostFocus(object sender, RoutedEventArgs e)
        {

            string textColor = TextBoxTextColor.Text.Trim();

            if (IsValidHexColor(textColor))
            {
                string textColorA = "FF";
                string textColorR = textColor.Substring(1, 2);
                string textColorG = textColor.Substring(3, 2);
                string textColorB = textColor.Substring(5, 2);

                byte textByteA = byte.Parse(textColorA, System.Globalization.NumberStyles.HexNumber);
                byte textByteR = byte.Parse(textColorR, System.Globalization.NumberStyles.HexNumber);
                byte textByteG = byte.Parse(textColorG, System.Globalization.NumberStyles.HexNumber);
                byte textByteB = byte.Parse(textColorB, System.Globalization.NumberStyles.HexNumber);

                System.Windows.Media.Color color1 = System.Windows.Media.Color.FromArgb(textByteA, textByteR, textByteG, textByteB);
                System.Windows.Media.Brush textBrush = new SolidColorBrush(color1);
                
                ButtonTextColor.Background = textBrush;
                NewTextColor = textBrush;
            }
            else
            {
                System.Windows.MessageBox.Show("Invalid color format. Please use #RRGGBB format.");
            }
        }
    }
}