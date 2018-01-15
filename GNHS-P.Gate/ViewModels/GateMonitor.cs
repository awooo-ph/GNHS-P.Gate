using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using GNHSP.Gate.Models;

namespace GNHSP.Gate.ViewModels
{
    class GateMonitor : INotifyPropertyChanged
    {
        private Student _Student;

        public Student Student
        {
            get => _Student;
            set
            {
                _Student = value;
                OnPropertyChanged(nameof(Student));
                OnPropertyChanged(nameof(HasStudent));
            }
        }

        private Access _Access;

        public Access Access
        {
            get => _Access;
            set
            {
                _Access = value;
                OnPropertyChanged(nameof(Access));
                OnPropertyChanged(nameof(Message));
            }
        }

        private GatePass _Pass;

        public GatePass Pass
        {
            get => _Pass;
            set
            {
                _Pass = value;
                OnPropertyChanged(nameof(Pass));
                OnPropertyChanged(nameof(Message));
            }
        }
        
        public string Message => (Pass?.In ?? false) ? "GOODBYE": "WELCOME";
        

        public bool HasStudent => Student != null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
