using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;

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
            
            // Enable hardware acceleration for better performance
            if (RenderCapability.Tier > 0)
            {
                // Set high quality bitmap scaling when hardware acceleration is available
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            }
            
            // Set up timer to close splash screen after 3 seconds
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
} 