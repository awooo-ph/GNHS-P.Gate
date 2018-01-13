using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace GNHSP.Gate.Views
{
    /// <summary>
    /// Interaction logic for AlertDialog.xaml
    /// </summary>
    public partial class AlertDialog : UserControl
    {
        public AlertDialog(string title, string message = "", PackIconKind icon = PackIconKind.Alert)
        {
            InitializeComponent();
            Title.Text = title;
            Message.Text = message;
            Icon.Kind = icon;
        }

    }
}
