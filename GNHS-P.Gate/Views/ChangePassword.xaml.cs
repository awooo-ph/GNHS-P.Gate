using System.Windows;
using System.Windows.Controls;
using GNHSP.Gate.Models;

namespace GNHSP.Gate.Views
{
    /// <summary>
    /// Interaction logic for ChangePassword.xaml
    /// </summary>
    public partial class ChangePassword : UserControl
    {
        public ChangePassword()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainViewModel.Instance.CurrentUser == null) return;
            
            if (MainViewModel.Instance.CurrentUser.Password != CurrentPassword.Password ||
                NewPassword.Password != NewPassword2.Password || NewPassword.Password.Length == 0)
            {
                InvalidPasswordMessage.Visibility = Visibility.Visible;
                return;
            }

            InvalidPasswordMessage.Visibility = Visibility.Hidden;
            MainViewModel.Instance.CurrentUser.Update(nameof(User.Password),NewPassword.Password);
            CancelClicked(null,e);
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.ChangePasswordVisible = false;
            InvalidPasswordMessage.Visibility = Visibility.Hidden;
            CurrentPassword.Password = "";
            NewPassword.Password = "";
            NewPassword2.Password = "";
        }
    }
}
