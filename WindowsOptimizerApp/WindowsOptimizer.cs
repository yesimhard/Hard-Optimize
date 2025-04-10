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

namespace WindowsOptimizerApp
{
    public class WindowsOptimizer
    {
        #region Basic Tweaks

        public static bool DisableMouseAcceleration()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true);
                if (key != null)
                {
                    key.SetValue("MouseSpeed", "0");
                    key.SetValue("MouseThreshold1", "0");
                    key.SetValue("MouseThreshold2", "0");
                    key.Close();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
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
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true))
                {
                    if (key != null)
                    {
                        key.SetValue("PerfLevelSrc", 2222, RegistryValueKind.DWord);
                        key.SetValue("PowerMizerEnable", 0, RegistryValueKind.DWord);
                        key.SetValue("PowerMizerLevel", 1, RegistryValueKind.DWord);
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

        public static bool OptimizeDrives()
        {
            try
            {
                // Get all fixed drives
                DriveInfo[] drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).ToArray();
                
                foreach (DriveInfo drive in drives)
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "defrag.exe";
                    process.StartInfo.Arguments = $"{drive.Name[0]}: /O";
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

        #endregion
    }
} 