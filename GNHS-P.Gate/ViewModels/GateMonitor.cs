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
                if(value == _Student)
                    return;
                _Student = value;
                OnPropertyChanged(nameof(Student));
                OnPropertyChanged(nameof(HasStudent));
            }
        }

        public bool HasStudent => Student != null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
