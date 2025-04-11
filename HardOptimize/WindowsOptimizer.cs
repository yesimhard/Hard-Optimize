using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using Microsoft.Win32;
using System.Windows;

namespace WindowsOptimizerApp
{
    public class WindowsOptimizer
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HardOptimize",
            "optimization_log.txt");

        private static readonly string BackupFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HardOptimize",
            "registry_backups");

        // Dictionary to track applied optimizations for potential reversal
        private static Dictionary<string, Dictionary<string, object>> _appliedOptimizations = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Logs an optimization action to the log file
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="successful">Whether the action was successful</param>
        /// <param name="details">Optional details about the action</param>
        public static void LogAction(string action, bool successful, string details = null)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Format log entry
                string status = successful ? "SUCCESS" : "FAILED";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [{status}] {action}";
                
                if (!string.IsNullOrEmpty(details))
                {
                    logEntry += $" - {details}";
                }

                // Append to log file
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Silently fail if logging encounters an error
                // This prevents logging issues from affecting core functionality
            }
        }

        /// <summary>
        /// Retrieves the log contents
        /// </summary>
        /// <returns>The contents of the log file</returns>
        public static string GetLogContents()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    return File.ReadAllText(LogFilePath);
                }
            }
            catch
            {
                // Silently fail if reading the log encounters an error
            }

            return "No optimization log found.";
        }

        /// <summary>
        /// Clears the log file
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ClearLog()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.WriteAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log cleared{Environment.NewLine}");
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of a registry key before modifying it
        /// </summary>
        /// <param name="keyPath">The registry key path to backup</param>
        /// <param name="keyName">Name for the backup file</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool BackupRegistryKey(string keyPath, string keyName)
        {
            try
            {
                // Ensure backup directory exists
                if (!Directory.Exists(BackupFolderPath))
                {
                    Directory.CreateDirectory(BackupFolderPath);
                }
                
                // Create timestamp for the backup filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFilePath = Path.Combine(BackupFolderPath, $"{keyName}_{timestamp}.reg");
                
                // Use reg.exe to export the registry key
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"export \"{keyPath}\" \"{backupFilePath}\" /y",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        LogAction($"Registry backup created for {keyPath}", true, backupFilePath);
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        LogAction($"Registry backup failed for {keyPath}", false, error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogAction($"Registry backup failed for {keyPath}", false, ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Restores a registry key from the most recent backup
        /// </summary>
        /// <param name="keyName">Name of the backup to restore</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool RestoreRegistryKey(string keyName)
        {
            try
            {
                if (!Directory.Exists(BackupFolderPath))
                {
                    LogAction($"Restore failed for {keyName}", false, "No backup directory found");
                    return false;
                }
                
                // Find the most recent backup file for this key
                var backupFiles = Directory.GetFiles(BackupFolderPath, $"{keyName}_*.reg")
                                         .OrderByDescending(f => f)
                                         .ToList();
                
                if (backupFiles.Count == 0)
                {
                    LogAction($"Restore failed for {keyName}", false, "No backup file found");
                    return false;
                }
                
                string mostRecentBackup = backupFiles[0];
                
                // Use reg.exe to import the registry backup
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"import \"{mostRecentBackup}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        LogAction($"Registry key restored from {mostRecentBackup}", true);
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        LogAction($"Registry restore failed for {keyName}", false, error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogAction($"Registry restore failed for {keyName}", false, ex.Message);
                return false;
            }
        }

        #region Basic Tweaks

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        // Mouse acceleration constants
        private const uint SPI_SETMOUSE = 0x0004;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        public static bool DisableMouseAcceleration()
        {
            try
            {
                // Check if we're running on Windows 11
                bool isWindows11 = Environment.OSVersion.Version.Build >= 22000;
                string details = isWindows11 ? "Windows 11 detected" : "Windows 10 or earlier";
                
                // Back up registry key before modifying
                BackupRegistryKey(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSettings");
                
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true);
                if (key != null)
                {
                    key.SetValue("MouseSpeed", "0", RegistryValueKind.String);
                    key.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
                    key.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
                    
                    // For Windows 11, we might need an additional setting for newer pointer precision controls
                    if (isWindows11)
                    {
                        key.SetValue("EnablePointerPrecision", 0, RegistryValueKind.DWord);
                    }
                    
                    // For some systems, we need to modify mouse acceleration settings in two places
                    // This ensures "Enhance pointer precision" is definitely turned off
                    using (RegistryKey mouseKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                    {
                        mouseKey.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
                    }
                    
                    // Use the SystemParametersInfo Windows API to apply changes immediately
                    int[] mouseParams = new int[3] { 0, 0, 0 }; // No acceleration
                    IntPtr mouseParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(mouseParams[0]) * mouseParams.Length);
                    
                    try
                    {
                        Marshal.Copy(mouseParams, 0, mouseParamsPtr, mouseParams.Length);
                        SystemParametersInfo(SPI_SETMOUSE, 0, mouseParamsPtr, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(mouseParamsPtr);
                    }
                    
                    key.Close();
                    LogAction("Disable mouse acceleration", true, details);
                    return true;
                }
                
                LogAction("Disable mouse acceleration", false, "Registry key not found");
                return false;
            }
            catch (Exception ex)
            {
                LogAction("Disable mouse acceleration", false, ex.Message);
                return false;
            }
        }

        public static bool OptimizeVisualEffects()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                }
                if (key != null)
                {
                    key.SetValue("VisualFXSetting", 2); // 2 = Best Performance
                    key.Close();
                }

                // Additional visual effect settings
                key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                if (key != null)
                {
                    key.SetValue("UserPreferencesMask", new byte[] { 0x90, 0x12, 0x01, 0x80 });
                    key.Close();
                }

                key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
                if (key != null)
                {
                    key.SetValue("ListviewAlphaSelect", 0);
                    key.SetValue("TaskbarAnimations", 0);
                    key.Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool OptimizeServiceSettings()
        {
            try
            {
                // Example: Disable Windows Search service for better performance
                ServiceController sc = new ServiceController("WSearch");
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Start", 4); // 4 = Disabled
                    }
                }

                // You can add more service optimizations here
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool OptimizeMemoryManagement()
        {
            try
            {
                // Memory management settings in registry
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    if (key != null)
                    {
                        key.SetValue("ClearPageFileAtShutdown", 0);
                        key.SetValue("LargeSystemCache", 0);
                        key.SetValue("NonPagedPoolSize", 0);
                        key.SetValue("SystemPages", 0);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Power Tweaks

        public static bool DisableUSBPowerManagement()
        {
            try
            {
                // Disable USB selective suspend
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USB", true))
                {
                    if (key != null)
                    {
                        key.SetValue("DisableSelectiveSuspend", 1);
                    }
                }

                // Disable USB power savings for all devices
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBController");
                ManagementObjectCollection collection = searcher.Get();

                foreach (ManagementObject device in collection)
                {
                    try
                    {
                        ManagementBaseObject newSettings = device.GetMethodParameters("SetPowerState");
                        newSettings["PowerState"] = 1; // Full Power
                        device.InvokeMethod("SetPowerState", newSettings, null);
                    }
                    catch
                    {
                        // Continue with next device if current one fails
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableStoragePowerManagement()
        {
            try
            {
                // Disable idle for storage devices
                Process process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "/setacvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                // Apply the changes
                process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "-S SCHEME_CURRENT";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ApplyHighPerformancePowerPlan()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"; // High Performance GUID
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableHibernation()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "-h off";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableFastStartup()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", true))
                {
                    if (key != null)
                    {
                        key.SetValue("HiberbootEnabled", 0);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableTimerCoalescing()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", true))
                {
                    if (key != null)
                    {
                        key.SetValue("CoalescingTimerInterval", 0);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TogglePowerThrottling(bool enable)
        {
            try
            {
                // Create the registry key if it doesn't exist
                RegistryKey throttlingKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", true);
                if (throttlingKey == null)
                {
                    throttlingKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling");
                }
                
                if (throttlingKey != null)
                {
                    throttlingKey.SetValue("PowerThrottlingOff", enable ? 0 : 1, RegistryValueKind.DWord);
                    throttlingKey.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Network Tweaks

        public static bool DisableNetworkPowerSaving()
        {
            try
            {
                // Disable network power saving features
                Process process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "/setacvalueindex SCHEME_CURRENT SUB_NETWORK ASLEEP 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                // Apply the changes
                process = new Process();
                process.StartInfo.FileName = "powercfg.exe";
                process.StartInfo.Arguments = "-S SCHEME_CURRENT";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableNetworkInterfaceFeatures()
        {
            try
            {
                // This would need to be customized per network adapter
                // Using PowerShell to disable offloading features on all network adapters
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "Get-NetAdapter | ForEach-Object { Disable-NetAdapterChecksumOffload -Name $_.Name -ErrorAction SilentlyContinue; Disable-NetAdapterLso -Name $_.Name -ErrorAction SilentlyContinue; Disable-NetAdapterRsc -Name $_.Name -ErrorAction SilentlyContinue }";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetDscpPriorityForGame(string executablePath)
        {
            try
            {
                if (!File.Exists(executablePath))
                {
                    return false;
                }

                string executableName = Path.GetFileName(executablePath);

                // Using PowerShell to set QoS policy
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"New-NetQosPolicy -Name \"Game Priority - {executableName}\" -AppPathNameMatchCondition \"{executableName}\" -DSCPAction 46 -IPProtocolMatchCondition Both";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Advanced Tweaks

        public static bool DisableDataExecutionPrevention()
        {
            try
            {
                // SECURITY WARNING: This is potentially dangerous
                MessageBoxResult result = MessageBox.Show(
                    "Warning: Disabling Data Execution Prevention can significantly reduce your system's security against malware. " +
                    "Only proceed if you fully understand the risks.\n\nDo you want to continue?",
                    "Security Risk Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes)
                {
                    LogAction("Disable Data Execution Prevention", false, "User cancelled due to security warning");
                    return false;
                }
                
                // Back up registry key before modifying
                BackupRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DEPSettings");
                
                // Use BCDEdit to disable DEP
                Process process = new Process();
                process.StartInfo.FileName = "bcdedit.exe";
                process.StartInfo.Arguments = "/set nx AlwaysOff";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableMeltdownSpectreFixes()
        {
            try
            {
                // SECURITY WARNING: This is potentially dangerous
                MessageBoxResult result = MessageBox.Show(
                    "Warning: Disabling Meltdown and Spectre fixes leaves your CPU vulnerable to these serious security exploits. " +
                    "This should only be done on systems that are not connected to the internet or processing sensitive data.\n\n" +
                    "Do you want to continue despite these significant security risks?",
                    "Serious Security Risk Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes)
                {
                    LogAction("Disable Meltdown/Spectre Fixes", false, "User cancelled due to security warning");
                    return false;
                }
                
                // Back up registry keys before modifying
                BackupRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "SpectreMeltdownSettings");
                
                // Registry keys to disable mitigations
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    if (key != null)
                    {
                        key.SetValue("FeatureSettingsOverride", 3);
                        key.SetValue("FeatureSettingsOverrideMask", 3);
                    }
                }
                
                // BCDEdit settings
                Process process = new Process();
                process.StartInfo.FileName = "bcdedit.exe";
                process.StartInfo.Arguments = "/set disabledynamictick yes";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ApplyBCDEditTweaks()
        {
            try
            {
                // Array of BCDEdit commands to run
                string[] commands = new string[]
                {
                    "/set useplatformclock no",
                    "/set disabledynamictick yes",
                    "/set tscsyncpolicy Enhanced",
                    "/timeout 0"
                };

                foreach (string command in commands)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "bcdedit.exe";
                    process.StartInfo.Arguments = command;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetProcessPriority(int win32PrioritySeparation)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Win32PrioritySeparation", win32PrioritySeparation, RegistryValueKind.DWord);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region GPU Tweaks

        public static bool InstallNVCleanInstall()
        {
            try
            {
                // Download and install NVCleanInstall
                // This would typically download and run an installer
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.techpowerup.com/download/techpowerup-nvcleanstall/",
                    UseShellExecute = true
                });
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ToggleWriteCombining(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true))
                {
                    if (key != null)
                    {
                        key.SetValue("EnableWriteCombining", enable ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisablePreemptions()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true))
                {
                    if (key != null)
                    {
                        key.SetValue("TdrLevel", 0, RegistryValueKind.DWord);
                        key.SetValue("TdrDelay", 60, RegistryValueKind.DWord);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableGPUPowerSaving()
        {
            try
            {
                // First determine GPU vendor
                string gpuVendor = GetGPUVendor();
                LogAction("GPU Detection", true, $"Detected GPU vendor: {gpuVendor}");
                
                if (gpuVendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    return DisableNvidiaGPUPowerSaving();
                }
                else if (gpuVendor.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                {
                    return DisableAmdGPUPowerSaving();
                }
                else
                {
                    LogAction("Disable GPU Power Saving", false, $"Unsupported GPU vendor: {gpuVendor}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogAction("Disable GPU Power Saving", false, ex.Message);
                return false;
            }
        }
        
        private static string GetGPUVendor()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                            {
                                return "NVIDIA";
                            }
                            else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || 
                                     name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                            {
                                return "AMD";
                            }
                            else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                            {
                                return "Intel";
                            }
                        }
                    }
                }
                
                return "Unknown";
            }
            catch (Exception ex)
            {
                LogAction("GPU Vendor Detection", false, ex.Message);
                return "Unknown";
            }
        }
        
        private static bool DisableNvidiaGPUPowerSaving()
        {
            try
            {
                BackupRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}", "NvidiaGPUSettings");
                
                // First, check for Nvidia Registry keys
                string[] possibleNvidiaKeys = { "0000", "0001", "0002", "0003", "0004", "0005" };
                bool success = false;
                
                foreach (string key in possibleNvidiaKeys)
                {
                    string registryPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\" + key;
                    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(registryPath, true))
                    {
                        if (baseKey != null)
                        {
                            string providerName = baseKey.GetValue("ProviderName")?.ToString() ?? "";
                            if (providerName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                            {
                                // This is an NVIDIA GPU, apply the settings
                                baseKey.SetValue("PerfLevelSrc", 0x00002222, RegistryValueKind.DWord);
                                baseKey.SetValue("PowerMizerEnable", 0, RegistryValueKind.DWord);
                                baseKey.SetValue("PowerMizerLevel", 1, RegistryValueKind.DWord);
                                baseKey.SetValue("PowerMizerLevelAC", 1, RegistryValueKind.DWord);
                                
                                success = true;
                                LogAction("Disable NVIDIA GPU Power Saving", true, $"Applied to key {registryPath}");
                            }
                        }
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                LogAction("Disable NVIDIA GPU Power Saving", false, ex.Message);
                return false;
            }
        }
        
        private static bool DisableAmdGPUPowerSaving()
        {
            try
            {
                BackupRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}", "AmdGPUSettings");
                
                // Look for AMD Registry keys
                string[] possibleAmdKeys = { "0000", "0001", "0002", "0003", "0004", "0005" };
                bool success = false;
                
                foreach (string key in possibleAmdKeys)
                {
                    string registryPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\" + key;
                    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(registryPath, true))
                    {
                        if (baseKey != null)
                        {
                            string providerName = baseKey.GetValue("ProviderName")?.ToString() ?? "";
                            if (providerName.Contains("AMD", StringComparison.OrdinalIgnoreCase) || 
                                providerName.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
                            {
                                // This is an AMD GPU, apply the settings
                                
                                // Disable PowerPlay and power management features
                                baseKey.SetValue("EnableUlps", 0, RegistryValueKind.DWord);
                                baseKey.SetValue("PP_SclkDeepSleepDisable", 1, RegistryValueKind.DWord);
                                baseKey.SetValue("PP_ThermalAutoThrottlingEnable", 0, RegistryValueKind.DWord);
                                baseKey.SetValue("KMD_EnableComputePreemption", 0, RegistryValueKind.DWord);
                                
                                // Set highest performance profile
                                baseKey.SetValue("KMD_DynamicPowerManagement", 0, RegistryValueKind.DWord);
                                baseKey.SetValue("DisableDrmdmaPowerGating", 1, RegistryValueKind.DWord);
                                
                                success = true;
                                LogAction("Disable AMD GPU Power Saving", true, $"Applied to key {registryPath}");
                            }
                        }
                    }
                }
                
                // Also check for additional AMD-specific settings
                using (RegistryKey advancedKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000\UMD", true))
                {
                    if (advancedKey != null)
                    {
                        // Disable ULPS (Ultra Low Power State)
                        advancedKey.SetValue("ULPS", 0, RegistryValueKind.DWord);
                        
                        // Set main DXVA power level to maximum
                        advancedKey.SetValue("Main_PX", 0, RegistryValueKind.DWord);
                        
                        success = true;
                        LogAction("Disable AMD GPU Advanced Power Settings", true);
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                LogAction("Disable AMD GPU Power Saving", false, ex.Message);
                return false;
            }
        }

        #endregion

        #region Debloat Windows

        public static bool UninstallWindowsApps(List<string> appNames)
        {
            try
            {
                foreach (string appName in appNames)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"Get-AppxPackage *{appName}* | Remove-AppxPackage";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DisableServices(List<string> serviceNames)
        {
            try
            {
                foreach (string serviceName in serviceNames)
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true))
                        {
                            if (key != null)
                            {
                                key.SetValue("Start", 4); // 4 = Disabled
                            }
                        }

                        // Also try to stop the service if it's running
                        ServiceController sc = new ServiceController(serviceName);
                        if (sc.Status == ServiceControllerStatus.Running)
                        {
                            sc.Stop();
                        }
                    }
                    catch
                    {
                        // Continue with next service if current one fails
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Cleanup

        public static bool CleanTempFiles()
        {
            try
            {
                // Temp directories to clean
                string[] tempDirs = new string[]
                {
                    Path.GetTempPath(),
                    Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp"
                };

                foreach (string dir in tempDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        foreach (FileInfo file in di.GetFiles())
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch
                            {
                                // Skip files that are in use
                            }
                        }
                        foreach (DirectoryInfo subDir in di.GetDirectories())
                        {
                            try
                            {
                                subDir.Delete(true);
                            }
                            catch
                            {
                                // Skip directories that are in use
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CleanWindowsUpdateFiles()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cleanmgr.exe";
                process.StartInfo.Arguments = "/sagerun:1";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                // Note: This runs in the background, so we don't wait for it to exit

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CleanRecycleBin()
        {
            try
            {
                // Use PowerShell to properly empty the Recycle Bin - this is more reliable than cmd
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                // Run Clear-RecycleBin with -Force to suppress confirmation dialog
                process.StartInfo.Arguments = "-Command \"Clear-RecycleBin -Force\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit();
                
                // Also try the traditional way to make sure we get everything
                // Check all drives and attempt to clean their recycle bins
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        string recycleBinPath = $"{drive.Name}$Recycle.Bin";
                        if (Directory.Exists(recycleBinPath))
                        {
                            try
                            {
                                Process cmdProcess = new Process();
                                cmdProcess.StartInfo.FileName = "cmd.exe";
                                cmdProcess.StartInfo.Arguments = $"/c rd /s /q \"{recycleBinPath}\"";
                                cmdProcess.StartInfo.UseShellExecute = false;
                                cmdProcess.StartInfo.CreateNoWindow = true;
                                cmdProcess.Start();
                                cmdProcess.WaitForExit(5000); // Wait up to 5 seconds for each drive
                            }
                            catch
                            {
                                // Continue with next drive if current one fails
                            }
                        }
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool OptimizeDrives()
        {
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        OptimizeDrive(drive.Name[0]);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool OptimizeDrive(char driveLetter)
        {
            try
            {
                // Run disk defragmenter for the specified drive
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "defrag.exe";
                    process.StartInfo.Arguments = $"{driveLetter}: /O";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Creates a system restore point before applying optimizations
        /// </summary>
        /// <param name="description">Description of the restore point</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool CreateRestorePoint(string description = "Hard Optimize - Before Optimization")
        {
            try
            {
                // First ensure System Restore is enabled on the system drive
                ProcessStartInfo enableSrPsi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "Enable-ComputerRestore -Drive \"$env:SystemDrive\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (Process enableSrProcess = Process.Start(enableSrPsi))
                {
                    enableSrProcess.WaitForExit();
                }
                
                // Create the restore point
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"Checkpoint-Computer -Description \"{description}\" -RestorePointType \"APPLICATION_INSTALL\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        LogAction("Create System Restore Point", true, description);
                        return true;
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        LogAction("Create System Restore Point", false, error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogAction("Create System Restore Point", false, ex.Message);
                return false;
            }
        }
    }
} 