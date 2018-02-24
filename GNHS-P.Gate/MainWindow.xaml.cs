using System;
using System.Windows;
using GNHSP.Gate.Properties;

namespace GNHSP.Gate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Settings.Default.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != nameof(Settings.Default.FullScreen)) return;
                if (Settings.Default.FullScreen)
                {
                    WindowState = WindowState.Maximized;
                    WindowStyle = WindowStyle.None;
                }
                else
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                }
            };
        }
        
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (Settings.Default.FullScreen)
            {
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
            }
        }

        //private Magic _logGenie;
        //private void HistoryButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    _logGenie.IsGenieOut = !_logGenie.IsGenieOut;
        //}

        //private async void EditButtonClicked(object sender, RoutedEventArgs e)
        //{
        //    if (_logGenie.IsGenieOut)
        //    {
        //        _logGenie.IsGenieOut = false;
        //        while (StudentLog.Visibility == Visibility.Visible)
        //        {
        //            await TaskEx.Delay(10);
        //        }
        //        await TaskEx.Delay(400);
        //    }
            
        //    _logGenie.Lamp =(Button) sender;
        //    _logGenie.IsGenieOut = true;
        //}
    }
}
