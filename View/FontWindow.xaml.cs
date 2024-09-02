using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using WinForms = System.Windows.Forms;
using System.Windows.Data;
using System.Linq;

namespace SMW_Data.View
{
    public partial class FontsWindow : Window
    {
        public bool FontsOK { get; set; }
        public FontFamily ChangeFontTitle { get; set; }
        public FontFamily ChangeFontAuthor { get; set; }
        public FontFamily NewFontTitle => (FontFamily)ComboBoxFontTitle.SelectedItem;
        public FontFamily NewFontAuthor => (FontFamily)ComboBoxFontAuthor.SelectedItem;

        private readonly MainWindow mainWindow;

        public FontsWindow(MainWindow main)
        {
            Owner = main;
            mainWindow = main;
            InitializeComponent();

            var sortedFonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            ComboBoxFontTitle.ItemsSource = sortedFonts;
            ComboBoxFontAuthor.ItemsSource = sortedFonts;

            FontFamily CurrentFontTitle = mainWindow.CurrentFontTitle;
            FontFamily CurrentFontAuthor = mainWindow.CurrentFontAuthor;

            ComboBoxFontTitle.SelectedItem = sortedFonts.FirstOrDefault(f => f.Source == CurrentFontTitle.Source);
            ComboBoxFontAuthor.SelectedItem = sortedFonts.FirstOrDefault(f => f.Source == CurrentFontAuthor.Source);
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            FontsOK = true;
            ChangeFontTitle = NewFontTitle;
            ChangeFontAuthor = NewFontAuthor;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}