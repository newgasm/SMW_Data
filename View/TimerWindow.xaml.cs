using System.Windows;

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
            ComboBoxLevelAccuracy.SelectedIndex = mainWindow.SelectedLevelAccuracyIndex;
            ComboBoxTotalAccuracy.SelectedIndex = mainWindow.SelectedTotalAccuracyIndex;
        }
        private void ButtonTimerOK_Click(object sender, RoutedEventArgs e)
        {
            TimerOK = true;
            mainWindow.SelectedLevelAccuracyIndex = ComboBoxLevelAccuracy.SelectedIndex;
            mainWindow.SelectedTotalAccuracyIndex = ComboBoxTotalAccuracy.SelectedIndex;
            this.Close();
        }

        private void ButtonTimerCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}