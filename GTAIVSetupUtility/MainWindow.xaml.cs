using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using ByteSizeLib;
using GTAIVSetupUtilityWPF.Common;
using GTAIVSetupUtilityWPF.Functions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVSetupUtilityWPF.GTAIVSetupUtility
{
    public partial class MainWindow
    {
        private readonly (int, int, int, bool, bool, bool, bool) _resultVk;
        private readonly int _installDxvk;
        private int _vram1;
        private int _vram2;
        private bool _ffix;
        private bool _ffixLatest;
        private bool _isRetail;
        private bool _isIvsdkInstalled;
        private bool _dxvkOnIgpu;
        private bool _firstGpu = true;
        private string _rtssConfig = File.Exists(@"C:\Program Files (x86)\RivaTuner Statistics Server\Profiles\GTAIV.exe.cfg") ? @"C:\Program Files (x86)\RivaTuner Statistics Server\Profiles\GTAIV.exe.cfg" : File.Exists(@"C:\Program Files (x86)\RivaTuner Statistics Server\Profiles\Global") ? @"C:\Program Files (x86)\RivaTuner Statistics Server\Profiles\Global" : string.Empty;
        private string? _iniPath;
        private string? _iniPathZp;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
        public MainWindow()
        {
            if (File.Exists("GTAIVSetupUtilityLog.txt")) { File.Delete("GTAIVSetupUtilityLog.txt"); }
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "GTAIVSetupUtilityLog.txt");
            });
            Logger.Info(" Initializing the main window...");
            InitializeComponent();
            Logger.Info(" Main window initialized!");
            
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            
            if (isLinux)
            {
                Logger.Info(" Detected Linux operating system - DXVK functionality will be disabled.");
                _installDxvk = 0;
                
                DxvkPanel.IsEnabled = false;

                MessageBox.Show(
                    "DXVK installation and Vulkan checking is only available on Windows.\n\n" +
                    "These features have been disabled. Launch options configuration remains available.",
                    "Linux Detected - DXVK Unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
        
                Logger.Info(" DXVK functionality disabled for Linux.");
                return;
            }

            Logger.Info(" Initializing the vulkan check...");
            _resultVk = VulkanChecker.VulkanCheck();
            Logger.Info(" Vulkan check finished!");

            var vkDgpuDxvkSupport = _resultVk.Item1;
            var vkIgpuDxvkSupport = _resultVk.Item2;
            var gplSupport = _resultVk.Item3;
            var igpuOnly = _resultVk.Item4;
            var dgpuOnly = _resultVk.Item5;
            var intelIgpu = _resultVk.Item6;
            var allowAsync = _resultVk.Item7;

            switch (igpuOnly)
            {
                case true when !dgpuOnly:
                    Logger.Debug($" User's PC only has an iGPU. Setting Install DXVK to {vkIgpuDxvkSupport}.");
                    _installDxvk = vkIgpuDxvkSupport;
                    break;
                case false when dgpuOnly:
                    Logger.Debug($" User's PC only has a dGPU. Setting Install DXVK to {vkDgpuDxvkSupport}.");
                    _installDxvk = vkDgpuDxvkSupport;
                    break;
                case false when !dgpuOnly:
                    Logger.Debug(" User's PC has both an iGPU and a dGPU. Doing further checks...");
                    switch ((vkDgpuDxvkSupport, vkIgpuDxvkSupport))
                    {
                        case (0, 1):
                        case (0, 2):
                        case (0, 3):
                            Logger.Debug(" User PC's iGPU supports DXVK, but their dGPU does not - asking them what to do...");
                            var result = MessageBox.Show("Your iGPU supports DXVK but your GPU doesn't - do you still wish to install?", "Install DXVK?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                Logger.Debug(" User chose to install DXVK for the iGPU");
                                _dxvkOnIgpu = true;
                                Logger.Debug($" Setting Install DXVK to {vkIgpuDxvkSupport}");
                                _installDxvk = vkIgpuDxvkSupport;
                            }
                            else
                            {
                                Logger.Debug(" User chose not to install DXVK.");
                            }
                            break;
                        case (1, 2):
                        case (1, 3):
                        case (2, 3):
                            Logger.Debug(" User PC's iGPU supports DXVK, but their dGPU supports an inferior version - asking them what to do...");
                            var resultVer = MessageBox.Show("Your iGPU supports a greater version of DXVK than your GPU - which version do you wish to install?\n\nPress 'Yes' to install the version matching your GPU.\n\nPress 'No' to install the version matching your iGPU instead.", "Which DXVK version to install?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (resultVer == MessageBoxResult.Yes)
                            {
                                Logger.Debug($" User chose to install DXVK for the dGPU. Setting Install DXVK to {vkDgpuDxvkSupport}.");
                                _installDxvk = vkDgpuDxvkSupport;
                            }
                            else
                            {
                                Logger.Debug($" User chose to install DXVK for the iGPU. Setting Install DXVK to {vkIgpuDxvkSupport}.");
                                _dxvkOnIgpu = true;
                                _installDxvk = vkIgpuDxvkSupport;
                            }
                            break;
                        case (3, 3):
                        case (2, 2):
                        case (1, 1):
                        case (2, 1):
                        case (3, 1):
                        case (3, 2):
                        case (3, 0):
                        case (2, 0):
                        case (1, 0):
                            Logger.Debug($" User's GPU supports the same or a better version of DXVK as the iGPU. Setting Install DXVK to {vkDgpuDxvkSupport}");
                            _installDxvk = vkDgpuDxvkSupport;
                            break;
                    }

                    break;
            }

            if (intelIgpu && igpuOnly)
            {
                Logger.Debug(" User's PC only has an Intel iGPU. Prompting them to install DXVK 1.10.1.");
                var result = MessageBox.Show("Your PC only has an Intel iGPU on it. While it does support more modern versions on paper, it's reported that DXVK 1.10.1 might be your only supported version. Do you wish to install it?\n\nIf 'No' is selected, DXVK will be installed following the normal procedure.", "Message", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    Logger.Debug(" Setting Install DXVK to -1 - a special case to install 1.10.1 for Intel iGPU's.");
                    _installDxvk = -1;
                }
            }
            if (gplSupport != 2 || _installDxvk < 2) { AsyncCheckbox.Visibility = Visibility.Visible; AsyncCheckbox.IsEnabled = true; AsyncCheckbox.IsChecked = true; Logger.Debug(" User's GPU doesn't support GPL in full, enable async toggle."); }
            else if (allowAsync) { AsyncCheckbox.Visibility = Visibility.Visible; AsyncCheckbox.IsEnabled = true; Logger.Debug(" One of user's GPU doesn't support GPL in full, allow enabling async for an edge case scenario."); }
        }


        private static string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        private void ResetVars()
        {
            InstallDxvkBtn.IsDefault = true;
            LaunchOptionsBtn.IsDefault = false;
            LaunchOptionsBtn.FontWeight = FontWeights.Normal;
            InstallDxvkBtn.Width = 160;
            InstallDxvkBtn.FontWeight = FontWeights.SemiBold;
            InstallDxvkBtn.FontSize = 12;
            InstallDxvkBtn.Content = "Install DXVK";
            UninstallDxvkBtn.Visibility = Visibility.Collapsed;
            VidMemCheckbox.IsEnabled = true;
            VidMemCheckbox.IsChecked = true;
            Gb3Checkbox.IsEnabled = true;
            Gb4Checkbox.IsEnabled = true;
        }

        private void DeleteFiles(string directory, List<string> filename)
        {
            foreach(var file in filename)
            {
                if (File.Exists($"{directory}/{file}"))
                {
                    File.Delete($"{directory}/{file}");
                }
            }
        }

        private void client_DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Debug(" Successfully downloaded.");
                _downloadFinished = true;
                InstallDxvkBtn.Content = $"Installing...";
            });
        }
        private void async_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled async.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("DXVK-async is the next best alternative to stutter-less experience - since your GPU doesn't support GPL with Fast Linking, you can reduce stutter with async shader building, which will be a little faster than traditional shader building.");
        }
        private void vsync_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled VSync.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("The in-game VSync implementation produces framepacing issues. DXVK's VSync implementation should be preferred.\n\nIt's recommended to keep this on and in-game's implementation off.");
        }
        private void Latency_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Max Frame Latency.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option may help avoiding further framepacing issues. It's recommended to keep this on.");
        }
        private void NoRestrictions_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -norestrictions.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option allows you to set any in-game settings independently of what the game restricts you to. It's recommended to keep this on. ");
        }

        private void NoMemRestrict_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -nomemrestrict.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("-nomemrestrict allows the game to use all the dedicated memory resources up to the limits. It's recommended to keep this on.");
        }
        private void Windowed_Click(object sender, RoutedEventArgs e)
        {
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option allows to use Borderless Fullscreen instead of Exclusive Fullscreen. Provides better experience and sometimes better performance. It's recommended to keep this on.");
        }
        private void VidMem_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -availablevidmem.");
            if (VidMemCheckbox.IsChecked == false)
            {
                Gb3Checkbox.IsEnabled = false;
                Gb4Checkbox.IsEnabled = false;
            }
            else
            {
                Gb3Checkbox.IsEnabled = true;
                Gb4Checkbox.IsEnabled = true;
            }

            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option forces a specific value of video memory due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
        }

        private void VidMemLock_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User changed the vidmem lock");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option allows to change between a 3GB and a 4GB VRAM lock when setting -availablevidmem up.\n\nThe game is 32-bit, so going beyond 4GB is entirely pointless. However, some people reported less issues with it being locked to 3GB instead.\n\nIt's recommended to keep this at default. Option is disabled if latest FusionFix is detected.");
        }
        private void MonitorDetail_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Monitor Details.");
            if (TipsCheckbox.IsChecked != true) return;
            Logger.Debug(" Displaying a tip...");
            MessageBox.Show("This option forces a specific resolution and refresh rate due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User opened the About window.");
            MessageBox.Show(
                "This software is made by Gillian for the Modding Guide. Below is debug text, you don't need it normally.\n\n" +
                $"DXVK to install: {_installDxvk}\n" +
                $"dGPU DXVK Support: {_resultVk.Item1}\n" +
                $"iGPU DXVK Support: {_resultVk.Item2}\n" +
                $"GPL support state: {_resultVk.Item3}\n" +
                $"iGPU Only: {_resultVk.Item4}\n" +
                $"dGPU Only: {_resultVk.Item5}\n" +
                $"Intel iGPU: {_resultVk.Item6}\n\n" +
                $"Version: {GetAssemblyVersion()}",
                "Information");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User is selecting the game folder...");
            while (true)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = @"C:\Program Files (x86)\Steam\steamapps\Grand Theft Auto IV\GTAIV";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Logger.Debug(" User selected a folder, proceeding...");
                    var gameVersion = AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe");
                    var isDxvkInstalled = false;
                    var fusionFixIniPath = Directory.GetFiles(dialog.FileName, "GTAIV.EFLC.FusionFix.ini", SearchOption.AllDirectories).FirstOrDefault();
                    var fusionFixCfgPath = Directory.GetFiles(dialog.FileName, "GTAIV.EFLC.FusionFix.cfg", SearchOption.AllDirectories).FirstOrDefault();
                    var zolikaPatchIniPath = Directory.GetFiles(dialog.FileName, "ZolikaPatch.ini", SearchOption.AllDirectories).FirstOrDefault();
                    if (gameVersion.StartsWith("1, 0") || (gameVersion.StartsWith("1.2")))
                    {
                        if (File.Exists($@"{dialog.FileName}\commandline.txt")) {
                            Logger.Debug(" Filtering commandline.txt from garbage because I know *somebody* will have a garbage commandline.");
                            var lines = File.ReadAllLines($@"{dialog.FileName}\commandline.txt");
                            var filteredLines = lines.Where(line =>
                                !line.Contains("-no_3GB") &&
                                !line.Contains("-noprecache") &&
                                !line.Contains("-notimefix") &&
                                !line.Contains("-novblank") &&
                                !line.Contains("-percentvidmem") &&
                                !line.Contains("-memrestrict") &&
                                !line.Contains("-reserve") &&
                                !line.Contains("-reservedApp") &&
                                !line.Contains("-disableimposters") &&
                                !line.Contains("-force2vb") &&
                                !line.Contains("-minspecaudio"));
                            File.WriteAllLines($@"{dialog.FileName}\commandline.txt", filteredLines);
                        }
                        if (gameVersion.StartsWith("1, 0")) { _isRetail = true; Logger.Debug(" Folder contains a retail exe."); }
                        else {
                            Logger.Debug(" Folder contains an exe of Steam Version.");
                            _isRetail = false;
                            if (File.Exists($@"{dialog.FileName}\commandline.txt")) {
                                Logger.Info("commandline.txt detected on Steam Version...");
                                MessageBox.Show("You appear to have a commandline.txt, however your are using the Steam version which doesn't use that file. Consider moving these options to Steam Launch Options or launch arguments.");
                            }
                        }
                        if (_isRetail && !gameVersion.StartsWith("1, 0, 8"))
                        { VidMemCheckbox.IsEnabled = false; Gb3Checkbox.IsEnabled = false; Gb4Checkbox.IsEnabled = false; Logger.Debug(" Folder contains an exe of some pre-1.0.8.0 version. Disabling the -availablevidmem toggle."); }
                        if (File.Exists($@"{dialog.FileName}\dsound.dll"))
                        {
                            Logger.Info("dsound.dll detected, warning the user...");
                            var result = MessageBox.Show("You appear to have an outdated ASI Loader (dsound.dll). Consider removing it.\n\nPress 'Yes' to get redirected to download to the latest version - download the non-x64 one, rename dinput8.dll to xlive.dll if you don't plan to play GFWL and using non-Steam version.", "Outdated ASI loader.", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                var psi = new ProcessStartInfo
                                {
                                    FileName = "cmd",
                                    Arguments = "/c start https://github.com/ThirteenAG/Ultimate-ASI-Loader/releases/latest",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                };
                                Process.Start(psi);
                            }
                        }
                        DirectoryTxt.Text = "Game Directory:";
                        DirectoryTxt.FontWeight = FontWeights.Normal;
                        DirectoryTxt.TextDecorations = null;
                        TipsNote.TextDecorations = TextDecorations.Underline;
                        GameDirectory.Text = dialog.FileName;
                        LaunchOptionsPanel.IsEnabled = true;
                        DirectoryBtn.IsDefault = false;
                        ResetVars();

                        _isIvsdkInstalled = Directory.GetFiles(dialog.FileName, "IVSDKDotNet.asi", SearchOption.AllDirectories).FirstOrDefault() != null;

                        if (File.Exists($@"{dialog.FileName}\vulkan.dll") ||
                            File.Exists($@"{dialog.FileName}\d3d9.dll"))
                        {
                            isDxvkInstalled = true;
                            VidMemCheckbox.IsEnabled = false;
                            VidMemCheckbox.IsChecked = false;
                            Gb3Checkbox.IsEnabled = false;
                            Gb4Checkbox.IsEnabled = false;
                            UninstallDxvkBtn.Visibility = Visibility.Collapsed;
                        }
                        
                        if (File.Exists($@"{dialog.FileName}\vulkan.dll"))
                        {
                            _ffix = true;
                            _ffixLatest = true;
                        }
                        else
                        {
                            _ffixLatest = false;
                            MessageBox.Show("If you have FusionFix installed, please note that is outdated. It is highly recommended to install the latest version.");
                        }

                        if (_resultVk is { Item1: 0, Item2: 0 })
                        { DxvkPanel.IsEnabled = false; Logger.Debug(" DXVK is not supported - disabling the DXVK panel."); }
                        else
                        {
                            if (isDxvkInstalled)
                            {
                                Logger.Debug(" Detected that DXVK is likely already installed.");
                                if (_ffixLatest)
                                {
                                    InstallDxvkBtn.Content = "Update DXVK";
                                }
                                else
                                {
                                    InstallDxvkBtn.Content = "Reinstall DXVK";
                                    InstallDxvkBtn.IsDefault = false;
                                    LaunchOptionsBtn.IsDefault = true;
                                    LaunchOptionsBtn.FontWeight = FontWeights.SemiBold;
                                    InstallDxvkBtn.FontWeight = FontWeights.Normal;
                                    InstallDxvkBtn.Width = 78;
                                    InstallDxvkBtn.FontSize = 11;
                                    UninstallDxvkBtn.Visibility = Visibility.Visible;
                                }
                                MonitorDetailsCheckbox.IsChecked = true;
                                if (File.Exists($@"{dialog.FileName}\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -managed incase present.");
                                    var lines = File.ReadAllLines($@"{dialog.FileName}\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-managed"));
                                    File.WriteAllLines($@"{dialog.FileName}\commandline.txt", filteredLines);
                                }

                                var rtssGtaivConfig = new IniEditor(_rtssConfig);
                                if (_isIvsdkInstalled && !string.IsNullOrEmpty(_rtssConfig))
                                {
                                    if (rtssGtaivConfig.ReadValue("Hooking", "EnableHooking") == "1")
                                    {
                                        Logger.Info(" User has DXVK and IVSDK .NET and RTSS enabled at the same time. Showing the warning prompt.");
                                        MessageBox.Show($"You currently have RivaTuner Statistics Server enabled (it might be a part of MSI Afterburner).\n\nTo avoid issues with the game launching when DXVK and IVSDK .NET are both installed, go into your tray icons, press on the little monitor icon with the number 60, press 'Add' in bottom left, select GTA IV's executable and set Application detection level to 'None'.\n\nIf you want the statistics, set it to Low and restart the tool, with the game running.");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Debug(" DXVK is not installed, updating the button name incase the user changed directories before.");
                                InstallDxvkBtn.Content = "Install DXVK";
                            }
                            Logger.Debug(" Enabled the DXVK panel.");
                            DxvkPanel.IsEnabled = true;
                        }

                        var optionsChanged = false;
                        var optToChangeOptions = false;
                        var iniParserZp = new IniEditor(_iniPathZp);
                        var incompatibleOptions = new List<string>
                        {
                            "BenchmarkFix",
                            "BikePhoneAnimsFix",
                            "BorderlessWindowed",
                            "BuildingAlphaFix",
                            "BuildingDynamicShadows",
                            "CarDynamicShadowFix",
                            "CarPartsShadowFix",
                            "CutsceneFixes",
                            "DoNotPauseOnMinimize",
                            "DualVehicleHeadlights",
                            "EmissiveLerpFix",
                            "EpisodicVehicleSupport",
                            "EpisodicWeaponSupport",
                            "ForceCarHeadlightShadows",
                            "ForceDynamicShadowsEverywhere",
                            "ForceShadowsOnObjects",
                            "HighFPSBikePhysicsFix",
                            "HighFPSSpeedupFix",
                            "HighQualityReflections",
                            "ImprovedShaderStreaming",
                            "MouseFix",
                            "NewMemorySystem",
                            "NoLiveryLimit",
                            "NoLODLightHeightCutoff",
                            "OutOfCommissionFix",
                            "PoliceEpisodicWeaponSupport",
                            "RemoveUselessChecks",
                            "RemoveBoundingBoxCulling",
                            "ReversingLightFix",
                            "SkipIntro",
                            "SkipMenu"
                        };
                        switch (!string.IsNullOrEmpty(fusionFixIniPath), !string.IsNullOrEmpty(zolikaPatchIniPath))
                        {
                            case (false, false):
                                Logger.Debug(" User doesn't have neither ZolikaPatch or FusionFix. Disabling the Borderless Windowed toggle.");
                                WindowedCheckbox.IsChecked = false;
                                WindowedCheckbox.IsEnabled = false;
                                break;
                            case (true, false):
                                _iniPath = !string.IsNullOrEmpty(fusionFixCfgPath) ? fusionFixCfgPath : fusionFixIniPath;
                                _ffix = true;
                                Logger.Debug(" User has FusionFix.");
                                if (File.Exists($@"{dialog.FileName}\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -windowed and -noBlockOnLostFocus incase present.");
                                    var lines = File.ReadAllLines($@"{dialog.FileName}\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-windowed") &&
                                        !line.Contains("-noBlockOnLostFocus"));
                                    File.WriteAllLines($@"{dialog.FileName}\commandline.txt", filteredLines);
                                }
                                break;
                            case (false, true):
                                _iniPathZp = zolikaPatchIniPath;
                                _ffix = false;
                                Logger.Debug(" User has ZolikaPatch.");
                                var iniParser = new IniEditor(_iniPathZp);
                                if (iniParser.ReadValue("Options", "RestoreDeathMusic") == "N/A")
                                {
                                    var result = MessageBox.Show("Your ZolikaPatch is outdated.\n\nDo you wish to download the latest version? (this will redirect to Zolika1351's website for manual download)", "ZolikaPatch is outdated", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        var psi = new ProcessStartInfo
                                        {
                                            FileName = "cmd",
                                            Arguments = "/c start https://zolika1351.pages.dev/mods/ivpatch",
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                        };
                                        Process.Start(psi);
                                        MessageBox.Show("Press OK to restart the app after updating ZolikaPatch. Do not unpack 'PlayGTAIV.exe'.", "ZolikaPatch is outdated");
                                        System.Windows.Forms.Application.Restart();
                                        Environment.Exit(0);
                                    }
                                }
                                if (File.Exists($@"{dialog.FileName}\GFWLDLC.asi"))
                                {
                                    File.Delete($@"{dialog.FileName}\GFWLDLC.asi");
                                }
                                if (iniParser.ReadValue("Options", "LoadDLCs") == "0")
                                {
                                    iniParser.EditValue("Options", "LoadDLCs", "1");
                                }
                                if (File.Exists($@"{dialog.FileName}\dinput8.dll") && !File.Exists($@"{dialog.FileName}\xlive.dll"))
                                {
                                    var result = MessageBox.Show("You appear to be using GFWL. Do you wish to remove Steam Achievements (if exists) and fix ZolikaPatch options to receive GFWL achievements?\n\nPressing 'No' can revert this if you agreed to this earlier.", "GFWL Achievements", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        if (File.Exists($@"{dialog.FileName}\SteamAchievements.asi"))
                                        {
                                            if (!Directory.Exists($@"{dialog.FileName}\backup"))
                                            {
                                                Directory.CreateDirectory($@"{dialog.FileName}\backup");
                                            }
                                            File.Move($@"{dialog.FileName}\SteamAchievements.asi", $@"{dialog.FileName}\backup\SteamAchievements.asi");
                                        }
                                        if (iniParser.ReadValue("Options", "TryToSkipAllErrors") == "1")
                                        {
                                            iniParser.EditValue("Options", "TryToSkipAllErrors", "0");
                                        }
                                        if (iniParser.ReadValue("Options", "VSyncFix") == "1")
                                        {
                                            iniParser.EditValue("Options", "VSyncFix", "0");
                                        }
                                    }
                                    else
                                    {
                                        if (File.Exists($@"{dialog.FileName}\backup\SteamAchievements.asi")) { File.Move($@"{dialog.FileName}\backup\SteamAchievements.asi", $"{dialog.FileName}\\SteamAchievements.asi"); }
                                        if (iniParser.ReadValue("Options", "TryToSkipAllErrors") == "0")
                                        {
                                            iniParser.EditValue("Options", "TryToSkipAllErrors", "1");
                                        }
                                        if (iniParser.ReadValue("Options", "VSyncFix") == "0")
                                        {
                                            iniParser.EditValue("Options", "VSyncFix", "1");
                                        }
                                    }
                                }

                                break;
                            case (true, true):
                                Logger.Debug(" User has FusionFix and ZolikaPatch. Asking the user if they want to change incompatible ZolikaPatch options...");
                                _iniPath = fusionFixCfgPath;
                                _iniPathZp = zolikaPatchIniPath;
                                _ffix = true;

                                if (File.Exists($"{dialog.FileName}\\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -windowed and -noBlockOnLostFocus incase present.");
                                    var lines = File.ReadAllLines($@"{dialog.FileName}\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-windowed") &&
                                        !line.Contains("-noBlockOnLostFocus"));
                                    File.WriteAllLines($@"{dialog.FileName}\commandline.txt", filteredLines);

                                }
                                if (iniParserZp.ReadValue("Options", "RestoreDeathMusic") == "N/A")
                                {
                                    var result = MessageBox.Show("Your ZolikaPatch is outdated.\n\nDo you wish to download the latest version? (this will redirect to Zolika1351's website for manual download)", "ZolikaPatch is outdated", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        var psi = new ProcessStartInfo
                                        {
                                            FileName = "cmd",
                                            Arguments = "/c start https://zolika1351.pages.dev/mods/ivpatch",
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                        };
                                        Process.Start(psi);
                                        MessageBox.Show("Press OK to restart the app after updating ZolikaPatch. Do not unpack 'PlayGTAIV.exe'.", "ZolikaPatch is outdated");
                                        System.Windows.Forms.Application.Restart();
                                        Environment.Exit(0);
                                    }
                                }
                                if (File.Exists($@"{dialog.FileName}\GFWLDLC.asi"))
                                {
                                    if (iniParserZp.ReadValue("Options", "LoadDLCs") == "0" && iniParserZp.ReadValue("Options", "LoadDLCs") != "N/A")
                                    {
                                        iniParserZp.EditValue("Options", "LoadDLCs", "1");
                                        File.Delete($@"{dialog.FileName}\GFWLDLC.asi");
                                    }
                                }

                                if (File.Exists($@"{dialog.FileName}\dinput8.dll") && !File.Exists($@"{dialog.FileName}\xlive.dll"))
                                {
                                    var result = MessageBox.Show("You appear to be using GFWL. Do you wish to remove Steam Achievements (if exists) and fix ZolikaPatch options to receive GFWL achievements?\n\nPressing 'No' can revert this if you agreed to this earlier.", "GFWL Achievements", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        if (File.Exists($@"{dialog.FileName}\SteamAchievements.asi"))
                                        {
                                            if (!Directory.Exists($@"{dialog.FileName}\backup"))
                                            {
                                                Directory.CreateDirectory($@"{dialog.FileName}\backup");
                                            }
                                            File.Move($@"{dialog.FileName}\SteamAchievements.asi", $@"{dialog.FileName}\backup\SteamAchievements.asi");
                                        }
                                        if (iniParserZp.ReadValue("Options", "TryToSkipAllErrors") == "1")
                                        {
                                            iniParserZp.EditValue("Options", "TryToSkipAllErrors", "0");
                                        }
                                        if (iniParserZp.ReadValue("Options", "VSyncFix") == "1")
                                        {
                                            iniParserZp.EditValue("Options", "VSyncFix", "0");
                                        }
                                    }
                                    else
                                    {
                                        if (File.Exists($@"{dialog.FileName}\backup\SteamAchievements.asi")) { File.Move($@"{dialog.FileName}\backup\SteamAchievements.asi", $@"{dialog.FileName}\SteamAchievements.asi"); }
                                        if (iniParserZp.ReadValue("Options", "TryToSkipAllErrors") == "0")
                                        {
                                            iniParserZp.EditValue("Options", "TryToSkipAllErrors", "1");
                                        }
                                        if (iniParserZp.ReadValue("Options", "VSyncFix") == "0")
                                        {
                                            iniParserZp.EditValue("Options", "VSyncFix", "1");
                                        }
                                    }
                                }

                                foreach (var option in incompatibleOptions.Where(option => iniParserZp.ReadValue("Options", option) == "1"))
                                {
                                    if (!optToChangeOptions)
                                    {
                                        var result = MessageBox.Show("Your ZolikaPatch options are incompatible with FusionFix. This may lead to crashes, inconsistencies, visual issues etc.\n\nDo you wish to fix the options?", "Fix ZolikaPatch - FusionFix compatibility?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                        if (result == MessageBoxResult.Yes)
                                        {
                                            optToChangeOptions = true;
                                            iniParserZp.EditValue("Options", option, "0");
                                            optionsChanged = true;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        iniParserZp.EditValue("Options", option, "0");
                                        optionsChanged = true;
                                    }
                                }
                                if (optionsChanged) { iniParserZp.SaveFile(); }
                                break;

                        }

                        break;
                    }
                    Logger.Debug(" User selected the wrong folder. Displaying a MessageBox.");
                    MessageBox.Show("The selected folder does not contain a supported version of GTA IV.");
                }
                else
                {
                    break;
                }

            }
        }

        private bool _downloadFinished;
        private bool _extractFinished;

        private async Task InstallDxvk(string downloadUrl)
        {
            try
            {
                Logger.Debug(" Downloading the .tar.gz...");
                Logger.Debug(" Downloading the selected release...");
        
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        await using (var contentStream = await response.Content.ReadAsStreamAsync())
                        await using (var fileStream = new FileStream("./dxvk.tar.gz", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalBytesRead = 0L;
                            int bytesRead;
                    
                            while ((bytesRead = await contentStream.ReadAsync(buffer)) != 0)
                            {
                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                                totalBytesRead += bytesRead;

                                if (totalBytes <= 0) continue;
                                var percentage = (double)totalBytesRead / totalBytes * 100;
                                int percentageInt = Convert.ToInt16(percentage);
                            
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    InstallDxvkBtn.Content = $"Downloading... ({percentageInt}%)";
                                });
                            }
                        }
                    }
                }
        
                _downloadFinished = true;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Error downloading DXVK");
                throw;
            }
        }
        private async Task ExtractDxvk(string installationDir, List<string> dxvkConf)
        {
            Logger.Debug(" Extracting the d3d9.dll from the archive...");
            await using (var fsIn = new FileStream("./dxvk.tar.gz", FileMode.Open))
            await using (var gzipStream = new GZipStream(fsIn, CompressionMode.Decompress))
            {
                var tarReader = new TarReader(gzipStream);
                while (await tarReader.GetNextEntryAsync() is { } entry)
                {
                    Logger.Debug(entry.Name);
                    if (!entry.Name.EndsWith("x32/d3d9.dll")) continue;
                    await using var fsOut = File.Create(Path.Combine(installationDir, _ffixLatest ? "vulkan.dll" : "d3d9.dll"));
                    await entry.DataStream!.CopyToAsync(fsOut);
                    Logger.Debug(" d3d9.dll extracted into the game folder.");
                    break;
                }
            }

            Logger.Debug(" Deleting the .tar.gz...");
            File.Delete("dxvk.tar.gz");

            Logger.Debug(" Writing the dxvk.conf...");
            await using (var confWriter = File.CreateText(Path.Combine(installationDir, "dxvk.conf")))
            {
                foreach (var option in dxvkConf)
                {
                    await confWriter.WriteLineAsync(option);
                }
            }
            Logger.Debug(" dxvk.conf successfully written to game folder.");
            _extractFinished = true;
        }
        
        private async Task DownloadDxvk(string link, List<string> dxvkConf, bool gitlab, bool alt, int release = 0)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
            var firstResponse = await httpClient.GetAsync(link);
            firstResponse.EnsureSuccessStatusCode();
            var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(firstResponseBody).RootElement;
            var downloadUrl = (gitlab, alt) switch
            {
                (false, false) => parsed.GetProperty("assets")[release].GetProperty("browser_download_url").GetString(),
                (true, false) => parsed[release]
                    .GetProperty("assets")
                    .GetProperty("links")
                    .EnumerateArray()
                    .First(jsonElement => jsonElement.GetProperty("name").GetString()!.Contains("tar.gz"))
                    .GetProperty("url")
                    .GetString(),
                (false, true) => parsed.GetProperty("browser_download_url").GetString(),
                (true, true) => parsed.GetProperty("assets")
                    .GetProperty("links")
                    .EnumerateArray()
                    .First(jsonElement => jsonElement.GetProperty("name").GetString()!.Contains("tar.gz"))
                    .GetProperty("url")
                    .GetString()
            };
            await InstallDxvk(downloadUrl!);
            while (!_downloadFinished)
            {
                await Task.Delay(500);
            }
            _downloadFinished = false;
            await ExtractDxvk(GameDirectory.Text, dxvkConf);
        }
        private async void InstallDxvkBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User clicked on the Install DXVK button.");
            DxvkPanel.IsEnabled = false;

            Logger.Info(" Removing old files if present.");
            List<string> tobedeleted = ["d3d10.dll", "d3d10_1.dll", "d3d10core.dll", "d3d11.dll", "dxgi.dll", "dxvk.conf", "GTAIV.dxvk-cache", "PlayGTAIV.dxvk-cache", "LaunchGTAIV.dxvk-cache", "GTAIV_d3d9.log", "PlayGTAIV_d3d9.log", "LaunchGTAIV_d3d9.log"];
            if (!_ffixLatest) tobedeleted.Add("d3d9.dll");
            DeleteFiles(GameDirectory.Text, tobedeleted);

            InstallDxvkBtn.Content = "Installing...";
            var rtssGtaivConfig = new IniEditor(_rtssConfig);
            if (_isIvsdkInstalled && !string.IsNullOrEmpty(_rtssConfig))
            {
                if (rtssGtaivConfig.ReadValue("Hooking", "EnableHooking") == "1")
                {
                    Logger.Info(" User has IVSDK .NET installed and RTSS enabled at the same time and wants to install DXVK. Showing the warning prompt.");
                    MessageBox.Show($"You currently have RivaTuner Statistics Server enabled (it might be a part of MSI Afterburner).\n\nTo avoid issues with the game launching when DXVK and IVSDK .NET are both installed, go into your tray icons, press on the little monitor icon with the number 60, press 'Add' in bottom left, select GTA IV's executable and set Application detection level to 'None'.\n\nIf you want the statistics, set it to Low and restart the tool, with the game running.");
                }
            }

            List<string> dxvkConfig = [];

            Logger.Debug(" Setting up dxvk.conf in accordance with user's choices.");

            if (VSyncCheckbox.IsChecked == true)
            {
                Logger.Debug(" Adding d3d9.presentInterval = 1 and d3d9.numBackBuffers = 3");
                dxvkConfig.Add("d3d9.presentInterval = 1");
                dxvkConfig.Add("d3d9.numBackBuffers = 3");
            }
            if (FrameLatencyCheckBox.IsChecked == true)
            {
                Logger.Debug(" Adding d3d9.maxFrameLatency = 1");
                dxvkConfig.Add("d3d9.maxFrameLatency = 1");
            }

            Logger.Debug(" Quering links to install DXVK...");
            switch (_installDxvk)
            {
                case 1:
                    // we're using the "if" in each case because of the async checkbox
                    if (AsyncCheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing Latest DXVK-Sarek-async...");
                        dxvkConfig.Add("dxvk.enableAsync = true");
                        await DownloadDxvk("https://api.github.com/repos/pythonlover02/dxvk-Sarek/releases/latest", dxvkConfig, false, false);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"Latest DXVK-Sarek-async has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK-Sarek-async has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing Latest DXVK-Sarek...");
                        await DownloadDxvk("https://api.github.com/repos/pythonlover02/dxvk-Sarek/releases/latest", dxvkConfig, false, false, 1);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"Latest DXVK-Sarek has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK-Sarek has been installed!");
                    }
                    break;
                case 2:
                    if (AsyncCheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-gplasync 2.6.2...");
                        dxvkConfig.Add("dxvk.enableAsync = true");
                        await DownloadDxvk("https://gitlab.com/api/v4/projects/43488626/releases/v2.6.2-1", dxvkConfig, true, true);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"DXVK-gplasync 2.6.2 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" DXVK-async 2.6.2 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 2.6.2...");
                        await DownloadDxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/222230856", dxvkConfig, false, true);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"DXVK 2.6.2 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.", "Information");
                        Logger.Info(" DXVK 2.6.2 has been installed!");
                    }
                    break;
                case 3:
                    if (AsyncCheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing Latest DXVK-gplasync...");
                        dxvkConfig.Add("dxvk.enableAsync = true");
                        dxvkConfig.Add("dxvk.gplAsyncCache = true");
                        await DownloadDxvk("https://gitlab.com/api/v4/projects/43488626/releases/", dxvkConfig, true, false);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"Latest DXVK-gplasync has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK-gplasync has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing Latest DXVK...");
                        await DownloadDxvk("https://api.github.com/repos/doitsujin/dxvk/releases/latest", dxvkConfig, false, false);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"Latest DXVK has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK has been installed!");
                    }
                    break;
                case -1:
                    if (AsyncCheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.1...");
                        dxvkConfig.Add("dxvk.enableAsync = true");
                        await DownloadDxvk("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/60677007", dxvkConfig, false, true);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"DXVK-async 1.10.1 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" DXVK-async 1.10.1 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.1...");
                        await DownloadDxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/60669426", dxvkConfig, false, true);
                        while (!_extractFinished)
                        {
                            await Task.Delay(500);
                        }
                        _extractFinished = false;
                        MessageBox.Show($"DXVK 1.10.1 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.", "Information");
                        Logger.Info(" DXVK 1.10.1 has been installed!");
                    }
                    break;
            }
            Logger.Debug(" DXVK installed, editing the launch options toggles and enabling the panels back...");
            if (_ffixLatest)
            {
                InstallDxvkBtn.Content = "Update DXVK";
            }
            else
            {
                InstallDxvkBtn.Content = "Reinstall DXVK";
                InstallDxvkBtn.Width = 78;
                InstallDxvkBtn.FontSize = 11;
                UninstallDxvkBtn.Visibility = Visibility.Visible;
            }
            InstallDxvkBtn.IsDefault = false;
            LaunchOptionsBtn.IsDefault = true;
            InstallDxvkBtn.FontWeight = FontWeights.Normal;
            MonitorDetailsCheckbox.IsChecked = true;
            DxvkPanel.IsEnabled = true;
        }

        private void UninstallDxvkBtn_Click(object sender, RoutedEventArgs e)
        {
            DxvkPanel.IsEnabled = false;
            Logger.Info(" Removing all DXVK files present.");
            List<string> tobedeleted = ["d3d10.dll", "d3d10_1.dll", "d3d10core.dll", "d3d11.dll", "dxgi.dll", "dxvk.conf", "GTAIV.dxvk-cache", "PlayGTAIV.dxvk-cache", "LaunchGTAIV.dxvk-cache", "GTAIV_d3d9.log", "PlayGTAIV_d3d9.log", "LaunchGTAIV_d3d9.log"];
            if (!_ffixLatest) tobedeleted.Add("d3d9.dll");
            foreach (var element in tobedeleted)
            {
                Console.WriteLine(element);
            }
            DeleteFiles(GameDirectory.Text, tobedeleted);
            UninstallDxvkBtn.Visibility = Visibility.Collapsed;
            InstallDxvkBtn.Content = "Install DXVK";
            InstallDxvkBtn.IsDefault = true;
            InstallDxvkBtn.FontWeight = FontWeights.SemiBold;
            InstallDxvkBtn.Width = 160;
            InstallDxvkBtn.FontSize = 12;
            LaunchOptionsBtn.IsDefault = false;
            MonitorDetailsCheckbox.IsChecked = false;
            MessageBox.Show("DXVK with all it's remains successfully uninstalled.");
            DxvkPanel.IsEnabled = true;
        }
        private void SetupLaunchOptions_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User clicked on Setup Launch Options, checking the toggles...");
            var launchOptions = new List<string>();
            if (NoRestrictionsCheckbox.IsChecked == true) { launchOptions.Add("-norestrictions"); Logger.Debug(" Added -norestrictions."); }
            if (NoMemRestrictCheckbox.IsChecked == true) { launchOptions.Add("-nomemrestrict"); Logger.Debug(" Added -nomemrestrict."); }

            var ffWindowed = true;
            var ffBorderless = true;
            var ffFocusLossless = true;
            if (WindowedCheckbox.IsEnabled)
            {
                var iniParser = new IniEditor(_iniPath);
                bool borderlessWindowedValue;
                if (_ffix)
                {
                    ffWindowed = iniParser.ReadValue("MAIN", "Windowed") == "1";
                    ffBorderless = iniParser.ReadValue("MAIN", "BorderlessWindowed") == "1";
                    ffFocusLossless = iniParser.ReadValue("MAIN", "BlockOnLostFocus") == "0";
                    borderlessWindowedValue = ffWindowed && ffBorderless && ffFocusLossless;
                }
                else
                {
                    borderlessWindowedValue = iniParser.ReadValue("Options", "BorderlessWindowed") == "1";
                }
                switch (WindowedCheckbox.IsChecked)
                {
                    case true:
                    {
                        Logger.Debug(" User chose to enable Borderless Windowed");
                        if (!borderlessWindowedValue)
                        {
                            Logger.Debug(" Borderless Windowed is disabled in the ini, enabling it back...");
                            if (_ffix)
                            {
                                if (!ffWindowed) { iniParser.EditValue("MAIN", "Windowed", "1"); }
                                if (!ffBorderless) { iniParser.EditValue("MAIN", "BorderlessWindowed", "1"); }
                                if (!ffFocusLossless) { iniParser.EditValue("MAIN", "BlockOnLostFocus", "0"); }
                                Logger.Debug(" Enabled Borderless Windowed and disabled Pause Game on Focus Loss.");
                            }
                            else
                            {
                                launchOptions.Add("-windowed");
                                launchOptions.Add("-noBlockOnLostFocus");
                                iniParser.EditValue("Options", "BorderlessWindowed", "1");
                                Logger.Debug(" Added -windowed and -noBlockOnLostFocus.");
                            }
                            iniParser.SaveFile();
                        }

                        break;
                    }
                    case false when (borderlessWindowedValue || ffWindowed || ffBorderless || ffFocusLossless):
                    {
                        Logger.Debug(" User chose to disable Borderless Windowed but it's enabled in the ini, disabling it...");
                        if (_ffix)
                        {
                            iniParser.EditValue("MAIN", "Windowed", "0");
                            iniParser.EditValue("MAIN", "BorderlessWindowed", "0");
                            iniParser.EditValue("MAIN", "BlockOnLostFocus", "1");
                        }
                        else
                        {
                            iniParser.EditValue("Options", "BorderlessWindowed", "0");
                        }
                        iniParser.SaveFile();
                        break;
                    }
                }
            }
            if (!_ffixLatest)
            {
                if (VidMemCheckbox.IsChecked == true)
                {
                    Logger.Debug(" -availablevidmem checked, quering user's VRAM...");
                    try
                    {
                        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                        var videoControllers = searcher.Get();

                        foreach (var o in videoControllers)
                        {
                            var obj = (ManagementObject)o;
                            var adapterRam = obj["AdapterRAM"] != null ? obj["AdapterRAM"].ToString() : "N/A";
                            if (adapterRam == "N/A") continue;
                            var tempVram = Convert.ToInt16(ByteSize.FromBytes(Convert.ToDouble(adapterRam)).MebiBytes + 1);
                            if (_firstGpu)
                            {
                                Logger.Debug($"GPU0 has {tempVram}MB of VRAM");
                                _vram1 = tempVram;
                                _firstGpu = false;
                            }
                            else if (tempVram > _vram1 || tempVram > _vram2)
                            {
                                Logger.Debug($"Next GPU has {tempVram}MB of VRAM");
                                _vram2 = tempVram;
                            }
                        }

                        if (_resultVk.Item4 || _resultVk.Item5)
                        {
                            if (Gb3Checkbox.IsChecked == true)
                            {
                                if (_vram1 > 3072) _vram1 = 3072;
                            }
                            else
                            {
                                if (_vram1 > 4096) _vram1 = 4096;
                            }
                            launchOptions.Add($"-availablevidmem {_vram1}");
                            Logger.Debug($" Added -availablevidmem {_vram1}.");
                        }
                        else
                        {
                            var vram = !_dxvkOnIgpu ? Math.Max(_vram1, _vram2) : Math.Min(_vram1, _vram2);
                            if (Gb3Checkbox.IsChecked == true)
                            {
                                if (vram > 3072) vram = 3072;
                            }
                            else
                            {
                                if (vram > 4096) vram = 4096;
                            }
                            launchOptions.Add($"-availablevidmem {vram}");
                            Logger.Debug($" Added -availablevidmem {vram}.");
                        }
                    }
                    catch (Exception ex)
                    {
                       // i know this is an awful and unoptimized and full of bad practices implementation, plz forgib
                       Logger.Error(ex, "Had some weird error during quering vram; asking the user for manual input");
                       var noerror = false;
                       var vram = 0;
                       while (!noerror)
                       {
                           try
                           {
                               vram = Convert.ToInt16(PromptDialog.Dialog.Prompt("VRAM could not be queried automatically.\n\nInput your VRAM value (in MB):", "Failsafe VRAM input"));
                               noerror = true;
                           }
                           catch
                           {
                               Logger.Error("Didn't receive a number, requesting again");
                               MessageBox.Show("Not a number, try again...");
                           }

                       }
                       if (Gb3Checkbox.IsChecked == true)
                       {
                            if (vram > 3072) vram = 3072;
                       }
                       else
                       {
                            if (vram > 4096) vram = 4096;
                       }
                       launchOptions.Add($"-availablevidmem {vram}");
                       Logger.Debug($" Added -availablevidmem {vram}.");
                    }
                }
            }
            if (MonitorDetailsCheckbox.IsChecked == true)
            {
                Logger.Debug(" Monitor Details checked, quering user's monitor details...");
                DisplayInfo.GetPrimaryDisplayInfo(out var width, out var height, out var refreshRate);
                launchOptions.Add($"-width {width}");
                launchOptions.Add($"-height {height}");
                launchOptions.Add($"-refreshrate {refreshRate}");
                Logger.Debug($" Added -width {width}, -height {height}, -refreshrate {refreshRate}.");
            }
            if (!File.Exists($"{GameDirectory.Text}\\d3d9.dll") && !_ffixLatest)
            {
                launchOptions.Add("-managed");
            }
            if (_isRetail)
            {
                Logger.Debug(" Game .exe is retail - inputting values via commandline.txt...");
                if (File.Exists($"{GameDirectory.Text}\\commandline.txt"))
                {
                    Logger.Debug(" Old commandline.txt detected, removing...");
                    File.Delete($"{GameDirectory.Text}\\commandline.txt");
                }
                Logger.Debug(" Writing new commandline.txt...");
                using (var writer = new StreamWriter($@"{GameDirectory.Text}\commandline.txt"))
                {
                    foreach (var line in launchOptions)
                    {
                        writer.WriteLine(line);
                    }
                }
                Logger.Info($" Following launch options have been set to commandline.txt: {string.Join(" ", launchOptions)}");
                MessageBox.Show($"Following launch options have been set up automatically for you: \n\n{string.Join(" ", launchOptions)}\n\nDo not worry that VRAM value isn't your full value - that is intentional and you can change that if you need to.");
            }
            else
            {
                Logger.Info($" Game .exe is 1.2 or later - asked user to input the values on their own and copied them to clipboard: {string.Join(" ", launchOptions)}");
                MessageBox.Show($" The app can't set the launch options automatically, paste them in Steam's Launch Options manually (will be copied to clipboard after you press Ok):\n\n{string.Join(" ", launchOptions)}\n\nDo not worry that VRAM value isn't your full value - that is intentional and you can change that if you need to.");
                try { Clipboard.SetText(string.Join(" ", launchOptions)); } catch (Exception ex) { MessageBox.Show($" The app couldn't copy the options to clipboard - input them manually:\n\n{string.Join(" ", launchOptions)}"); Logger.Debug(ex, " Weird issues with clipboard access."); }
            }

        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Logger.Debug(" User clicked on a hyperlink from the main window.");
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c start {e.Uri.AbsoluteUri}",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process.Start(psi);
        }
    }
}
