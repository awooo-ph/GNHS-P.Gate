using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GNHSP.Gate.Models;
using GNHSP.Gate.ViewModels;
using GNHSP.Gate.Views;
using MaterialDesignThemes.Wpf;

namespace GNHSP.Gate
{
    class MainViewModel : INotifyPropertyChanged
    {
        private MainViewModel()
        {
           // ShowLogin();
        }
        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private User _CurrentUser;

        public User CurrentUser
        {
            get => _CurrentUser;
            private set
            {
                if(value == _CurrentUser)
                    return;
                _CurrentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        private bool _loginShown;
        public async void ShowLogin(DialogHost host)
        {
            if (CurrentUser != null) return;
            if (_loginShown) return;
            _loginShown = true;
            var login = new Views.Login();
            host.DialogContent = login;
            host.IsOpen = true;
            
            host.DialogClosing += (sender, args) =>
            {
                if (!((bool) args.Parameter))
                    App.Current.Shutdown(0);
                var usr = User.Cache.FirstOrDefault(x => x.Username.ToLower() == login.Username.Text.ToLower());
                if (usr == null && User.Cache.Count == 0)
                {
                    usr = new User()
                    {
                        Username = login.Username.Text,
                    };
                    usr.Save();
                }
                if (usr == null) return;
                if (string.IsNullOrEmpty(usr.Password)) usr.Update(nameof(User.Password), login.Password.Password);
                if (usr.Password == login.Password.Password)
                {
                    CurrentUser = usr;
                }
                else
                {
                    CurrentUser = null;
                }
            };
                
            while (host.IsOpen)
            {
                await TaskEx.Delay(100);
            }

            if (CurrentUser != null) return;
            
            var alert = new AlertDialog("Authentication Failed","The username and password you entered is invalid.");
            host.DialogContent = alert;
            host.IsOpen = true;

            while(host.IsOpen)
            {
                await TaskEx.Delay(100);
            }
            _loginShown = false;
            ShowLogin(host);
        }

        private ListCollectionView _students;

        public ListCollectionView Students
        {
            get
            {
                if (_students != null) return _students;
                _students = (ListCollectionView) CollectionViewSource.GetDefaultView(Student.Cache);
                return _students;
            }
        }

        private ICommand _editStudentCommand;

        public ICommand EditStudentCommand => _editStudentCommand ?? (_editStudentCommand = new DelegateCommand<Student>(
        d =>
        {
            StudentEditor.Student = d;
            StudentEditor.IsOpen = true;
            StudentEditor.IsFlipped = false;
        }));

        private StudentEditorViewModel _StudentEditor = new StudentEditorViewModel();

        public StudentEditorViewModel StudentEditor
        {
            get => _StudentEditor;
            private set
            {
                if(value == _StudentEditor)
                    return;
                _StudentEditor = value;
                OnPropertyChanged(nameof(StudentEditor));
            }
        }

        private ICommand _addStudentCommand;

        public ICommand AddStudentCommand => _addStudentCommand ?? (_addStudentCommand = new DelegateCommand(d =>
        {
            StudentEditor.Student = new Student();
            StudentEditor.IsFlipped = false;
            StudentEditor.IsOpen = true;
        }));

        private ICommand _deleteAllStudentsCommand;

        public ICommand DeleteAllStudentsCommand =>
            _deleteAllStudentsCommand ?? (_deleteAllStudentsCommand = new DelegateCommand(
                d =>
                {
                    var list = Student.Cache.ToList();
                    foreach (var student in list)
                    {
                        student.Delete();
                    }
                    MessageQueue.Enqueue("All students have been deleted.","UNDO",
                        students =>
                        {
                            foreach (var student in students)
                                student.Undelete();
                        }
                        ,list, true);
                }));

        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue => _messageQueue ?? (_messageQueue = new SnackbarMessageQueue());
    }
}
