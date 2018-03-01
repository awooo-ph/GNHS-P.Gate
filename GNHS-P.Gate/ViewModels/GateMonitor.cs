using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Models;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GNHSP.Gate.Properties;
using Microsoft.Win32;

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

                StartTimer();
            }
        }

        private async void StartTimer()
        {
            _lastChanged = DateTime.Now;

            if (_timeStarted)
                return;
            _timeStarted = true;

            while ((DateTime.Now - _lastChanged).TotalMilliseconds < 7777)
                await TaskEx.Delay(111);

            _Student = null;
            OnPropertyChanged(nameof(Student));
            OnPropertyChanged(nameof(HasStudent));
            _timeStarted = false;
        }

        private bool _timeStarted;
        private DateTime _lastChanged;
        
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
        
        public string Message => (Pass?.In ?? false) ? "WELCOME": "GOODBYE";
        

        public bool HasStudent => Student != null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
