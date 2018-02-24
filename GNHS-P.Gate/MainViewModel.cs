using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GNHSP.Gate.ViewModels;
using GNHSP.Gate.Views;
using MaterialDesignThemes.Wpf;
using Settings = GNHSP.Gate.Properties.Settings;

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

               var message = "";
               if (pass.In)
               {
                   message = Settings.Default.EntranceMessage;
               }
               else
               {
                   message = Settings.Default.ExitMessage;
               }

               message = message.Replace("[NAME]", stud.Fullname)
                   .Replace("[TIME]", pass.Time.ToString("MMM d h:mm tt"));
               if(!string.IsNullOrWhiteSpace(stud.ContactNumber))
                SMS.Send(message,stud.ContactNumber,true);
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

            if (Settings.Default.NotifyAbsent)
            {
                if (!string.IsNullOrWhiteSpace(Settings.Default.AbsentTime))
                    Task.Factory.StartNew(AbsentReminder);
                else
                    CheckAttendance(DateTime.Now.AddDays(-1).Date);
            }
        }

        private async void AbsentReminder()
        {
            var cutoff = DateTime.Now;
            if (!DateTime.TryParse(Settings.Default.AbsentTime, out cutoff)) return;
            while (DateTime.Now < cutoff)
                await TaskEx.Delay(1111);
            CheckAttendance(DateTime.Now.Date);
        }

        private bool _checkingAttendance;
        private void CheckAttendance(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) return;
            if (string.IsNullOrWhiteSpace(Settings.Default.AbsentMessage)) return;
            if (_checkingAttendance) return;
            _checkingAttendance = true;
            Task.Factory.StartNew(async () =>
            {
                var students = Student.Cache.ToList();
                foreach (var student in students)
                {
                    if(string.IsNullOrWhiteSpace(student.ContactNumber)) continue;
                    if(student.DateRegistered.Date==date.Date || student.LastAbsentNotification.Date==date.Date) continue;
                    var previousPass = GatePass.GetPreviousPass(student.Id);
                    if (date == previousPass?.Time.Date) continue;
                    var message = Settings.Default.AbsentMessage
                        .Replace("[NAME]", student.Fullname)
                        .Replace("[TIME]", date.ToString("MMM d, yyyy"));
                    SMS.Send(message, student.ContactNumber);
                    student.Update(nameof(Student.LastAbsentNotification),date.Date);
                    await TaskEx.Delay(1111);
                }
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
                if (value)
                    InfoIndex = 1;
                else
                    InfoIndex = 0;
            }
        }

        private int _InfoIndex;

        public int InfoIndex
        {
            get => _InfoIndex;
            set
            {
                if(value == _InfoIndex)
                    return;
                _InfoIndex = value;
                OnPropertyChanged(nameof(InfoIndex));
            }
        }

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
                _students.Filter = FilterStudent;
                return _students;
            }
        }

        private bool FilterStudent(object o)
        {
            if (!(o is Student stud)) return false;

            if (FilterGrade > 0)
            {
                var grade = (int) stud.Grade;
                if (FilterGrade +6 != grade)
                    return false;
            }
            if (!string.IsNullOrWhiteSpace(FilterSection))
            {
                if (stud.Section.ToLower() != FilterSection.ToLower())
                    return false;
            }
            if (string.IsNullOrWhiteSpace(SearchKeyword)) return true;
            return stud.Fullname.ToLower().Contains(SearchKeyword.ToLower());
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
        },d=>
        {
            return CurrentUser?.IsAdmin ?? false;
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
            ShowStudents = false;
        }));

        private string _SearchKeyword;

        public string SearchKeyword
        {
            get => _SearchKeyword;
            set
            {
                if(value == _SearchKeyword)
                    return;
                _SearchKeyword = value;
                OnPropertyChanged(nameof(SearchKeyword));
                Students.Refresh();
            }
        }

        private int _FilterGrade;

        public int FilterGrade
        {
            get => _FilterGrade;
            set
            {
                if(value == _FilterGrade)
                    return;
                _FilterGrade = value;
                OnPropertyChanged(nameof(FilterGrade));
                Students.Refresh();
            }
        }

        private string _FilterSection;

        public string FilterSection
        {
            get => _FilterSection;
            set
            {
                if(value == _FilterSection)
                    return;
                _FilterSection = value;
                OnPropertyChanged(nameof(FilterSection));
                Students.Refresh();
            }
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
}
