using System.Drawing;
using System.Windows;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace SMW_Data.View
{
    public partial class TimerWindow : Window
    {
        public bool TimerOK { get; set; }

        private readonly MainWindow mainWindow;

        public TimerWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();
            
            ComboBoxLevelAccuracy.SelectedItem = ComboBoxLevelAccuracy.Items[0];
            ComboBoxLastLevelAccuracy.SelectedItem = ComboBoxLastLevelAccuracy.Items[0];
            ComboBoxTotalAccuracy.SelectedItem = ComboBoxTotalAccuracy.Items[0];

        }
        private void ButtonTimerOK_Click(object sender, RoutedEventArgs e)
        {
            TimerOK = true;
            this.Close();
        }

        private void ButtonTimerCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}