using System;
using System.Windows;
using System.Windows.Threading;

namespace WindowsOptimizerApp
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private DispatcherTimer timer;

        public SplashScreen()
        {
            InitializeComponent();
            
            // Set up timer to close splash screen after 3 seconds
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
} 