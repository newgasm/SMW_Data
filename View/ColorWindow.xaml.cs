using System.Windows;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace SMW_Data.View
{
    public partial class ColorWindow : Window
    {
        public bool ColorOK { get; set; }
        public Brush ChangeBackgroundColor { get; set; }
        public Brush ChangeTextColor { get; set; }
        public Brush NewBackgroundColor;
        public Brush NewTextColor;
        private readonly MainWindow mainWindow;

        public ColorWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();

            // Initialize colors
            ChangeBackgroundColor = Brushes.White;
            ChangeTextColor = Brushes.White;

            // Pull in current Background Color
            SolidColorBrush MainBackgroundColor = mainWindow.CurrentBackgroundColor;
            Color color1 = MainBackgroundColor.Color;
            string CurrentBackgroundHexColor = ColorToHexString2(color1);
            TextBoxBackgroundColor.Text = CurrentBackgroundHexColor;
            
            //Change Background Color Button to current color
            Brush hexColorBrush1 = new SolidColorBrush(color1);
            ButtonBackgroundColor.Background = hexColorBrush1;
            NewBackgroundColor = hexColorBrush1;

            // Pull in current Text Color
            SolidColorBrush MainTextColor = mainWindow.CurrentTextColor;
            Color color2 = MainTextColor.Color;
            string CurrentTextHexColor = ColorToHexString2(color2);
            TextBoxTextColor.Text = CurrentTextHexColor;

            //Change Text Color Button to current color
            Brush hexColorBrush2 = new SolidColorBrush(Color.FromArgb(color2.A, color2.R, color2.G, color2.B));
            ButtonTextColor.Background = hexColorBrush2;
            NewTextColor = hexColorBrush2;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            ColorOK = true;
            ChangeBackgroundColor = NewBackgroundColor;
            ChangeTextColor = NewTextColor;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static string ColorToHexString2(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string ColorToHexString(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
        private static bool IsValidHexColor(string color)
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
                Brush backgroundBrush = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
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
                Brush textBrush = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                ButtonTextColor.Background = textBrush;
                NewTextColor = textBrush;
            }
            else
            {
                // Do nothing
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

                Color color = System.Windows.Media.Color.FromArgb(backgroundByteA, backgroundByteR, backgroundByteG, backgroundByteB);
                Brush backgroundBrush = new SolidColorBrush(color);

                ButtonBackgroundColor.Background = backgroundBrush;
                NewBackgroundColor = backgroundBrush;
            }
            else
            {
                MessageBox.Show("Invalid color format. Please use #RRGGBB format.");
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

                Color color1 = System.Windows.Media.Color.FromArgb(textByteA, textByteR, textByteG, textByteB);
                Brush textBrush = new SolidColorBrush(color1);
                
                ButtonTextColor.Background = textBrush;
                NewTextColor = textBrush;
            }
            else
            {
                MessageBox.Show("Invalid color format. Please use #RRGGBB format.");
            }
        }
    }
}