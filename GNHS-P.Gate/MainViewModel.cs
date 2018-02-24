using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
           Messenger.Default.AddListener<string>(Messages.Scan, id =>
           {
               var stud = Student.Cache.FirstOrDefault(x => x.Barcode.ToUpper() == id.ToUpper());
               if (stud == null) return;
               GateMonitor.Student = stud;
               var pass = GatePass.GetPreviousPass(stud.Id);
               if (pass == null)
               {
                   pass = new GatePass(stud.Id);                  
               }
               else
               {
                   pass = new GatePass(stud.Id,!pass.In);
               }
               pass.Save();
               GateMonitor.Pass = pass;
           });
            
            Messenger.Default.AddListener<User>(Messages.ModelDeleted,
                usr =>
                {
                    MessageQueue.Enqueue("User deleted", "UNDO", u =>
                    {
                        u.Undelete();
                    }, usr);
                    CurrentUser = null;
                });
        }

        private bool _ShowLog;

        public bool ShowLog
        {
            get => _ShowLog;
            set
            {
                if(value == _ShowLog)
                    return;
                _ShowLog = value;
                OnPropertyChanged(nameof(ShowLog));
        private bool _ShowStudents;

        public bool ShowStudents
        {
            get => _ShowStudents;
            set
            {
                if(value == _ShowStudents)
                    return;
                if (value && CurrentUser == null)
                {
                    MainViewModel.Instance.ShowLogin(((MainWindow) Application.Current.MainWindow).DialogHost);
                    return;
                }
                _ShowStudents = value;
                OnPropertyChanged(nameof(ShowStudents));
            }
        }

        private ICommand _ToggleShowLogCommand;

        public ICommand ToggleShowLogCommand => _ToggleShowLogCommand ?? 
                                                (_ToggleShowLogCommand = new DelegateCommand(
                                                        d =>
                                                        {
                                                            ShowLog = !ShowLog;
                                                        }));

        private GateMonitor _gateMonitor;
        public GateMonitor GateMonitor => _gateMonitor ?? (_gateMonitor = new GateMonitor());
        
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
                if(value==null)
                    Keyboard.UnHook();
                else
                    Keyboard.Hook(App.Current.MainWindow);
            }
        }

        private bool _loginShown;
        private DialogHost RootDialog;
        public async void ShowLogin(DialogHost host)
        {
            if (RootDialog == null && host != null)
                RootDialog = host;
            if (RootDialog == null) return;
            if (CurrentUser != null) return;
            if (_loginShown) return;
            _loginShown = true;
            var login = new Views.Login();
            RootDialog.DialogContent = login;
            RootDialog.IsOpen = true;
            var cancelled = true;
            RootDialog.DialogClosing += (sender, args) =>
            {
                if (!((bool) args.Parameter)) return;
                var usr = User.Cache.FirstOrDefault(x => x.Username.ToLower() == login.Username.Text.ToLower());
                if (usr == null && User.Cache.Count == 0)
                {
                    usr = new User()
                    {
                        Username = login.Username.Text,
                        IsAdmin = true,
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
                cancelled = false;
            };
                
            while (RootDialog.IsOpen)
            {
                await TaskEx.Delay(100);
            }
            
            _loginShown = false;
            if (CurrentUser != null || cancelled) return;
            
            var alert = new AlertDialog("Authentication Failed","The username and password you entered is invalid.");
            RootDialog.DialogContent = alert;
            RootDialog.IsOpen = true;

            while(RootDialog.IsOpen)
            {
                await TaskEx.Delay(100);
            }
            
            ShowLogin(RootDialog);
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
        }, d => CurrentUser?.IsAdmin ?? false));

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
        },d=>CurrentUser?.IsAdmin??false));

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
                }, d => CurrentUser?.IsAdmin ?? false));

        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue => _messageQueue ?? (_messageQueue = new SnackbarMessageQueue());
        
        public bool IsWaitingForScanner => Keyboard.IsWaitingForScanner;

        private ICommand _registerCommand;
        private bool _regListenerAdded;
        public ICommand RegisterCommand => _registerCommand ?? (_registerCommand = new DelegateCommand(d =>
        {
            if (!_regListenerAdded)
            {
                _regListenerAdded = true;
                Messenger.Default.AddListener(Messages.ScannerRegistered, () =>
                {
                    awooo.Context.Post(dd =>
                    {
                        OnPropertyChanged(nameof(IsWaitingForScanner));
                    },null);
                });
            }
            
            Keyboard.IsWaitingForScanner = true;
            OnPropertyChanged(nameof(IsWaitingForScanner));
        }));

        private ListCollectionView _logs;

        public ListCollectionView Logs
        {
            get
            {
                if (_logs != null) return _logs;
                _logs = new ListCollectionView(GatePass.Cache);
                _logs.CustomSort = new LogComparer();
                return _logs;
            }
        }

        class LogComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var log1 = x as GatePass;
                var log2 = y as GatePass;
                return log2.Time.CompareTo(log1.Time);
            }
        }

        private ICommand _newUserCommand;

        public ICommand NewUserCommand
        {
            get
            {
                if (_newUserCommand != null) return _newUserCommand;
                var vs = CollectionViewSource.GetDefaultView(User.Cache);
                _newUserCommand = new DelegateCommand(d =>
                {
                    var usr = new User() {Username = "NEW USER"};
                    User.Cache.Add(usr);
                    vs.MoveCurrentTo(usr);                    
                }, d => User.Cache.All(x => x.Id > 0));

                vs.CurrentChanged += (sender, args) =>
                {
                    var newUser = User.Cache.FirstOrDefault(x => x.Id == 0);
                    if (newUser == null)
                        return;
                    var cur = vs.CurrentItem as User;
                    if (newUser.Id != cur?.Id)
                        User.Cache.Remove(newUser);
                };
                
                return _newUserCommand;
            }
        }

        private bool _ChangePasswordVisible;

        public bool ChangePasswordVisible
        {
            get => _ChangePasswordVisible;
            set
            {
                if(value == _ChangePasswordVisible)
                    return;
                _ChangePasswordVisible = value;
                OnPropertyChanged(nameof(ChangePasswordVisible));
            }
        }

        
        private ICommand _ToggleChangePasswordCommand;

        public ICommand ToggleChangePasswordCommand =>
            _ToggleChangePasswordCommand ?? (_ToggleChangePasswordCommand = new DelegateCommand(
                d =>
                {
                    ChangePasswordVisible = !ChangePasswordVisible;
                }));

        private bool _IsSettingsVisible;

        public bool IsSettingsVisible
        {
            get => _IsSettingsVisible;
            set
            {
                if(value == _IsSettingsVisible)
                    return;

                if (value && CurrentUser == null)
                {
                    MainViewModel.Instance.ShowLogin(((MainWindow)Application.Current.MainWindow).DialogHost);
                    return;
                }
                
                _IsSettingsVisible = value;
                OnPropertyChanged(nameof(IsSettingsVisible));
            }
        }

        private ICommand _CancelRegisterCommand;

        public ICommand CancelRegisterCommand =>
            _CancelRegisterCommand ?? (_CancelRegisterCommand = new DelegateCommand(
                d =>
                {
                    Keyboard.IsWaitingForScanner = false;
                    OnPropertyChanged(nameof(IsWaitingForScanner));
                }));

        private ICommand _logoutCommand;

        public ICommand LogoutCommand => _logoutCommand ?? (_logoutCommand = new DelegateCommand(d =>
        {
            CurrentUser = null;
            IsSettingsVisible = false;
            ShowLogin(null);
        }));
    }
}
