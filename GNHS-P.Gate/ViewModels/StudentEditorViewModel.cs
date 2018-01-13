using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GNHSP.Gate.Models;

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
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
