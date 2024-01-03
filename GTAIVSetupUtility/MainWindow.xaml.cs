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
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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
                    if (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0") || (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1.2")))
                    {
                        if (AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0")) { isretail = true; Logger.Debug(" Folder contains a retail exe."); }
                        else { isretail = false; Logger.Debug(" Folder contains an exe of Steam Version."); }
                        if (isretail && !AppVersionGrabber.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0, 8"))
                        { vidmemcheck.IsEnabled = false; gb3check.IsEnabled = false; gb4check.IsEnabled = false; Logger.Debug(" Folder contains an exe of some pre-1.0.8.0 version. Disabling the -availablevidmem toggle."); }

                        directorytxt.Text = "Game Directory:";
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
                                break;
                            case (false, true):
                                iniModify = zolikaPatchPath;
                                ffix = false;
                                Logger.Debug(" User has ZolikaPatch.");
                                break;
                            case (true, true):
                                Logger.Debug(" User has FusionFix and ZolikaPatch. Disabling unnecessary ZolikaPatch options...");
                                iniModify = fusionFixCfgPath;
                                ziniModify = zolikaPatchPath;
                                ffix = true;
                                IniEditor iniParser = new IniEditor(ziniModify);
                                iniParser.EditValue("Options", "BuildingAlphaFix", "0");
                                iniParser.EditValue("Options", "EmissiveLerpFix", "0");
                                iniParser.EditValue("Options", "BorderlessWindowed", "0");
                                iniParser.EditValue("Options", "CutsceneFixes", "0");
                                iniParser.EditValue("Options", "HighFPSBikePhysicsFix", "0");
                                iniParser.EditValue("Options", "OutOfCommissionFix", "0");
                                iniParser.EditValue("Options", "SkipIntro", "0");
                                iniParser.EditValue("Options", "SkipMenu", "0");
                                iniParser.SaveFile();
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
                    downloadUrl = parsed[0].GetProperty("assets").GetProperty("links")[0].GetProperty("url").GetString();
                    break;
                }
                case (false, true):
                {
                    downloadUrl = parsed.GetProperty("browser_download_url").GetString();
                    break;
                }

            }
            DXVKInstaller.InstallDXVK(downloadUrl!, gamedirectory.Text, dxvkconf);
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
                        await downloaddxvk("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/73567231", dxvkconf, false, true);
                        MessageBox.Show($"DXVK-async 1.10.3 has been installed!");
                        Logger.Info(" DXVK-async 1.10.3 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.3...");
                        await downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/73461736", dxvkconf, false, true);
                        MessageBox.Show($"DXVK 1.10.3 has been installed!");
                        Logger.Info(" DXVK 1.10.3 has been installed!");
                    }
                    break;
                case 2:
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing Latest DXVK-gplasync...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        dxvkconf.Add("dxvk.gplAsyncCache = true");
                        await downloaddxvk("https://gitlab.com/api/v4/projects/43488626/releases/", dxvkconf, true, false);
                        MessageBox.Show($"Latest DXVK-gplasync has been installed!");
                        Logger.Info(" Latest DXVK-gplasync has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing Latest DXVK...");
                        await downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/latest", dxvkconf, false, false);
                        MessageBox.Show($"Latest DXVK has been installed!");
                        Logger.Info(" Latest DXVK has been installed!");
                    }
                    break;
                case 3:
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.1...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        await downloaddxvk("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/60677007", dxvkconf, false, true);
                        MessageBox.Show($"DXVK-async 1.10.1 has been installed!");
                        Logger.Info(" DXVK-async 1.10.1 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.1...");
                        await downloaddxvk("https://api.github.com/repos/doitsujin/dxvk/releases/assets/60669426", dxvkconf, false, true);
                        MessageBox.Show($"DXVK 1.10.1 has been installed!", "Information");
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
                Logger.Debug(" Setting up Borderless Windowed...");

                IniEditor iniParser = new IniEditor(iniModify);
                string borderlessWindowedValue;
                if (ffix)
                {
                    borderlessWindowedValue = iniParser.ReadValue("MAIN", "BorderlessWindowed");
                }
                else
                {
                    borderlessWindowedValue = iniParser.ReadValue("Options", "BorderlessWindowed");
                }
                if (windowedcheck.IsChecked == true)
                {
                    Logger.Debug(" User chose to enable Borderless Windowed");
                    if (borderlessWindowedValue == "0")
                    {
                        Logger.Debug(" Borderless Windowed is disabled in the ini, enabling it back...");
                        if (ffix)
                        {
                            iniParser.EditValue("MAIN", "BorderlessWindowed", "1");
                        }
                        else
                        {
                            iniParser.EditValue("Options", "BorderlessWindowed", "1");
                        }
                        iniParser.SaveFile();
                    }
                    launchoptions.Add("-windowed");
                    Logger.Debug(" Added -windowed.");
                }
                else if (windowedcheck.IsChecked == false && borderlessWindowedValue == "1")
                {
                    Logger.Debug(" User chose to disable Borderless Windowed but it's enabled in the ini, disabling it...");
                    if (ffix)
                    {
                        iniParser.EditValue("MAIN", "BorderlessWindowed", "0");
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
                else if (!resultvk.Item3 && !resultvk.Item4)
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
