using System;
using System.ComponentModel;
using System.Data.Models;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;

namespace GNHSP.Gate.ViewModels
{
    class StudentEditorViewModel : INotifyPropertyChanged
    {
        private bool _IsFlipped;

        public bool IsFlipped
        {
            get => _IsFlipped;
            set
            {
                if(value == _IsFlipped)
                    return;
                _IsFlipped = value;
                OnPropertyChanged(nameof(IsFlipped));
            }
        }

        public string Title => Student?.Id > 0 ? "EDIT STUDENT" : "NEW STUDENT";

        private bool _IsOpen;

        public bool IsOpen
        {
            get => _IsOpen;
            set
            {
                if(value == _IsOpen)
                    return;
                _IsOpen = value;
                OnPropertyChanged(nameof(IsOpen));
            }
        }

        private ICommand _closeCommand;

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new DelegateCommand(d =>
        {
            IsOpen = false;
        }));

        private ICommand _deleteCommand;

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new DelegateCommand(d =>
        {
            IsOpen = false;
            MainViewModel.Instance.MessageQueue.Enqueue("Student deleted.", "UNDO", s => s.Undelete(), Student, true);
            Student.Delete(false);                       
        },d=>Student?.CanDelete()??false));

        private ICommand _toggleFlipCommand;

        public ICommand ToggleFlipCommand => _toggleFlipCommand ?? (_toggleFlipCommand = new DelegateCommand(d =>
        {
            IsFlipped = !IsFlipped;
        }));

        private Student _Student;

        public Student Student
        {
            get => _Student;
            set
            {
                if(value == _Student)
                    return;
                _Student = value;
                OnPropertyChanged(nameof(Student));
                OnPropertyChanged(nameof(Title));
                Logs.Refresh();
            }
        }

        private ICommand _changePictureCommand;

        public ICommand ChangePictureCommand => _changePictureCommand ?? (_changePictureCommand = new DelegateCommand(
            d =>
            {
                var dialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = @"All Images|*.BMP;*.JPG;*.JPEG;*.GIF;*.PNG|
                            BMP Files|*.BMP;*.DIB;*.RLE|
                            JPEG Files|*.JPG;*.JPEG;*.JPE;*.JFIF|
                            GIF Files|*.GIF|
                            PNG Files|*.PNG",
                    Title = "Select Picture",
                };
                if (!(dialog.ShowDialog() ?? false))
                    return;

                Student.Picture = File.ReadAllBytes(dialog.FileName);
            }));

        private ListCollectionView _logs;

        public ListCollectionView Logs
        {
            get
            {
                if (_logs != null)
                    return _logs;
                _logs = new ListCollectionView(GatePass.Cache);
                _logs.CustomSort = new LogComparer();
                _logs.Filter = FilterLog;
                return _logs;
            }
        }

        private bool FilterLog(object o)
        {
            if (!(o is GatePass log)) return false;
            return log.StudentId == Student?.Id;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
