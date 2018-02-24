using System.Data.Models;
using System.Windows;
using System.Windows.Controls;

namespace GNHSP.Gate.Views
{
    /// <summary>
    /// Interaction logic for Users.xaml
    /// </summary>
    public partial class Users : UserControl
    {
        public Users()
        {
            InitializeComponent();
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            var usr = Password.DataContext as User;
            if (usr == null) return;
            if (Password.Password.Length > 0)
                usr.Password = Password.Password;
        }
    }
}
