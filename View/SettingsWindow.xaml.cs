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

        private System.Windows.Media.Brush NewBackgroundColor;
        private System.Windows.Media.Brush NewTextColor;


        public SettingsWindow(Window parentWindow)
        {
            Owner = parentWindow;
            InitializeComponent();
            //Change both Buttons to current color
            //Change both text boxes to current HEX
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

        private static string ColorToHexString(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
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

        private void TextBoxBackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {

        string backgroundColor = TextBoxBackgroundColor.Text;
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
        //Crashes if not a string that works


        }

        private void TextBoxTextColor_TextChanged(object sender, TextChangedEventArgs e)
        {

        string textColor = TextBoxTextColor.Text;
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
            //Crashes if not a string that works (need to limit to 6 characters and 000000 to FFFFFF... 0 to 16777215)
        }
    }
}