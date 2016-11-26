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

namespace ImageFolderFixer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {

        private System.Windows.Interop.WindowInteropHelper _windowInteropHelper;
        public IntPtr Handle { get { return _windowInteropHelper.Handle; } }

        public MainWindow()
        {
            InitializeComponent();

            _windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
                return;

            textBox.ScrollToEnd();
        }
    }
}
