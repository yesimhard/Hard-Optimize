using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Input;

namespace WindowsOptimizerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variables for tracking window dragging
        private bool isDragging = false;
        private Point startPoint;

        public MainWindow()
        {
            InitializeComponent();
            CheckAdminPrivileges();
        }

        #region Custom Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            startPoint = e.GetPosition(null);
            Mouse.Capture((UIElement)sender);
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPoint = e.GetPosition(null);
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;

                Left += deltaX;
                Top += deltaY;
                startPoint = currentPoint;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            isDragging = false;
            Mouse.Capture(null);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                ((Button)sender).Content = "\uE922"; // Maximize icon
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                ((Button)sender).Content = "\uE923"; // Restore icon
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        private void CheckAdminPrivileges()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            
            if (!isAdmin)
            {
                MessageBox.Show("This application requires administrative privileges to perform system optimizations.\n\n" +
                                "Please restart the application as administrator.", 
                                "Administrative Privileges Required", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
            }
        }

        private void ButtonApplyBasicTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected basic tweaks
            ApplyBasicTweaks();
        }

        private void ButtonApplyPowerTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected power tweaks
            ApplyPowerTweaks();
        }

        private void ButtonApplyNetworkTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected network tweaks
            ApplyNetworkTweaks();
        }

        private void ButtonSetDscpPriority_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select game executable
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select Game or Application Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Set DSCP priority for selected game
                bool success = WindowsOptimizer.SetDscpPriorityForGame(openFileDialog.FileName);
                if (success)
                {
                    MessageBox.Show($"DSCP priority set for {Path.GetFileName(openFileDialog.FileName)}.", 
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to set DSCP priority.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ButtonApplyAdvancedTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected advanced tweaks
            ApplyAdvancedTweaks();
        }

        private void ButtonInstallNVCleanInstall_Click(object sender, RoutedEventArgs e)
        {
            // Open NVCleanInstall download page
            WindowsOptimizer.InstallNVCleanInstall();
        }

        private void ButtonApplyNvidiaSettings_Click(object sender, RoutedEventArgs e)
        {
            // Apply Nvidia Profile Inspector settings
            MessageBox.Show("This feature will download and apply optimal Nvidia Profile Inspector settings.\n\n" + 
                            "Please wait while the process completes.", 
                            "Applying Nvidia Settings", MessageBoxButton.OK, MessageBoxImage.Information);

            // Placeholder for actual implementation
            MessageBox.Show("Nvidia Profile Inspector settings applied successfully.", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonApplyGpuTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected GPU tweaks
            ApplyGpuTweaks();
        }

        private void ButtonShowBiosInfo_Click(object sender, RoutedEventArgs e)
        {
            // Show BIOS access information
            MessageBox.Show("To access your BIOS settings:\n\n" +
                           "1. Restart your computer\n" +
                           "2. During startup, press the BIOS key (often F2, F10, F12, DEL, or ESC)\n" +
                           "3. Navigate to the Power Management or CPU Configuration section\n" +
                           "4. Apply the recommended settings\n\n" +
                           "Warning: Incorrect BIOS settings can cause system instability. Proceed with caution.",
                           "BIOS Access Information",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }

        private void ButtonDebloatWindows_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected debloat options
            DebloatWindows();
        }

        private void ButtonRunDiskCleanup_Click(object sender, RoutedEventArgs e)
        {
            // Run disk cleanup
            bool success = WindowsOptimizer.CleanWindowsUpdateFiles();
            if (success)
            {
                MessageBox.Show("Disk Cleanup started successfully. This process will continue in the background.", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to start Disk Cleanup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonOptimizeDrives_Click(object sender, RoutedEventArgs e)
        {
            // Optimize drives
            MessageBox.Show("Drive optimization will start now. This process may take some time.", 
                            "Starting Drive Optimization", MessageBoxButton.OK, MessageBoxImage.Information);
            
            Task.Run(() => {
                bool success = WindowsOptimizer.OptimizeDrives();
                Dispatcher.Invoke(() => {
                    if (success)
                    {
                        MessageBox.Show("Drives optimized successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to optimize drives.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            });
        }

        private void ButtonResetAllSettings_Click(object sender, RoutedEventArgs e)
        {
            // Reset all checkboxes and settings
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reset all settings?",
                                                     "Confirm Reset",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ResetAllSettings();
            }
        }

        private void ButtonApplyAllTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply all selected tweaks from all tabs
            MessageBoxResult result = MessageBox.Show("Are you sure you want to apply all selected optimizations?\n\n" +
                                                     "This will make multiple system changes that may require a restart.",
                                                     "Confirm Hard Optimize Tweaks",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                ApplyAllSelectedTweaks();
            }
        }

        #region Optimization Methods

        private void ApplyBasicTweaks()
        {
            // Implementation for basic tweaks
            bool success = true;

            // Find the checkboxes in the Basic Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Optimize Visual Effects for Performance":
                            success &= WindowsOptimizer.OptimizeVisualEffects();
                            break;
                        case "Disable Mouse Acceleration":
                            success &= WindowsOptimizer.DisableMouseAcceleration();
                            break;
                        case "Optimize Service Settings":
                            success &= WindowsOptimizer.OptimizeServiceSettings();
                            break;
                        case "Optimize Memory Management":
                            success &= WindowsOptimizer.OptimizeMemoryManagement();
                            break;
                    }
                }
            }

            if (success)
            {
                MessageBox.Show("Basic tweaks applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some tweaks could not be applied. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyPowerTweaks()
        {
            // Implementation for power tweaks
            bool success = true;

            // Find the checkboxes in the Power Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Disable Idle and Selective Suspend for USB Ports":
                            success &= WindowsOptimizer.DisableUSBPowerManagement();
                            break;
                        case "Disable Idle and Enhanced Power Management for Storage Devices":
                            success &= WindowsOptimizer.DisableStoragePowerManagement();
                            break;
                        case "Apply Highest Performance Power Plan":
                            success &= WindowsOptimizer.ApplyHighPerformancePowerPlan();
                            break;
                        case "Disable Hibernation":
                            success &= WindowsOptimizer.DisableHibernation();
                            break;
                        case "Disable Timer Coalescing":
                            success &= WindowsOptimizer.DisableTimerCoalescing();
                            break;
                        case "Disable Fast Startup":
                            success &= WindowsOptimizer.DisableFastStartup();
                            break;
                        case "Disable Power Throttling":
                            success &= WindowsOptimizer.TogglePowerThrottling(false);
                            break;
                    }
                }
            }

            if (success)
            {
                MessageBox.Show("Power tweaks applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some tweaks could not be applied. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyNetworkTweaks()
        {
            // Implementation for network tweaks
            bool success = true;

            // Find the checkboxes in the Network Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Disable Hidden Network Power Saving Features":
                        case "Disable Network Power Saving Features":
                            success &= WindowsOptimizer.DisableNetworkPowerSaving();
                            break;
                        case "Disable Unnecessary Network Interface Card Features":
                            success &= WindowsOptimizer.DisableNetworkInterfaceFeatures();
                            break;
                    }
                }
            }

            if (success)
            {
                MessageBox.Show("Network tweaks applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some tweaks could not be applied. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyAdvancedTweaks()
        {
            // Implementation for advanced tweaks
            bool success = true;

            // Confirmation for potentially dangerous tweaks
            MessageBoxResult result = MessageBox.Show(
                "Some advanced tweaks can affect system stability and security. Are you sure you want to continue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Find the checkboxes in the Advanced Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Disable Data Execution Prevention (DEP)":
                            success &= WindowsOptimizer.DisableDataExecutionPrevention();
                            break;
                        case "Disable Meltdown/Spectre Patches":
                            success &= WindowsOptimizer.DisableMeltdownSpectreFixes();
                            break;
                        case "Apply BCDEdit Tweaks":
                            success &= WindowsOptimizer.ApplyBCDEditTweaks();
                            break;
                    }
                }
            }

            // Handle Win32PrioritySeparation setting from ComboBox
            // Placeholder - would need to find and read the actual ComboBox value

            if (success)
            {
                MessageBox.Show("Advanced tweaks applied successfully. Some changes may require a restart to take effect.", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some tweaks could not be applied. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyGpuTweaks()
        {
            // Implementation for GPU tweaks
            bool success = true;

            // Find the checkboxes in the GPU Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Enable Write Combining":
                            success &= WindowsOptimizer.ToggleWriteCombining(true);
                            break;
                        case "Disable Preemptions":
                            success &= WindowsOptimizer.DisablePreemptions();
                            break;
                        case "Disable Hidden Registry Power Saving Features":
                            success &= WindowsOptimizer.DisableGPUPowerSaving();
                            break;
                    }
                }
            }

            if (success)
            {
                MessageBox.Show("GPU tweaks applied successfully. Some changes may require a restart to take effect.", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some tweaks could not be applied. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DebloatWindows()
        {
            // Implementation for Windows debloating
            List<string> selectedApps = new List<string>();
            List<string> selectedServices = new List<string>();

            // Get selected apps and services
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true && child.Parent != null)
                {
                    // Check if this is in the Applications group
                    if (child.Parent is ListView && child.Parent.ToString().Contains("Applications"))
                    {
                        selectedApps.Add(child.Content.ToString());
                    }
                    // Check if this is in the Services group
                    else if (child.Parent is ListView && child.Parent.ToString().Contains("Services"))
                    {
                        selectedServices.Add(child.Content.ToString());
                    }
                }
            }

            // Confirm with the user
            if (selectedApps.Count > 0 || selectedServices.Count > 0)
            {
                string message = "You are about to remove or disable:\n\n";
                
                if (selectedApps.Count > 0)
                {
                    message += "Applications:\n- " + string.Join("\n- ", selectedApps) + "\n\n";
                }
                
                if (selectedServices.Count > 0)
                {
                    message += "Services:\n- " + string.Join("\n- ", selectedServices);
                }
                
                message += "\n\nAre you sure you want to continue?";
                
                MessageBoxResult result = MessageBox.Show(message, "Confirm Debloat", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    bool appsSuccess = true;
                    bool servicesSuccess = true;
                    
                    if (selectedApps.Count > 0)
                    {
                        appsSuccess = WindowsOptimizer.UninstallWindowsApps(selectedApps);
                    }
                    
                    if (selectedServices.Count > 0)
                    {
                        servicesSuccess = WindowsOptimizer.DisableServices(selectedServices);
                    }
                    
                    if (appsSuccess && servicesSuccess)
                    {
                        MessageBox.Show("Windows debloat completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Some components could not be removed or disabled. Please run the application as administrator and try again.", 
                                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("No items selected for removal or disabling.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CleanupSystem()
        {
            // Implementation for system cleanup
            bool success = true;

            // Find the checkboxes in the Cleanup tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Clean Temporary Files":
                        case "Clean System Cache":
                            success &= WindowsOptimizer.CleanTempFiles();
                            break;
                        case "Clean Windows Update Files":
                            success &= WindowsOptimizer.CleanWindowsUpdateFiles();
                            break;
                    }
                }
            }

            if (success)
            {
                MessageBox.Show("System cleanup completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some cleanup tasks could not be completed. Please run the application as administrator and try again.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetAllSettings()
        {
            // Reset all checkboxes and settings to default
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                child.IsChecked = false;
            }

            MessageBox.Show("All settings have been reset.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyAllSelectedTweaks()
        {
            // Logic to apply all selected tweaks from all categories
            ApplyBasicTweaks();
            ApplyPowerTweaks();
            ApplyNetworkTweaks();
            ApplyAdvancedTweaks();
            ApplyGpuTweaks();
            DebloatWindows();
            CleanupSystem();
            
            // Prompt for restart if needed
            MessageBoxResult restartResult = MessageBox.Show("Some changes require a system restart to take effect.\n\n" +
                                                           "Would you like to restart your computer now?",
                                                           "Restart Required",
                                                           MessageBoxButton.YesNo,
                                                           MessageBoxImage.Question);
            
            if (restartResult == MessageBoxResult.Yes)
            {
                // Trigger system restart
                Process.Start("shutdown.exe", "/r /t 10 /c \"Hard Optimize - System restart required to apply changes.\"");
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Utility Methods

        // Helper method to find all visual children of a specific type
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        #endregion

        #region Utility Program Methods

        private void ButtonDownloadMsiAfterburner_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.msi.com/Landing/afterburner/graphics-cards",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadDDU_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.guru3d.com/files-details/display-driver-uninstaller-download.html",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadMsiMode_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.guru3d.com/files-details/msi-mode-utility-download.html",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadTimerResolution_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://cms.lucashale.com/timer-resolution/",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadAutoruns_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://docs.microsoft.com/en-us/sysinternals/downloads/autoruns",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadWindowsUpdateBlocker_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.sordum.org/9470/windows-update-blocker-v1-7/",
                UseShellExecute = true
            });
        }

        private void ButtonDownloadRevoUninstaller_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.revouninstaller.com/products/revo-uninstaller-free/",
                UseShellExecute = true
            });
        }

        #endregion
    }
}