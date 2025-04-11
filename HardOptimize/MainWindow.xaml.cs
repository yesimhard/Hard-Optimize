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
using System.Windows.Media;
using System.Linq;
using System.Text;

namespace WindowsOptimizerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Track which optimization categories have been applied for reversion
        private readonly Dictionary<string, bool> _appliedOptimizationCategories = new Dictionary<string, bool>
        {
            { "BasicTweaks", false },
            { "PowerTweaks", false },
            { "NetworkTweaks", false },
            { "AdvancedTweaks", false },
            { "GpuTweaks", false },
            { "VersionTweaks", false }
        };

        public MainWindow()
        {
            InitializeComponent();
            CheckAdminPrivileges();
            
            // Enable hardware acceleration for better performance
            if (RenderCapability.Tier > 0)
            {
                // Set high quality bitmap scaling when hardware acceleration is available
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            }

            // Detect Windows version for compatibility checks
            CheckWindowsVersion();
        }

        private void CheckWindowsVersion()
        {
            try
            {
                var versionInfo = new WindowsVersionOptimizer.WindowsVersionInfo();
                WindowsVersionText.Text = $"Version: {versionInfo.VersionString}";
                WindowsEditionText.Text = $"Edition: {versionInfo.GetWindowsEdition()}";
                
                // Load version-specific recommendations
                string[] recommendations = WindowsVersionOptimizer.GetVersionSpecificRecommendations();
                RecommendationsList.ItemsSource = recommendations;
                
                // Log the detected Windows version
                WindowsOptimizer.LogAction("Windows version check", true, versionInfo.VersionString);
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction("Windows version check", false, ex.Message);
                WindowsVersionText.Text = "Error detecting Windows version";
                WindowsEditionText.Text = string.Empty;
            }
        }

        #region Custom Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                try
                {
                    // Use DragMove instead of custom dragging logic
                    this.DragMove();
                }
                catch (InvalidOperationException)
                {
                    // Ignore if DragMove fails
                }
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            // Not needed when using DragMove
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
            // Create a system restore point before applying tweaks
            bool restorePointCreated = WindowsOptimizer.CreateRestorePoint("Hard Optimize - Before Basic Tweaks");
            if (!restorePointCreated)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Failed to create a system restore point. It's recommended to create a restore point before applying optimizations.\n\n" +
                    "Do you want to continue without a restore point?",
                    "Restore Point Creation Failed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            
            // Apply selected basic tweaks with message
            ApplyBasicTweaks(true);
        }

        private void ButtonApplyPowerTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected power tweaks with message
            ApplyPowerTweaks(true);
        }

        private void ButtonApplyNetworkTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected network tweaks with message
            ApplyNetworkTweaks(true);
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
            // Apply selected advanced tweaks with message
            ApplyAdvancedTweaks(true);
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

            try
            {
                string downloadPath = Path.Combine(Path.GetTempPath(), "NvidiaProfileInspector.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), "NvidiaProfileInspector");
                string npiExePath = Path.Combine(extractPath, "nvidiaProfileInspector.exe");
                string profilePath = Path.Combine(extractPath, "OptimalGameProfile.nip");
                
                // Ensure the extraction directory exists
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }
                
                // Download NvidiaProfileInspector if not already downloaded
                if (!File.Exists(npiExePath))
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadFile("https://github.com/Orbmu2k/nvidiaProfileInspector/releases/download/2.3.0.13/nvidiaProfileInspector.zip", downloadPath);
                    }
                    
                    // Extract the ZIP file
                    System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, extractPath, true);
                }
                
                // Create the optimal game profile file
                string profileContent = @"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfProfile>
  <Profile>
    <ProfileName>Base Profile</ProfileName>
    <Settings>
      <Setting>
        <SettingID>8294498</SettingID>
        <SettingValue>1</SettingValue>
        <SettingName>Max Frame Rate</SettingName>
      </Setting>
      <Setting>
        <SettingID>983226</SettingID>
        <SettingValue>1</SettingValue>
        <SettingName>Low Latency Mode</SettingName>
      </Setting>
      <Setting>
        <SettingID>983289</SettingID>
        <SettingValue>4</SettingValue>
        <SettingName>Power Management Mode</SettingName>
      </Setting>
      <Setting>
        <SettingID>983482</SettingID>
        <SettingValue>1</SettingValue>
        <SettingName>Texture filtering - Negative LOD bias</SettingName>
      </Setting>
      <Setting>
        <SettingID>1413190</SettingID>
        <SettingValue>1</SettingValue>
        <SettingName>Vertical Sync Tear Control</SettingName>
      </Setting>
    </Settings>
  </Profile>
</ArrayOfProfile>";

                // Write the profile to a file
                File.WriteAllText(profilePath, profileContent);
                
                // Run Nvidia Profile Inspector to import the profile
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = npiExePath;
                    process.StartInfo.Arguments = $"-silentImport \"{profilePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show("Nvidia Profile Inspector settings applied successfully.", 
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to apply Nvidia Profile Inspector settings.", 
                                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying Nvidia settings: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonApplyGpuTweaks_Click(object sender, RoutedEventArgs e)
        {
            // Apply selected GPU tweaks with message
            ApplyGpuTweaks(true);
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
            // Run disk cleanup with message
            CleanupSystem(true);
        }

        private void ButtonOptimizeDrives_Click(object sender, RoutedEventArgs e)
        {
            // Create a progress window
            var progressWindow = new Window
            {
                Title = "Drive Optimization Progress",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(15, 26, 44))
            };

            var progressGrid = new Grid();
            progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var textBlock = new TextBlock
            {
                Text = "Optimizing drives. This may take several minutes...",
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(textBlock, 0);

            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Margin = new Thickness(10),
                Height = 20,
                Width = 380
            };
            Grid.SetRow(progressBar, 1);

            var statusText = new TextBlock
            {
                Text = "Starting optimization...",
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(statusText, 2);

            progressGrid.Children.Add(textBlock);
            progressGrid.Children.Add(progressBar);
            progressGrid.Children.Add(statusText);
            progressWindow.Content = progressGrid;

            progressWindow.Show();

            // Start drive optimization in a background task
            Task.Run(() =>
            {
                bool success = false;
                try
                {
                    // Update status
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        statusText.Text = "Analyzing drives...";
                    });

                    // Get all fixed drives
                    DriveInfo[] drives = DriveInfo.GetDrives()
                        .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                        .ToArray();

                    for (int i = 0; i < drives.Length; i++)
                    {
                        DriveInfo drive = drives[i];
                        
                        // Update status with current drive
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            statusText.Text = $"Optimizing drive {drive.Name} ({i + 1}/{drives.Length})";
                        });

                        // Call the actual optimize method
                        WindowsOptimizer.OptimizeDrive(drive.Name[0]);
                    }

                    // Update status for cleanup
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        statusText.Text = "Finishing up...";
                    });

                    success = true;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error during drive optimization: {ex.Message}", 
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    // Close the progress window and show result
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressWindow.Close();
                        
                        if (success)
                        {
                            MessageBox.Show("Drive optimization completed successfully.", 
                                         "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });
                }
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
            // Ask user if they want to create a system restore point
            MessageBoxResult restoreResult = MessageBox.Show(
                "Would you like to create a system restore point before applying all tweaks?\n\n" +
                "This is recommended and will allow you to revert changes if needed.",
                "Create System Restore Point",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (restoreResult == MessageBoxResult.Yes)
            {
                // Show progress indicator for restore point creation
                var progressWindow = new Window
                {
                    Title = "Creating Restore Point",
                    Width = 400,
                    Height = 120,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow,
                    Background = new SolidColorBrush(Color.FromRgb(15, 26, 44))
                };

                var progressGrid = new Grid();
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                progressGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                var textBlock = new TextBlock
                {
                    Text = "Creating system restore point...",
                    Foreground = Brushes.White,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(textBlock, 0);

                var progressBar = new ProgressBar
                {
                    IsIndeterminate = true,
                    Margin = new Thickness(10),
                    Height = 20,
                    Width = 380
                };
                Grid.SetRow(progressBar, 1);

                progressGrid.Children.Add(textBlock);
                progressGrid.Children.Add(progressBar);
                progressWindow.Content = progressGrid;

                progressWindow.Show();

                // Create restore point in background
                Task.Run(() =>
                {
                    bool success = WindowsOptimizer.CreateRestorePoint();
                    Dispatcher.Invoke(() =>
                    {
                        progressWindow.Close();
                        
                        if (success)
                        {
                            MessageBox.Show("System restore point created successfully.\n\nProceeding with optimization...",
                                "Restore Point Created", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Continue with applying all tweaks
                            ApplyAllSelectedTweaks();
                        }
                        else
                        {
                            MessageBoxResult continueResult = MessageBox.Show(
                                "Failed to create system restore point. Do you want to continue with optimization anyway?",
                                "Restore Point Failed",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);
                                
                            if (continueResult == MessageBoxResult.Yes)
                            {
                                ApplyAllSelectedTweaks();
                            }
                        }
                    });
                });
            }
            else
            {
                // User chose not to create a restore point, proceed with applying tweaks
                ApplyAllSelectedTweaks();
            }
        }

        private void ViewLogButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a window to display the log
            var logWindow = new Window
            {
                Title = "Optimization Log",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(15, 26, 44))
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Log content display
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };
            
            var logTextBox = new TextBox
            {
                Text = WindowsOptimizer.GetLogContents(),
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(22, 38, 63)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap
            };
            
            scrollViewer.Content = logTextBox;
            Grid.SetRow(scrollViewer, 0);
            
            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var refreshButton = new Button
            {
                Content = "Refresh",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5)
            };
            refreshButton.Click += (s, args) => logTextBox.Text = WindowsOptimizer.GetLogContents();
            
            var clearButton = new Button
            {
                Content = "Clear Log",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5)
            };
            clearButton.Click += (s, args) => 
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to clear the log?",
                    "Confirm Clear Log",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    WindowsOptimizer.ClearLog();
                    logTextBox.Text = WindowsOptimizer.GetLogContents();
                }
            };
            
            var closeButton = new Button
            {
                Content = "Close",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5)
            };
            closeButton.Click += (s, args) => logWindow.Close();
            
            buttonPanel.Children.Add(refreshButton);
            buttonPanel.Children.Add(clearButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 1);
            
            grid.Children.Add(scrollViewer);
            grid.Children.Add(buttonPanel);
            logWindow.Content = grid;
            
            logWindow.ShowDialog();
        }

        #region Optimization Methods

        private void ApplyBasicTweaks(bool showMessage = true)
        {
            try
            {
                int successCount = 0;
                int totalCount = 0;
                StringBuilder resultMessage = new StringBuilder();
                
                // Find all CheckBox controls in the BasicTweaks tab
                var checkBoxes = FindVisualChildren<CheckBox>(this)
                    .Where(cb => cb.Parent != null && 
                           cb.Parent is Panel panel && 
                           panel.Parent != null && 
                           panel.Parent is GroupBox group && 
                           group.Header.ToString().Contains("Windows Settings"))
                    .ToList();
                
                if (checkBoxes.Count == 0)
                {
                    if (showMessage)
                    {
                        MessageBox.Show("No optimization options were found or selected.", 
                                      "No Action Taken", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    return;
                }
                
                // Start with mouse acceleration if selected
                var mouseCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Mouse Acceleration"));
                if (mouseCheckBox != null && mouseCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.DisableMouseAcceleration())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Mouse acceleration disabled successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to disable mouse acceleration");
                    }
                }
                
                // Visual effects
                var visualEffectsCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Visual Effects"));
                if (visualEffectsCheckBox != null && visualEffectsCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.OptimizeVisualEffects())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Visual effects optimized successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to optimize visual effects");
                    }
                }
                
                // Service settings
                var serviceCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Service Settings"));
                if (serviceCheckBox != null && serviceCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.OptimizeServiceSettings())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Service settings optimized successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to optimize service settings");
                    }
                }
                
                // Memory management
                var memoryCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Memory Management"));
                if (memoryCheckBox != null && memoryCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.OptimizeMemoryManagement())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Memory management optimized successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to optimize memory management");
                    }
                }
                
                // Update tracking
                _appliedOptimizationCategories["BasicTweaks"] = (successCount > 0);
                
                // Show results if requested
                if (showMessage)
                {
                    string title = (successCount == totalCount) ? "All Optimizations Applied" :
                                  (successCount == 0) ? "Optimization Failed" : "Partial Optimization Applied";
                    
                    MessageBoxImage icon = (successCount == totalCount) ? MessageBoxImage.Information :
                                         (successCount == 0) ? MessageBoxImage.Error : MessageBoxImage.Warning;
                    
                    MessageBox.Show(
                        $"Applied {successCount} out of {totalCount} basic optimizations.\n\n{resultMessage.ToString()}",
                        title,
                        MessageBoxButton.OK,
                        icon);
                }
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction("Apply Basic Tweaks", false, ex.Message);
                if (showMessage)
                {
                    MessageBox.Show(
                        $"An error occurred while applying optimizations:\n\n{ex.Message}", 
                        "Optimization Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
        }

        private void ApplyPowerTweaks(bool showMessage = true)
        {
            // Implementation for power tweaks
            bool success = true;
            bool anyTweaksApplied = false;

            // Find the checkboxes in the Power Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Disable Idle and Selective Suspend for USB Ports":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableUSBPowerManagement();
                            break;
                        case "Disable Idle and Enhanced Power Management for Storage Devices":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableStoragePowerManagement();
                            break;
                        case "Apply Highest Performance Power Plan":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.ApplyHighPerformancePowerPlan();
                            break;
                        case "Disable Hibernation":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableHibernation();
                            break;
                        case "Disable Timer Coalescing":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableTimerCoalescing();
                            break;
                        case "Disable Fast Startup":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableFastStartup();
                            break;
                        case "Disable Power Throttling":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.TogglePowerThrottling(false);
                            break;
                    }
                }
            }

            if (showMessage && anyTweaksApplied)
            {
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
            else if (showMessage && !anyTweaksApplied)
            {
                MessageBox.Show("No power tweaks were selected. Please select at least one option.", 
                                "No Action Taken", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyNetworkTweaks(bool showMessage = true)
        {
            // Implementation for network tweaks
            bool success = true;
            bool anyTweaksApplied = false;

            // Find the checkboxes in the Network Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Disable Hidden Network Power Saving Features":
                        case "Disable Network Power Saving Features":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableNetworkPowerSaving();
                            break;
                        case "Disable Unnecessary Network Interface Card Features":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableNetworkInterfaceFeatures();
                            break;
                    }
                }
            }

            if (showMessage && anyTweaksApplied)
            {
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
            else if (showMessage && !anyTweaksApplied)
            {
                MessageBox.Show("No network tweaks were selected. Please select at least one option.", 
                                "No Action Taken", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyAdvancedTweaks(bool showMessage = true)
        {
            try
            {
                int successCount = 0;
                int totalCount = 0;
                StringBuilder resultMessage = new StringBuilder();
                
                // Find all checkbox controls in the AdvancedTweaks tab
                var checkBoxes = FindVisualChildren<CheckBox>(this)
                    .Where(cb => cb.Parent != null && 
                           cb.Parent is Panel panel && 
                           panel.Parent != null && 
                           panel.Parent is GroupBox group && 
                           group.Header.ToString().Contains("Advanced System Tweaks"))
                    .ToList();
                
                if (checkBoxes.Count == 0)
                {
                    if (showMessage)
                    {
                        MessageBox.Show("No advanced optimization options were found or selected.", 
                                      "No Action Taken", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    return;
                }
                
                // Data Execution Prevention
                var depCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Data Execution Prevention"));
                if (depCheckBox != null && depCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.DisableDataExecutionPrevention())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Data Execution Prevention disabled successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to disable Data Execution Prevention");
                    }
                }
                
                // Meltdown/Spectre fixes
                var meltdownCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Meltdown/Spectre"));
                if (meltdownCheckBox != null && meltdownCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.DisableMeltdownSpectreFixes())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Meltdown/Spectre mitigations disabled successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to disable Meltdown/Spectre mitigations");
                    }
                }
                
                // BCDEdit Tweaks
                var bcdEditCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("BCDEdit Tweaks"));
                if (bcdEditCheckBox != null && bcdEditCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.ApplyBCDEditTweaks())
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ BCDEdit tweaks applied successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to apply BCDEdit tweaks");
                    }
                }
                
                // Process Priority
                var processPriorityCheckBox = checkBoxes.FirstOrDefault(cb => cb.Content.ToString().Contains("Process Priority"));
                if (processPriorityCheckBox != null && processPriorityCheckBox.IsChecked == true)
                {
                    totalCount++;
                    if (WindowsOptimizer.SetProcessPriority(38))
                    {
                        successCount++;
                        resultMessage.AppendLine("✓ Process priority tweaks applied successfully");
                    }
                    else
                    {
                        resultMessage.AppendLine("✗ Failed to apply process priority tweaks");
                    }
                }
                
                // Update tracking
                _appliedOptimizationCategories["AdvancedTweaks"] = (successCount > 0);
                
                // Show results if requested
                if (showMessage)
                {
                    string title = (successCount == totalCount) ? "All Advanced Tweaks Applied" :
                                  (successCount == 0) ? "Advanced Tweaks Failed" : "Partial Advanced Tweaks Applied";
                    
                    MessageBoxImage icon = (successCount == totalCount) ? MessageBoxImage.Information :
                                         (successCount == 0) ? MessageBoxImage.Error : MessageBoxImage.Warning;
                    
                    MessageBox.Show(
                        $"Applied {successCount} out of {totalCount} advanced tweaks.\n\n{resultMessage.ToString()}",
                        title,
                        MessageBoxButton.OK,
                        icon);
                }
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction("Apply Advanced Tweaks", false, ex.Message);
                if (showMessage)
                {
                    MessageBox.Show(
                        $"An error occurred while applying advanced tweaks:\n\n{ex.Message}",
                        "Optimization Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ApplyGpuTweaks(bool showMessage = true)
        {
            // Implementation for GPU tweaks
            bool success = true;
            bool anyTweaksApplied = false;

            // Find the checkboxes in the GPU Tweaks tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Enable Write Combining":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.ToggleWriteCombining(true);
                            break;
                        case "Disable Preemptions":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisablePreemptions();
                            break;
                        case "Disable Hidden Registry Power Saving Features":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.DisableGPUPowerSaving();
                            break;
                    }
                }
            }

            if (showMessage && anyTweaksApplied)
            {
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
            else if (showMessage && !anyTweaksApplied)
            {
                MessageBox.Show("No GPU tweaks were selected. Please select at least one option.", 
                                "No Action Taken", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void CleanupSystem(bool showMessage = true)
        {
            // Implementation for system cleanup
            bool success = true;
            bool anyTweaksApplied = false;

            // Find the checkboxes in the Cleanup tab
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    switch (child.Content.ToString())
                    {
                        case "Clean Temporary Files":
                        case "Clean System Cache":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.CleanTempFiles();
                            break;
                        case "Clean Windows Update Files":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.CleanWindowsUpdateFiles();
                            break;
                        case "Clean Recycle Bin":
                            anyTweaksApplied = true;
                            success &= WindowsOptimizer.CleanRecycleBin();
                            break;
                    }
                }
            }

            if (showMessage && anyTweaksApplied)
            {
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
            else if (showMessage && !anyTweaksApplied)
            {
                MessageBox.Show("No cleanup options were selected. Please select at least one option.", 
                                "No Action Taken", MessageBoxButton.OK, MessageBoxImage.Information);
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
            bool anyTweaksApplied = false;
            
            // Apply Basic Tweaks if any are selected
            bool basicTweaksSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Optimize Visual Effects for Performance" ||
                        content == "Disable Mouse Acceleration" ||
                        content == "Optimize Service Settings" ||
                        content == "Optimize Memory Management")
                    {
                        basicTweaksSelected = true;
                        break;
                    }
                }
            }
            
            if (basicTweaksSelected)
            {
                anyTweaksApplied = true;
                ApplyBasicTweaks(false); // Apply without showing individual messages
            }
            
            // Apply Power Tweaks if any are selected
            bool powerTweaksSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Disable Idle and Selective Suspend for USB Ports" ||
                        content == "Disable Idle and Enhanced Power Management for Storage Devices" ||
                        content == "Apply Highest Performance Power Plan" ||
                        content == "Disable Hibernation" ||
                        content == "Disable Timer Coalescing" ||
                        content == "Disable Fast Startup" ||
                        content == "Disable Power Throttling")
                    {
                        powerTweaksSelected = true;
                        break;
                    }
                }
            }
            
            if (powerTweaksSelected)
            {
                anyTweaksApplied = true;
                ApplyPowerTweaks(false); // Apply without showing individual messages
            }
            
            // Apply Network Tweaks if any are selected
            bool networkTweaksSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Disable Hidden Network Power Saving Features" ||
                        content == "Disable Network Power Saving Features" ||
                        content == "Disable Unnecessary Network Interface Card Features")
                    {
                        networkTweaksSelected = true;
                        break;
                    }
                }
            }
            
            if (networkTweaksSelected)
            {
                anyTweaksApplied = true;
                ApplyNetworkTweaks(false); // Apply without showing individual messages
            }
            
            // Apply Advanced Tweaks if any are selected
            bool advancedTweaksSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Disable Data Execution Prevention (DEP)" ||
                        content == "Disable Meltdown/Spectre Patches" ||
                        content == "Apply BCDEdit Tweaks")
                    {
                        advancedTweaksSelected = true;
                        break;
                    }
                }
            }
            
            if (advancedTweaksSelected)
            {
                // Confirmation for potentially dangerous tweaks
                MessageBoxResult result = MessageBox.Show(
                    "Some advanced tweaks can affect system stability and security. Are you sure you want to continue?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    anyTweaksApplied = true;
                    ApplyAdvancedTweaks(false); // Apply without showing individual messages
                }
            }
            
            // Apply GPU Tweaks if any are selected
            bool gpuTweaksSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Enable Write Combining" ||
                        content == "Disable Preemptions" ||
                        content == "Disable Hidden Registry Power Saving Features")
                    {
                        gpuTweaksSelected = true;
                        break;
                    }
                }
            }
            
            if (gpuTweaksSelected)
            {
                anyTweaksApplied = true;
                ApplyGpuTweaks(false); // Apply without showing individual messages
            }
            
            // Apply System Cleanup if any are selected
            bool cleanupSelected = false;
            foreach (var child in FindVisualChildren<CheckBox>(this))
            {
                if (child.IsChecked == true)
                {
                    string content = child.Content.ToString();
                    if (content == "Clean Temporary Files" ||
                        content == "Clean System Cache" ||
                        content == "Clean Windows Update Files" ||
                        content == "Clean Recycle Bin")
                    {
                        cleanupSelected = true;
                        break;
                    }
                }
            }
            
            if (cleanupSelected)
            {
                anyTweaksApplied = true;
                CleanupSystem(false); // Apply without showing individual messages
            }
            
            // Only show a message if any tweaks were actually applied
            if (anyTweaksApplied)
            {
                MessageBox.Show("All selected optimizations have been applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
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
            else
            {
                MessageBox.Show("No optimizations were selected. Please select at least one optimization to apply.", 
                               "No Action Taken", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ButtonRestoreSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var restoreOptions = new List<string>();
                
                // Build a list of applied optimization categories that can be reverted
                foreach (var category in _appliedOptimizationCategories)
                {
                    if (category.Value)
                    {
                        string displayName = category.Key switch
                        {
                            "BasicTweaks" => "Basic Windows Tweaks",
                            "PowerTweaks" => "Power Optimizations",
                            "NetworkTweaks" => "Network Optimizations",
                            "AdvancedTweaks" => "Advanced System Tweaks",
                            "GpuTweaks" => "GPU Optimizations",
                            "VersionTweaks" => "Version-Specific Optimizations",
                            _ => category.Key
                        };
                        
                        restoreOptions.Add(displayName);
                    }
                }
                
                if (restoreOptions.Count == 0)
                {
                    MessageBox.Show(
                        "No optimizations have been applied that can be restored.",
                        "Nothing to Restore",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
                
                // Create a dialog to let the user select categories to restore
                var dialog = new Window
                {
                    Title = "Restore Settings",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = (SolidColorBrush)FindResource(SystemColors.ControlBrushKey)
                };
                
                var mainPanel = new StackPanel { Margin = new Thickness(10) };
                mainPanel.Children.Add(new TextBlock 
                { 
                    Text = "Select optimization categories to restore:", 
                    Margin = new Thickness(0, 0, 0, 10),
                    FontWeight = FontWeights.Bold
                });
                
                var checkBoxList = new List<CheckBox>();
                foreach (var option in restoreOptions)
                {
                    var cb = new CheckBox { Content = option, Margin = new Thickness(0, 5, 0, 5), IsChecked = true };
                    mainPanel.Children.Add(cb);
                    checkBoxList.Add(cb);
                }
                
                var buttonPanel = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal, 
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 15, 0, 0)
                };
                
                var cancelButton = new Button { Content = "Cancel", Width = 80, Margin = new Thickness(5) };
                cancelButton.Click += (s, args) => { dialog.DialogResult = false; };
                
                var okButton = new Button { Content = "Restore", Width = 80, Margin = new Thickness(5) };
                okButton.Click += (s, args) => { dialog.DialogResult = true; };
                
                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(okButton);
                mainPanel.Children.Add(buttonPanel);
                
                dialog.Content = mainPanel;
                
                // Show dialog and process result
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    // Process selected categories to restore
                    List<string> selectedCategories = new List<string>();
                    for (int i = 0; i < checkBoxList.Count; i++)
                    {
                        if (checkBoxList[i].IsChecked == true)
                        {
                            string categoryKey = restoreOptions[i] switch
                            {
                                "Basic Windows Tweaks" => "BasicTweaks",
                                "Power Optimizations" => "PowerTweaks",
                                "Network Optimizations" => "NetworkTweaks",
                                "Advanced System Tweaks" => "AdvancedTweaks",
                                "GPU Optimizations" => "GpuTweaks",
                                "Version-Specific Optimizations" => "VersionTweaks",
                                _ => restoreOptions[i]
                            };
                            
                            selectedCategories.Add(categoryKey);
                        }
                    }
                    
                    // Restore each selected category
                    int restoredCount = 0;
                    foreach (var category in selectedCategories)
                    {
                        bool restored = RestoreCategory(category);
                        if (restored) restoredCount++;
                    }
                    
                    if (restoredCount > 0)
                    {
                        MessageBox.Show(
                            $"Successfully restored {restoredCount} out of {selectedCategories.Count} categories.",
                            "Restore Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed to restore any settings. See the log for details.",
                            "Restore Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction("Restore Settings", false, ex.Message);
                MessageBox.Show(
                    $"An error occurred while restoring settings:\n\n{ex.Message}",
                    "Restore Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private bool RestoreCategory(string category)
        {
            try
            {
                switch (category)
                {
                    case "BasicTweaks":
                        // Restore mouse settings
                        WindowsOptimizer.RestoreRegistryKey("MouseSettings");
                        
                        // Restore visual effects
                        WindowsOptimizer.RestoreRegistryKey("VisualEffects");
                        
                        // Restore memory management
                        WindowsOptimizer.RestoreRegistryKey("MemoryManagement");
                        
                        _appliedOptimizationCategories["BasicTweaks"] = false;
                        return true;
                        
                    case "PowerTweaks":
                        // Restore power settings
                        WindowsOptimizer.RestoreRegistryKey("PowerSettings");
                        
                        // Restore USB power management
                        WindowsOptimizer.RestoreRegistryKey("USBPower");
                        
                        // Restore disk power management
                        WindowsOptimizer.RestoreRegistryKey("DiskPower");
                        
                        _appliedOptimizationCategories["PowerTweaks"] = false;
                        return true;
                        
                    case "NetworkTweaks":
                        // Restore network settings
                        WindowsOptimizer.RestoreRegistryKey("NetworkSettings");
                        
                        _appliedOptimizationCategories["NetworkTweaks"] = false;
                        return true;
                        
                    case "AdvancedTweaks":
                        // Restore DEP settings
                        WindowsOptimizer.RestoreRegistryKey("DEPSettings");
                        
                        // Restore Meltdown/Spectre settings
                        WindowsOptimizer.RestoreRegistryKey("SpectreMeltdownSettings");
                        
                        _appliedOptimizationCategories["AdvancedTweaks"] = false;
                        return true;
                        
                    case "GpuTweaks":
                        // Restore GPU power settings
                        WindowsOptimizer.RestoreRegistryKey("GPUPowerSettings");
                        
                        _appliedOptimizationCategories["GpuTweaks"] = false;
                        return true;
                        
                    case "VersionTweaks":
                        // Restore Windows 11 settings
                        WindowsOptimizer.RestoreRegistryKey("Win11ContextMenu");
                        WindowsOptimizer.RestoreRegistryKey("Win11WidgetsSettings");
                        WindowsOptimizer.RestoreRegistryKey("Win11TransparencySettings");
                        WindowsOptimizer.RestoreRegistryKey("Win11SnapLayoutsSettings");
                        
                        // Restore Windows 10 settings
                        WindowsOptimizer.RestoreRegistryKey("Win10NewsInterestsSettings");
                        WindowsOptimizer.RestoreRegistryKey("Win10CortanaSettings");
                        WindowsOptimizer.RestoreRegistryKey("Win10TipsSettings");
                        
                        _appliedOptimizationCategories["VersionTweaks"] = false;
                        return true;
                        
                    default:
                        WindowsOptimizer.LogAction($"Restore category '{category}'", false, "Unknown category");
                        return false;
                }
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction($"Restore category '{category}'", false, ex.Message);
                return false;
            }
        }

        private void ButtonApplyVersionOptimizations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a system restore point first
                bool restorePointCreated = WindowsOptimizer.CreateRestorePoint("Hard Optimize - Before Version Tweaks");
                if (!restorePointCreated)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Failed to create a system restore point. It's recommended to create a restore point before applying version-specific optimizations.\n\n" +
                        "Do you want to continue without a restore point?",
                        "Restore Point Creation Failed",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                
                // Determine Windows version and apply appropriate optimizations
                var versionInfo = new WindowsVersionOptimizer.WindowsVersionInfo();
                bool success = false;
                
                if (versionInfo.IsWindows11)
                {
                    success = WindowsVersionOptimizer.ApplyWindows11Optimizations();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "Windows 11 optimizations applied successfully!\n\n" +
                            "Some changes may require a system restart to take full effect.",
                            "Optimization Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Some Windows 11 optimizations could not be applied.\n\n" +
                            "Please check the log for details.",
                            "Optimization Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else if (versionInfo.IsWindows10)
                {
                    success = WindowsVersionOptimizer.ApplyWindows10Optimizations();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "Windows 10 optimizations applied successfully!\n\n" +
                            "Some changes may require a system restart to take full effect.",
                            "Optimization Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Some Windows 10 optimizations could not be applied.\n\n" +
                            "Please check the log for details.",
                            "Optimization Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "No specific optimizations available for your Windows version.\n\n" +
                        "This feature is designed for Windows 10 and Windows 11 only.",
                        "Unsupported Windows Version",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                // Update tracking for potential reversal
                if (success)
                {
                    _appliedOptimizationCategories["VersionTweaks"] = true;
                }
            }
            catch (Exception ex)
            {
                WindowsOptimizer.LogAction("Apply version-specific optimizations", false, ex.Message);
                MessageBox.Show(
                    $"An error occurred while applying version optimizations:\n\n{ex.Message}",
                    "Optimization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}