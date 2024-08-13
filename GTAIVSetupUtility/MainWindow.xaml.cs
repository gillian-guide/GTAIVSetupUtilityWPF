using ByteSizeLib;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using GTAIVSetupUtilityWPF.Common;
using GTAIVSetupUtilityWPF.Functions;
using System.Threading.Tasks;
using PromptDialog;
using System.Net;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Shapes;
// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVSetupUtilityWPF
{
    public partial class MainWindow : Window
    {

        (int, int, bool, bool, bool, bool) resultvk;
        int installdxvk;
        int vram1;
        int vram2;
        bool ffix;
        bool isretail;
        bool isIVSDKInstalled;
        bool dxvkonigpu;
        bool firstgpu = true;
        string rtssconfig = File.Exists("C:\\Program Files (x86)\\RivaTuner Statistics Server\\Profiles\\GTAIV.exe.cfg") ? "C:\\Program Files (x86)\\RivaTuner Statistics Server\\Profiles\\GTAIV.exe.cfg" : File.Exists("C:\\Program Files (x86)\\RivaTuner Statistics Server\\Profiles\\Global") ? "C:\\Program Files (x86)\\RivaTuner Statistics Server\\Profiles\\Global" : string.Empty;
        string iniModify;
        string ziniModify;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
        public MainWindow()
        {
            if ( File.Exists("GTAIVSetupUtilityLog.txt")) { File.Delete("GTAIVSetupUtilityLog.txt"); }
            NLog.LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "GTAIVSetupUtilityLog.txt");
            });
            Logger.Info(" Initializing the main window...");
            InitializeComponent();
            Logger.Info(" Main window initialized!");

            Logger.Info(" Initializing the vulkan check...");
            resultvk = VulkanChecker.VulkanCheck();
            Logger.Info(" Vulkan check finished!");
            if (resultvk.Item6 && resultvk.Item1 == 2) { asynccheckbox.IsChecked = false; Logger.Debug($" User has an NVIDIA GPU, untoggling the async checkbox..."); }
        }


        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                Logger.Debug(" Successfully downloaded.");
                downloadfinished = true;
                installdxvkbtn.Content = $"Installing...";
            });
        }
        private void async_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled async.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("DXVK with async should provide better performance for most, but under some conditions it may provide worse performance instead. Without async, you might stutter the first time you see different areas. It won't stutter the next time in the same area.\n\nNote, however, that performance on NVIDIA when using DXVK 2.0+ may be worse. Feel free to experiment by re-installing DXVK.");
            }
        }
        private void vsync_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled VSync.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("The in-game VSync implementation produces framepacing issues. DXVK's VSync implementation should be preferred.\n\nIt's recommended to keep this on and in-game's implementation off.");
            }
        }
        private void latency_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Max Frame Latency.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option may help avoiding further framepacing issues. It's recommended to keep this on.");
            }
        }
        private void norestrictions_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -norestrictions.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option allows you to set any in-game settings independently of what the game restricts you to. It's recommended to keep this on. ");
            }
        }

        private void nomemrestrict_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -nomemrestrict.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("-nomemrestrict allows the game to use all the dedicated memory resources up to the limits. It's recommended to keep this on.");
            }
        }
        private void windowed_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option allows to use Borderless Fullscreen instead of Exclusive Fullscreen. Provides better experience and sometimes better performance. It's recommended to keep this on.");
            }
        }
        private void vidmem_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -availablevidmem.");
            if (vidmemcheck.IsChecked == false)
            {
                gb3check.IsEnabled = false;
                gb4check.IsEnabled = false;
            }
            else
            {
                gb3check.IsEnabled = true;
                gb4check.IsEnabled = true;
            }
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option forces a specific value of video memory due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
            }
        }

        private void vidmemlock_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User changed the vidmem lock");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option allows to change between a 3GB and a 4GB VRAM lock when setting -availablevidmem up.\n\nThe game is 32-bit, so going beyond 4GB is entirely pointless. However, some people reported less issues with it being locked to 3GB instead.\n\nIt's recommended to keep this at default.");
            }
        }
        private void monitordetail_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled Monitor Details.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option forces a specific resolution and refresh rate due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User opened the About window.");
            MessageBox.Show(
                "This software is made by Gillian for the Modding Guide. Below is debug text, you don't need it normally.\n\n" +
                $"Install DXVK: {installdxvk}\n" +
                $"dGPU DXVK Support: {resultvk.Item1}\n" +
                $"iGPU DXVK Support: {resultvk.Item2}\n" +
                $"iGPU Only: {resultvk.Item3}\n" +
                $"dGPU Only: {resultvk.Item4}\n" +
                $"Intel iGPU: {resultvk.Item5}\n" +
                $"NVIDIA GPU: {resultvk.Item6}\n\n" +
                $"Version: {GetAssemblyVersion()}",
                "Information");
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User is selecting the game folder...");
            while (true)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\Grand Theft Auto IV\\GTAIV";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Logger.Debug(" User selected a folder, proceeding...");
                    string gamever = AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe");
                    if (gamever.StartsWith("1, 0") || (gamever.StartsWith("1.2")))
                    {
                        if (File.Exists($"{dialog.FileName}\\commandline.txt")) {
                            Logger.Debug(" Filtering commandline.txt from garbage because I know *somebody* will have a garbage commandline.");
                            string[] lines = File.ReadAllLines($"{dialog.FileName}\\commandline.txt");
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
                                !line.Contains("-forcer2vb") &&
                                !line.Contains("-minspecaudio"));
                                File.WriteAllLines($"{dialog.FileName}\\commandline.txt", filteredLines);
                        }
                        if (gamever.StartsWith("1, 0")) { isretail = true; Logger.Debug(" Folder contains a retail exe."); }
                        else {
                            Logger.Debug(" Folder contains an exe of Steam Version.");
                            isretail = false;
                            if (File.Exists($"{dialog.FileName}\\commandline.txt")){
                                Logger.Info("commandline.txt detected on Steam Version...");
                                MessageBox.Show("You appear to have a commandline.txt, however your are using the Steam version which doesn't use that file. Consider moving these options to Steam Launch Options or launch arguments.");
                            }
                          }
                        if (isretail && !gamever.StartsWith("1, 0, 8"))
                        { vidmemcheck.IsEnabled = false; gb3check.IsEnabled = false; gb4check.IsEnabled = false; Logger.Debug(" Folder contains an exe of some pre-1.0.8.0 version. Disabling the -availablevidmem toggle."); }
                        if (File.Exists($"{dialog.FileName}\\dsound.dll"))
                        {
                            Logger.Info("dsound.dll detected, warning the user...");
                            var result = MessageBox.Show("You appear to have an outdated ASI Loader (dsound.dll). Consider removing it.\n\nPress 'Yes' to get redirected to download to the latest version - download the non-x64 one, rename dinput8.dll to xlive.dll if you don't plan to play GFWL and using non-Steam version.", "Outdated ASI loader.", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    FileName = "cmd",
                                    Arguments = $"/c start {"https://github.com/ThirteenAG/Ultimate-ASI-Loader/releases/latest"}",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                };
                                Process.Start(psi);
                            }
                        }
                        directorytxt.Text = "Game Directory:";
                        directorytxt.FontWeight = FontWeights.Normal;
                        directorytxt.TextDecorations = null;
                        tipsnote.TextDecorations = TextDecorations.Underline;
                        gamedirectory.Text = dialog.FileName;
                        launchoptionsPanel.IsEnabled = true;

                        isIVSDKInstalled = Directory.GetFiles(dialog.FileName, "IVSDKDotNet.asi", SearchOption.AllDirectories).FirstOrDefault()!=null;
                        bool isDXVKInstalled = File.Exists($"{dialog.FileName}\\d3d9.dll");
                        if (resultvk.Item1 == 0 && resultvk.Item2 == 0)
                            { dxvkPanel.IsEnabled = false; Logger.Debug(" DXVK is not supported - disabling the DXVK panel."); }
                        else
                        {
                            if (isDXVKInstalled)
                            {
                                Logger.Debug(" Detected d3d9.dll - likely DXVK is already installed.");
                                installdxvkbtn.Content = "Reinstall DXVK";
                                monitordetailcheck.IsChecked = true;
                                if (File.Exists($"{dialog.FileName}\\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -managed incase present.");
                                    string[] lines = File.ReadAllLines($"{dialog.FileName}\\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-managed"));
                                    File.WriteAllLines($"{dialog.FileName}\\commandline.txt", filteredLines);
                                }
                                if (isIVSDKInstalled && !string.IsNullOrEmpty(rtssconfig))
                                {
                                    IniEditor rtssGTAIVConfig = new IniEditor(rtssconfig);
                                    if (rtssGTAIVConfig.ReadValue("Hooking", "EnableHooking") == "1")
                                    {
                                        Logger.Info(" User has DXVK and IVSDK .NET and RTSS enabled at the same time. Showing the warning prompt.");
                                        MessageBox.Show($"You currently have RivaTuner Statistics Server enabled (it might be a part of MSI Afterburner).\n\nTo avoid issues with the game launching when DXVK and IVSDK .NET are both installed, go into your tray icons, press on the little monitor icon with the number 60, press 'Add' in bottom left, select GTA IV's executable and set Application detection level to 'None'.\n\nIf you want the statistics, set it to Low and restart the tool, with the game running.");
                                    }
                                }
                            }
                            else
                            {
                                Logger.Debug(" DXVK is not installed, updating the button name incase the user changed directories before.");
                                installdxvkbtn.Content = "Install DXVK";
                            }
                            Logger.Debug(" Enabled the DXVK panel.");
                            dxvkPanel.IsEnabled = true;
                        }

                        string fusionFixPath = Directory.GetFiles(dialog.FileName, "GTAIV.EFLC.FusionFix.ini", SearchOption.AllDirectories).FirstOrDefault();
                        string fusionFixCfgPath = Directory.GetFiles(dialog.FileName, "GTAIV.EFLC.FusionFix.cfg", SearchOption.AllDirectories).FirstOrDefault();
                        string zolikaPatchPath = Directory.GetFiles(dialog.FileName, "ZolikaPatch.ini", SearchOption.AllDirectories).FirstOrDefault();

                        switch (!string.IsNullOrEmpty(fusionFixPath), !string.IsNullOrEmpty(zolikaPatchPath))
                        {
                            case (false, false):
                                Logger.Debug(" User doesn't have neither ZolikaPatch or FusionFix. Disabling the Borderless Windowed toggle.");
                                windowedcheck.IsChecked = false;
                                windowedcheck.IsEnabled = false;
                                break;
                            case (true, false):
                                iniModify = !string.IsNullOrEmpty(fusionFixCfgPath) ? fusionFixCfgPath : fusionFixPath;
                                ffix = true;
                                Logger.Debug(" User has FusionFix.");
                                if (File.Exists($"{dialog.FileName}\\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -windowed and -noBlockOnLostFocus incase present.");
                                    string[] lines = File.ReadAllLines($"{dialog.FileName}\\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-windowed") &&
                                        !line.Contains("-noBlockOnLostFocus"));
                                    File.WriteAllLines($"{dialog.FileName}\\commandline.txt", filteredLines);
                                }
                                break;
                            case (false, true):
                                iniModify = zolikaPatchPath;
                                ffix = false;
                                Logger.Debug(" User has ZolikaPatch.");
                                IniEditor iniParserft = new IniEditor(ziniModify);
                                if (iniParserft.ReadValue("Options", "RestoreDeathMusic") == "N/A")
                                {
                                    var result = MessageBox.Show("Your ZolikaPatch is outdated.\n\nDo you wish to download the latest version? (this will redirect to Zolika1351's website for manual download)", "ZolikaPatch is outdated", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        ProcessStartInfo psi = new ProcessStartInfo
                                        {
                                            FileName = "cmd",
                                            Arguments = $"/c start {"https://zolika1351.pages.dev/mods/ivpatch"}",
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                        };
                                        Process.Start(psi);
                                        MessageBox.Show("Press OK to restart the app after updating ZolikaPatch. Do not unpack 'PlayGTAIV.exe'.", "ZolikaPatch is outdated");
                                        System.Windows.Forms.Application.Restart();
                                        Environment.Exit(0);
                                    }
                                }
                                if (File.Exists($"{dialog.FileName}\\GFWLDLC.asi"))
                                {
                                    File.Delete($"{dialog.FileName}\\GFWLDLC.asi");
                                }
                                if (iniParserft.ReadValue("Options", "LoadDLCs") == "0")
                                {
                                    iniParserft.EditValue("Options", "LoadDLCs", "1");
                                }
                                if (File.Exists($"{dialog.FileName}\\dinput8.dll") && !File.Exists($"{dialog.FileName}\\xlive.dll"))
                                {
                                    var result = MessageBox.Show("You appear to be using GFWL. Do you wish to remove Steam Achievements (if exists) and fix ZolikaPatch options to receive GFWL achievements?\n\nPressing 'No' can revert this if you agreed to this earlier.", "GFWL Achievements", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        if (File.Exists($"{dialog.FileName}\\SteamAchievements.asi"))
                                       {
                                            if (!Directory.Exists($"{dialog.FileName}\\backup"))
                                            {
                                                Directory.CreateDirectory($"{dialog.FileName}\\backup");
                                            }
                                            File.Move($"{dialog.FileName}\\SteamAchievements.asi", $"{dialog.FileName}\\backup\\SteamAchievements.asi");
                                        }
                                        if (iniParserft.ReadValue("Options", "TryToSkipAllErrors") == "1")
                                        {
                                            iniParserft.EditValue("Options", "TryToSkipAllErrors", "0");
                                        }
                                        if (iniParserft.ReadValue("Options", "VSyncFix") == "1")
                                        {
                                            iniParserft.EditValue("Options", "VSyncFix", "0");
                                        }
                                    }
                                    else
                                    {
                                        if (File.Exists($"{dialog.FileName}\\backup\\SteamAchievements.asi")) { File.Move($"{dialog.FileName}\\backup\\SteamAchievements.asi", $"{dialog.FileName}\\SteamAchievements.asi"); }
                                        if (iniParserft.ReadValue("Options", "TryToSkipAllErrors") == "0")
                                        {
                                            iniParserft.EditValue("Options", "TryToSkipAllErrors", "1");
                                        }
                                        if (iniParserft.ReadValue("Options", "VSyncFix") == "0")
                                        {
                                            iniParserft.EditValue("Options", "VSyncFix", "1");
                                        }
                                    }
                                }

                                break;
                            case (true, true):
                                Logger.Debug(" User has FusionFix and ZolikaPatch. Asking the user if they want to change incompatible ZolikaPatch options...");
                                iniModify = fusionFixCfgPath;
                                ziniModify = zolikaPatchPath;
                                ffix = true;
                                IniEditor iniParsertt = new IniEditor(ziniModify);
                                bool optionsChanged = false;
                                bool optToChangeOptions = false;
                                List<string> incompatibleOptions = new List<string>()
                                {
                                    "BuildingAlphaFix",
                                    "EmissiveLerpFix",
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
                                    "HighFPSBikePhysicsFix",
                                    "HighFPSSpeedupFix",
                                    "HighQualityReflections",
                                    "ImprovedShaderStreaming",
                                    "MouseFix",
                                    "NewMemorySystem",
                                    "NoLiveryLimit",
                                    "OutOfCommissionFix",
                                    "PoliceEpisodicWeaponSupport",
                                    "RemoveBoundingBoxCulling",
                                    "ReversingLightFix",
                                    "SkipIntro",
                                    "SkipMenu"
                                };

                                if (File.Exists($"{dialog.FileName}\\commandline.txt"))
                                {
                                    Logger.Debug(" Filtering commandline.txt from -windowed and -noBlockOnLostFocus incase present.");
                                    string[] lines = File.ReadAllLines($"{dialog.FileName}\\commandline.txt");
                                    var filteredLines = lines.Where(line =>
                                        !line.Contains("-windowed") &&
                                        !line.Contains("-noBlockOnLostFocus"));
                                    File.WriteAllLines($"{dialog.FileName}\\commandline.txt", filteredLines);

                                }
                                if (iniParsertt.ReadValue("Options", "RestoreDeathMusic") == "N/A")
                                {
                                    var result = MessageBox.Show("Your ZolikaPatch is outdated.\n\nDo you wish to download the latest version? (this will redirect to Zolika1351's website for manual download)", "ZolikaPatch is outdated", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        ProcessStartInfo psi = new ProcessStartInfo
                                        {
                                            FileName = "cmd",
                                            Arguments = $"/c start {"https://zolika1351.pages.dev/mods/ivpatch"}",
                                            CreateNoWindow = true,
                                            UseShellExecute = false,
                                        };
                                        Process.Start(psi);
                                        MessageBox.Show("Press OK to restart the app after updating ZolikaPatch. Do not unpack 'PlayGTAIV.exe'.", "ZolikaPatch is outdated");
                                        System.Windows.Forms.Application.Restart();
                                        Environment.Exit(0);
                                    }
                                }
                                if (File.Exists($"{dialog.FileName}\\GFWLDLC.asi"))
                                {
                                    if (iniParsertt.ReadValue("Options", "LoadDLCs") == "0" && iniParsertt.ReadValue("Options", "LoadDLCs") != "N/A")
                                    {
                                        iniParsertt.EditValue("Options", "LoadDLCs", "1");
                                        File.Delete($"{dialog.FileName}\\GFWLDLC.asi");
                                    }
                                }

                                if (File.Exists($"{dialog.FileName}\\dinput8.dll") && !File.Exists($"{dialog.FileName}\\xlive.dll"))
                                {
                                    var result = MessageBox.Show("You appear to be using GFWL. Do you wish to remove Steam Achievements (if exists) and fix ZolikaPatch options to receive GFWL achievements?\n\nPressing 'No' can revert this if you agreed to this earlier.", "GFWL Achievements", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        if (File.Exists($"{dialog.FileName}\\SteamAchievements.asi"))
                                        {
                                            if (!Directory.Exists($"{dialog.FileName}\\backup"))
                                            {
                                                Directory.CreateDirectory($"{dialog.FileName}\\backup");
                                            }
                                            File.Move($"{dialog.FileName}\\SteamAchievements.asi", $"{dialog.FileName}\\backup\\SteamAchievements.asi");
                                        }
                                        if (iniParsertt.ReadValue("Options", "TryToSkipAllErrors") == "1")
                                        {
                                            iniParsertt.EditValue("Options", "TryToSkipAllErrors", "0");
                                        }
                                        if (iniParsertt.ReadValue("Options", "VSyncFix") == "1")
                                        {
                                            iniParsertt.EditValue("Options", "VSyncFix", "0");
                                        }
                                    }
                                    else
                                    {
                                        if (File.Exists($"{dialog.FileName}\\backup\\SteamAchievements.asi")) { File.Move($"{dialog.FileName}\\backup\\SteamAchievements.asi", $"{dialog.FileName}\\SteamAchievements.asi"); }
                                        if (iniParsertt.ReadValue("Options", "TryToSkipAllErrors") == "0")
                                        {
                                            iniParsertt.EditValue("Options", "TryToSkipAllErrors", "1");
                                        }
                                        if (iniParsertt.ReadValue("Options", "VSyncFix") == "0")
                                        {
                                            iniParsertt.EditValue("Options", "VSyncFix", "1");
                                        }
                                    }
                                }

                                foreach (string option in incompatibleOptions)
                                {
                                    if (iniParsertt.ReadValue("Options", option) == "1")
                                    {
                                        if (!optToChangeOptions)
                                        {
                                            var result = MessageBox.Show("Your ZolikaPatch options are incompatible with FusionFix. This may lead to crashes, inconsistencies, visual issues etc.\n\nDo you wish to fix the options?", "Fix ZolikaPatch - FusionFix compatibility?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                            if (result == MessageBoxResult.Yes)
                                            {
                                                optToChangeOptions = true;
                                                iniParsertt.EditValue("Options", option, "0");
                                                optionsChanged = true;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            iniParsertt.EditValue("Options", option, "0");
                                            optionsChanged = true;
                                        }
                                    }
                                }
                                if (optionsChanged) { iniParsertt.SaveFile(); }
                                break;

                        }

                        break;
                    }
                    else
                    {
                        Logger.Debug(" User selected the wrong folder. Displaying a MessageBox.");
                        MessageBox.Show("The selected folder does not contain a supported version of GTA IV.");
                    }
                }
                else
                {
                    break;
                }

            }
        }

        bool downloadfinished = false;
        bool extractfinished = false;

        private async Task InstallDXVK(string downloadUrl)
        {
            try
            {
                Logger.Debug(" Downloading the .tar.gz...");
                Thread thread = new Thread(() =>
                {
                    Logger.Debug(" Downloading the selected release...");
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadFileAsync(new Uri(downloadUrl), "./dxvk.tar.gz");
                });
                thread.Start();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Error downloading DXVK");
                throw;
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                int percentageInt = Convert.ToInt16(percentage);
                installdxvkbtn.Content = $"Downloading... ({percentageInt}%)";
            });
        }
        private async Task ExtractDXVK(string installationDir, List<string> dxvkConf)
        {

            Logger.Debug(" Extracting the d3d9.dll from the archive...");
            using (FileStream fsIn = new FileStream("./dxvk.tar.gz", FileMode.Open))
            using (GZipInputStream gzipStream = new GZipInputStream(fsIn))
            using (TarInputStream tarStream = new TarInputStream(gzipStream))
            {
                Logger.Debug("3");
                TarEntry entry;
                while ((entry = tarStream.GetNextEntry()) != null)
                {
                    Logger.Debug(entry.Name);
                    if (entry.Name.EndsWith("x32/d3d9.dll"))
                    {
                        using (FileStream fsOut = File.Create(System.IO.Path.Combine(installationDir, "d3d9.dll")))
                        {
                            tarStream.CopyEntryContents(fsOut);
                            Logger.Debug(" d3d9.dll extracted into the game folder.");
                        }
                        break;
                    }
                }
            }

            Logger.Debug(" Deleting the .tar.gz...");
            File.Delete("dxvk.tar.gz");

            Logger.Debug(" Writing the dxvk.conf...");
            using (StreamWriter confWriter = File.CreateText(System.IO.Path.Combine(installationDir, "dxvk.conf")))
            {
                foreach (string option in dxvkConf)
                {
                    confWriter.WriteLine(option);
                }
            }
            Logger.Debug(" dxvk.conf successfully written to game folder.");
            extractfinished = true;
        }
        private async Task downloaddxvk(string link, List<string> dxvkconf, bool gitlab, bool githubalt)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
            var firstResponse = await httpClient.GetAsync(link);
            firstResponse.EnsureSuccessStatusCode();
            var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(firstResponseBody).RootElement;
            string downloadUrl = null;
            switch (gitlab, githubalt)
            {
                case (false, false):
                    {
                        downloadUrl = parsed.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        break;
                    }
                case (true, false):
                    {
                        downloadUrl = parsed[0].GetProperty("assets").GetProperty("links").EnumerateArray().First(link => link.GetProperty("name").GetString().Contains("tar.gz")).GetProperty("url").GetString();
                        break;
                    }
                case (false, true):
                    {
                        downloadUrl = parsed.GetProperty("browser_download_url").GetString();
                        break;
                    }

            }
            InstallDXVK(downloadUrl!);
            while (!downloadfinished)
            {
                await Task.Delay(500);
            }
            downloadfinished = false;
            ExtractDXVK(gamedirectory.Text, dxvkconf);
        }
            private async void installdxvkbtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User clicked on the Install DXVK button.");
            dxvkPanel.IsEnabled = false;
            installdxvkbtn.Content = "Installing...";
            if (isIVSDKInstalled && !string.IsNullOrEmpty(rtssconfig))
            {
                IniEditor rtssGTAIVConfig = new IniEditor(rtssconfig);
                if (rtssGTAIVConfig.ReadValue("Hooking", "EnableHooking") == "1")
                {
                    Logger.Info(" User has IVSDK .NET installed and RTSS enabled at the same time and wants to install DXVK. Showing the warning prompt.");
                    MessageBox.Show($"You currently have RivaTuner Statistics Server enabled (it might be a part of MSI Afterburner).\n\nTo avoid issues with the game launching when DXVK and IVSDK .NET are both installed, go into your tray icons, press on the little monitor icon with the number 60, press 'Add' in bottom left, select GTA IV's executable and set Application detection level to 'None'.\n\nIf you want the statistics, set it to Low and restart the tool, with the game running.");
                }
            }
            int dgpu_dxvk_support = resultvk.Item1;
            int igpu_dxvk_support = resultvk.Item2;
            bool igpuonly = resultvk.Item3;
            bool dgpuonly = resultvk.Item4;
            bool inteligpu = resultvk.Item5;

            if (igpuonly && !dgpuonly)
            {
                switch (igpu_dxvk_support)
                {
                    case 1:
                        Logger.Debug(" User's PC only has an iGPU. Setting Install DXVK to 1.");
                        installdxvk = 1;
                        break;
                    case 2:
                        Logger.Debug(" User's PC only has an iGPU. Setting Install DXVK to 2.");
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && dgpuonly)
            {
                switch (dgpu_dxvk_support)
                {
                    case 1:
                        Logger.Debug(" User's PC only has a dGPU. Setting Install DXVK to 1.");
                        installdxvk = 1;
                        break;
                    case 2:
                        Logger.Debug(" User's PC only has a dGPU. Setting Install DXVK to 2.");
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && !dgpuonly)
            {
                Logger.Debug(" User's PC has both an iGPU and a dGPU. Doing further checks...");
                switch ((dgpu_dxvk_support, igpu_dxvk_support))
                {
                    case (0, 1):
                    case (0, 2):
                        Logger.Debug(" User PC's iGPU supports DXVK, but their dGPU does not - asking them what to do...");
                        var result = MessageBox.Show("Your iGPU supports DXVK but your GPU doesn't - do you still wish to install?", "Install DXVK?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            Logger.Debug(" User chose to install DXVK for the iGPU");
                            dxvkonigpu = true;
                            switch (igpu_dxvk_support)
                            {
                                case 1:
                                    Logger.Debug(" Setting Install DXVK to 1.");
                                    installdxvk = 1;
                                    break;
                                case 2:
                                    Logger.Debug(" Setting Install DXVK to 2.");
                                    installdxvk = 2;
                                    break;
                            }
                        }
                        else
                        {
                            Logger.Debug(" User chose not to install DXVK.");
                        }
                        break;
                    case (1, 2):
                        Logger.Debug(" User PC's iGPU supports DXVK, but their dGPU supports an inferior version - asking them what to do...");
                        var resultVer = MessageBox.Show("Your iGPU supports a greater version of DXVK than your GPU - which version do you wish to install?\n\nPress 'Yes' to install the version matching your GPU.\n\nPress 'No' to install the version matching your iGPU instead.", "Which DXVK version to install?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (resultVer == MessageBoxResult.Yes)
                        {
                            Logger.Debug(" User chose to install DXVK for the dGPU. Setting Install DXVK to 1.");
                            installdxvk = 1;
                        }
                        else
                        {
                            Logger.Debug(" User chose to install DXVK for the iGPU. Setting Install DXVK to 2.");
                            dxvkonigpu = true;
                            installdxvk = 2;
                        }
                        break;
                    case (2, 2):
                    case (1, 1):
                    case (2, 1):
                    case (2, 0):
                        Logger.Debug(" User's GPU supports the same or a better version of DXVK as the iGPU.");
                        switch (dgpu_dxvk_support)
                        {
                            case 1:
                                Logger.Debug(" Setting Install DXVK to 1.");
                                installdxvk = 1;
                                break;
                            case 2:
                                Logger.Debug(" Setting Install DXVK to 2.");
                                installdxvk = 2;
                                break;
                        }
                        break;
                }
            }

            if (inteligpu && igpuonly)
            {
                Logger.Debug(" User's PC only has an Intel iGPU. Prompting them to install DXVK 1.10.1.");
                MessageBoxResult result = MessageBox.Show("Your PC only has an Intel iGPU on it. While it does support more modern versions on paper, it's reported that DXVK 1.10.1 might be your only supported version. Do you wish to install it?\n\nIf 'No' is selected, DXVK will be installed following the normal procedure.", "Message", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    Logger.Debug(" Setting Install DXVK to 3 - a special case to install 1.10.1 for Intel iGPU's.");
                    installdxvk = 3;
                }
            }

            List<string> dxvkconf = new List<string> { };

            Logger.Debug(" Setting up dxvk.conf in accordance with user's choices.");

            if (vsynccheckbox.IsChecked == true)
            {
                Logger.Debug(" Adding d3d9.presentInterval = 1 and d3d9.numBackBuffers = 3");
                dxvkconf.Add("d3d9.presentInterval = 1");
                dxvkconf.Add("d3d9.numBackBuffers = 3");
            }
            if (framelatencycheckbox.IsChecked == true)
            {
                Logger.Debug(" Adding d3d9.maxFrameLatency = 1");
                dxvkconf.Add("d3d9.maxFrameLatency = 1");
            }

            Logger.Debug(" Quering links to install DXVK...");

            switch (installdxvk)
            {
                case 1:
                    /// we're using the "if" in each case because of the async checkbox
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.3...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        downloaddxvk("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/73567231", dxvkconf, false, true);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"DXVK-async 1.10.3 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" DXVK-async 1.10.3 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.3...");
                        downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/73461736", dxvkconf, false, true);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"DXVK 1.10.3 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" DXVK 1.10.3 has been installed!");
                    }
                    break;
                case 2:
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing Latest DXVK-gplasync...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        dxvkconf.Add("dxvk.gplAsyncCache = true");
                        downloaddxvk("https://gitlab.com/api/v4/projects/43488626/releases/", dxvkconf, true, false);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"Latest DXVK-gplasync has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK-gplasync has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing Latest DXVK...");
                        downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/latest", dxvkconf, false, false);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"Latest DXVK has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" Latest DXVK has been installed!");
                    }
                    break;
                case 3:
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.1...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        downloaddxvk("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/60677007", dxvkconf, false, true);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"DXVK-async 1.10.1 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.");
                        Logger.Info(" DXVK-async 1.10.1 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.1...");
                        downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/60669426", dxvkconf, false, true);
                        while (!extractfinished)
                        {
                            await Task.Delay(500);
                        }
                        extractfinished = false;
                        MessageBox.Show($"DXVK 1.10.1 has been installed!\n\nConsider going to Steam - Settings - Downloads and disable `Enable Shader Pre-caching` - this may improve your performance.", "Information");
                        Logger.Info(" DXVK 1.10.1 has been installed!");
                    }
                    break;
            }
            Logger.Debug(" DXVK installed, editing the launch options toggles and enabling the panels back...");
            installdxvkbtn.Content = "Reinstall DXVK";
            dxvkPanel.IsEnabled = true;
            windowedcheck.IsChecked = true;
            vidmemcheck.IsChecked = true;
            monitordetailcheck.IsChecked = true;

        }
        private void setuplaunchoptions_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User clicked on Setup Launch Options, checking the toggles...");
            List<string> launchoptions = new List<string> { };
            if (norestrictionscheck.IsChecked == true) { launchoptions.Add("-norestrictions"); Logger.Debug(" Added -norestrictions."); }
            if (nomemrestrictcheck.IsChecked == true) { launchoptions.Add("-nomemrestrict"); Logger.Debug(" Added -nomemrestrict."); }
            if (windowedcheck.IsEnabled)
            {
                IniEditor iniParser = new IniEditor(iniModify);
                bool borderlessWindowedValue;
                bool ffWindowed = true;
                bool ffBorderless = true;
                bool ffFocusLossless = true;
                if (ffix)
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
                if (windowedcheck.IsChecked == true)
                {
                    Logger.Debug(" User chose to enable Borderless Windowed");
                    if (borderlessWindowedValue == false)
                    {
                        Logger.Debug(" Borderless Windowed is disabled in the ini, enabling it back...");
                        if (ffix)
                        {
                            if (!ffWindowed) { iniParser.EditValue("MAIN", "Windowed", "1"); }
                            if (!ffBorderless) { iniParser.EditValue("MAIN", "BorderlessWindowed", "1"); }
                            if (!ffFocusLossless) { iniParser.EditValue("MAIN", "BlockOnLostFocus", "0"); }
                            Logger.Debug(" Enabled Borderless Windowed and disabled Pause Game on Focus Loss.");
                        }
                        else
                        {
                            launchoptions.Add("-windowed");
                            launchoptions.Add("-noBlockOnLostFocus");
                            iniParser.EditValue("Options", "BorderlessWindowed", "1");
                            Logger.Debug(" Added -windowed and -noBlockOnLostFocus.");
                        }
                        iniParser.SaveFile();
                    }
                }
                else if (windowedcheck.IsChecked == false && (borderlessWindowedValue == true || ffWindowed || ffBorderless || ffFocusLossless))
                {
                    Logger.Debug(" User chose to disable Borderless Windowed but it's enabled in the ini, disabling it...");
                    if (ffix)
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
                }
            }
            if (vidmemcheck.IsChecked == true)
            {
                Logger.Debug(" -availablevidmem checked, quering user's VRAM...");
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    foreach (var tempvram in from ManagementObject obj in searcher.Get()
                                             let adapterRAM = obj["AdapterRAM"] != null ? obj["AdapterRAM"].ToString() : "N/A"
                                             let tempvram = System.Convert.ToInt16(ByteSize.FromBytes(System.Convert.ToDouble(adapterRAM)).MebiBytes + 1)
                                             select tempvram)
                    {
                        if (firstgpu)
                        {
                            vram1 = tempvram;
                            firstgpu = false;
                        }
                        else if (tempvram > vram1 || tempvram > vram2)
                        {
                            vram2 = tempvram;
                        }
                    }

                    if (resultvk.Item3 || resultvk.Item4)
                    {
                        if (gb3check.IsChecked == true)
                        {
                            if (vram1 > 3072) vram1 = 3072;
                        }
                        else
                        {
                            if (vram1 > 4096) vram1 = 4096;
                        }
                        launchoptions.Add($"-availablevidmem {vram1}");
                        Logger.Debug($" Added -availablevidmem {vram1}.");
                    }
                    else
                    {
                        int vram;
                        if (!dxvkonigpu)
                        {
                            vram = Math.Max(vram1, vram2);
                        }
                        else
                        {
                            vram = Math.Min(vram1, vram2);
                        }
                        if (gb3check.IsChecked == true)
                        {
                            if (vram > 3072) vram = 3072;
                        }
                        else
                        {
                            if (vram > 4096) vram = 4096;
                        }
                        launchoptions.Add($"-availablevidmem {vram}");
                        Logger.Debug($" Added -availablevidmem {vram}.");
                    }
                }
                catch (Exception ex)
                {
                   // i know this is an awful and unoptimized and full of bad practices implementation, plz forgib
                   Logger.Error(ex, "Had some weird error during quering vram; asking the user for manual input");
                   bool noerror = false;
                   int vram = 0;
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
                   if (gb3check.IsChecked == true)
                   {
                        if (vram > 3072) vram = 3072;
                   }
                   else
                   {
                        if (vram > 4096) vram = 4096;
                   }
                   launchoptions.Add($"-availablevidmem {vram}");
                   Logger.Debug($" Added -availablevidmem {vram}.");
                }
            }
            if (monitordetailcheck.IsChecked == true)
            {
                Logger.Debug(" Monitor Details checked, quering user's monitor details...");
                int width, height, refreshRate;
                DisplayInfo.GetPrimaryDisplayInfo(out width, out height, out refreshRate);
                launchoptions.Add($"-width {width}");
                launchoptions.Add($"-height {height}");
                launchoptions.Add($"-refreshrate {refreshRate}");
                Logger.Debug($" Added -width {width}, -height {height}, -refreshrate {refreshRate}.");
            }
            if (!File.Exists($"{gamedirectory.Text}\\d3d9.dll"))
            {
                launchoptions.Add("-managed");
            }
            if (isretail)
            {
                Logger.Debug(" Game .exe is retail - inputting values via commandline.txt...");
                if (File.Exists($"{gamedirectory.Text}\\commandline.txt"))
                {
                    Logger.Debug(" Old commandline.txt detected, removing...");
                    File.Delete($"{gamedirectory.Text}\\commandline.txt");
                }
                Logger.Debug(" Writing new commandline.txt...");
                using (StreamWriter writer = new StreamWriter($"{gamedirectory.Text}\\commandline.txt"))
                {
                    foreach (string line in launchoptions)
                    {
                        writer.WriteLine(line);
                    }
                }
                Logger.Info($" Following launch options have been set to commandline.txt: {string.Join(" ", launchoptions)}");
                MessageBox.Show($"Following launch options have been set up automatically for you: \n\n{string.Join(" ", launchoptions)}\n\nDo not worry that VRAM value isn't your full value - that is intentional and you can change that if you need to.");
            }
            else
            {
                Logger.Info($" Game .exe is 1.2 or later - asked user to input the values on their own and copied them to clipboard: {string.Join(" ", launchoptions)}");
                MessageBox.Show($" The app can't set the launch options automatically, paste them in Steam's Launch Options manually (will be copied to clipboard after you press Ok):\n\n{string.Join(" ", launchoptions)}\n\nDo not worry that VRAM value isn't your full value - that is intentional and you can change that if you need to.");
                try { Clipboard.SetText(string.Join(" ", launchoptions)); } catch (Exception ex) { MessageBox.Show($" The app couldn't copy the options to clipboard - input them manually:\n\n{string.Join(" ", launchoptions)}"); Logger.Debug(ex, " Weird issues with clipboard access."); }
            }

        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Logger.Debug(" User clicked on a hyperlink from the main window.");
            ProcessStartInfo psi = new ProcessStartInfo
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
