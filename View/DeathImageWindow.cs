using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace SMW_Data.View
{
    public partial class DeathImageWindow : Window
    {
        public bool DeathImageOK { get; set; }
        private readonly MainWindow mainWindow;

        public DeathImageWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();

            int savedIndex = mainWindow.SelectedDeathImageIndex;
            ComboBoxDeathImage.SelectedIndex = savedIndex;
            SetImageBasedOnSelection(savedIndex);
        }

        private void ComboBoxDeathImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ComboBoxDeathImage.SelectedIndex;
            SetImageBasedOnSelection(selectedIndex);
        }

        private void SetImageBasedOnSelection(int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMB1.png"));
                    break;
                case 1:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMB3.png"));
                    break;
                case 2:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMW.png"));
                    break;
                case 3:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("pack://application:,,,/images/Paper Mario.png"));
                    break;
                default:
                    Image_MarioDeath.Source = new BitmapImage(new Uri("pack://application:,,,/images/SMW.png"));
                    break;
            }
        }

        private void ButtonDeathImageOK_Click(object sender, RoutedEventArgs e)
        {
            DeathImageOK = true;
            mainWindow.SelectedDeathImageIndex = ComboBoxDeathImage.SelectedIndex;
            this.Close();
        }

        private void ButtonDeathImageCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}