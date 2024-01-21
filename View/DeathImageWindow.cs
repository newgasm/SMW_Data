using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

namespace SMW_Data.View
{
    public partial class DeathImageWindow : Window
    {
        public bool DeathImageOK { get; set; }
        public ComboBoxItem selectedDeathImage;
        private readonly MainWindow mainWindow;

        public DeathImageWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();
            ComboBoxDeathImage.SelectedItem = ComboBoxDeathImage.Items[0];
        }

        private void ComboBoxDeathImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ComboBoxDeathImage.SelectedIndex)
            {
                case 0:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/SMB1.png"));
                    //Image_MarioDeath.Source = new BitmapImage(new Uri("SMB1.png", UriKind.Relative));
                    break;
                case 1:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/SMB2.png"));
                    break;
                case 2:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/SMB3.png"));
                    break;
                case 3:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/SMW.png"));
                    break;
                case 4:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/Paper Mario.png"));
                    break;
                default:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("C:/Users/newgasm/Desktop/Programming/C#/SMW Data/SMW.png"));
                    break;
            }
        }

        private void ButtonDeathImageOK_Click(object sender, RoutedEventArgs e)
        {
            DeathImageOK = true;

            this.Close();
        }

        private void ButtonDeathImageCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}