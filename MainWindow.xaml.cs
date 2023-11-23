using ByteSizeLib;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVSetupUtilityWPF
{
    public partial class MainWindow : Window
    {

        (int, int, bool, bool, bool, bool) resultvk;
        int installdxvk = 0;
        bool dxvkonigpu;
        int vram1;
        int vram2;
        string iniModify;
        string ziniModify;
        bool ffix;
        bool isretail;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            if ( File.Exists("GTAIVSetupUtilityLogOld.txt")) { File.Delete("GTAIVSetupUtilityLogOld.txt"); }
            if ( File.Exists("GTAIVSetupUtilityLog.txt")) { File.Move("GTAIVSetupUtilityLog.txt", "GTAIVSetupUtilityLogOld.txt"); }
            NLog.LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "GTAIVSetupUtilityLog.txt");
            });
            Logger.Info(" Initializing the main window...");
            InitializeComponent();
            Logger.Info(" Main window initialized!");
            VKCheck();
        }

        async private void VKCheck()
        {
            Logger.Info(" Initializing the vulkan check...");
            resultvk = vulkanChecker.VulkanCheck();
            Logger.Info(" Vulkan check finished!");
            if (resultvk.Item6 && resultvk.Item1 == 2) { asynccheckbox.IsChecked = false; Logger.Debug($" User has an NVIDIA GPU, untoggling the async checkbox..."); }
            Overlay.Visibility = Visibility.Collapsed;
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
        private void managed_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User toggled -managed or -nomemrestrict.");
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("-managed may improve performance when using DXVK, but it's not compatible with -nomemrestrict. It's recommended to choose -nomemrestrict that allows the game to use all the memory resources up to it's limits.");
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
            if (tipscheck.IsChecked == true)
            {
                Logger.Debug(" Displaying a tip...");
                MessageBox.Show("This option forces a specific value of video memory due to the game not being able to do so automatically sometimes. Due to weird issues that occur from the game using more than 3GB, the toggle only sets the value to 3GB. You can manually change the lock to 4GB or higher, but there's little gain to doing that. It's recommended to keep this at default.");
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
                    if (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0") || (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1.2")))
                    {
                        if (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0")) { isretail = true; Logger.Debug(" Folder contains a retail exe."); }
                        else { isretail = false; Logger.Debug(" Folder contains an exe of Steam Version."); }
                        if (isretail == true && !checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0, 8"))
                        { vidmemcheck.IsEnabled = false; Logger.Debug(" Folder contains an exe of some pre-1.0.8.0 version. Disabling the -availablevidmem toggle."); }

                        directorytxt.Text = "Game Directory:";
                        gamedirectory.Text = dialog.FileName;
                        launchoptionsPanel.IsEnabled = true;
                        if (resultvk.Item1 == 0 && resultvk.Item2 == 0)
                        { dxvkPanel.IsEnabled = false; Logger.Debug(" DXVK is not supported - disabling the DXVK panel."); }
                        else
                        {
                            if (File.Exists($"{dialog.FileName}\\d3d9.dll"))
                            {
                                Logger.Debug(" Detected d3d9.dll - likely DXVK is already installed.");
                                installdxvkbtn.Content = "Reinstall DXVK";
                            }
                            else
                            {
                                Logger.Debug(" DXVK is not installed, changing the button name incase the user changed directories before.");
                                installdxvkbtn.Content = "Install DXVK";
                            }
                            Logger.Debug(" Enabled the DXVK panel.");
                            dxvkPanel.IsEnabled = true;
                        }
                        bool fusionFixPresent = File.Exists($"{dialog.FileName}\\GTAIV.EFLC.FusionFix.asi") || File.Exists($"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.asi");
                        bool zolikaPatchPresent = File.Exists($"{dialog.FileName}\\ZolikaPatch.asi") || File.Exists($"{dialog.FileName}\\plugins\\ZolikaPatch.asi");
                        switch (fusionFixPresent, zolikaPatchPresent)
                        {
                            case (false, false):
                                Logger.Debug(" User doesn't have neither ZolikaPatch or FusionFix. Disabling the Borderless Windowed toggle.");
                                windowedcheck.IsEnabled = false;
                                break;
                            case (true, false): case (false, true):
                                string iniFilePath = fusionFixPresent
                                ? (File.Exists($"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini") ? $"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini" : $"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.ini")
                                : (File.Exists($"{dialog.FileName}\\ZolikaPatch.ini") ? $"{dialog.FileName}\\ZolikaPatch.ini" : $"{dialog.FileName}\\plugins\\ZolikaPatch.ini");
                                if (!string.IsNullOrEmpty(iniFilePath))
                                {
                                    ffix = fusionFixPresent;
                                    Logger.Debug(fusionFixPresent ? " User has FusionFix." : " User has ZolikaPatch.");
                                    iniModify = iniFilePath;
                                }
                                break;
                            case (true, true):
                                string fusionFixIniFilePath = File.Exists($"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini")
                                    ? $"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini"
                                    : $"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.ini";
                                string zolikaPatchIniFilePath = File.Exists($"{dialog.FileName}\\ZolikaPatch.ini")
                                    ? $"{dialog.FileName}\\ZolikaPatch.ini"
                                    : $"{dialog.FileName}\\plugins\\ZolikaPatch.ini";
                                Logger.Debug(" User has FusionFix and ZolikaPatch. Disabling unnecessary ZolikaPatch options...");
                                iniModify = fusionFixIniFilePath;
                                ziniModify = zolikaPatchIniFilePath;
                                ffix = true;
                                IniParser iniParser = new IniParser(ziniModify);
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

        private async void installdxvkbtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug(" User clicked on the Install DXVK button.");
            dxvkPanel.IsEnabled = false;
            installdxvkbtn.Content = "Installing...";
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

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

            switch (installdxvk)
            {
                case 1:
                    /// we're using the "if" in each case because of the async checkbox
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.3...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/73567231");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK-async 1.10.3 has been installed!");
                        Logger.Info(" DXVK-async 1.10.3 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.3...");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/assets/73461736");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
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
                        var firstResponse = await httpClient.GetAsync("https://gitlab.com/api/v4/projects/43488626/releases/");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement[0].GetProperty("assets").GetProperty("links")[0].GetProperty("url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"Latest DXVK-gplasync has been installed!");
                        Logger.Info(" Latest DXVK-gplasync has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing Latest DXVK...");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/latest");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"Latest DXVK has been installed!");
                        Logger.Info(" Latest DXVK has been installed!");
                    }
                    break;
                case 3:
                    if (asynccheckbox.IsChecked == true)
                    {
                        Logger.Info(" Installing DXVK-async 1.10.1...");
                        dxvkconf.Add("dxvk.enableAsync = true");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/60677007");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK-async 1.10.1 has been installed!");
                        Logger.Info(" DXVK-async 1.10.1 has been installed!");
                    }
                    else
                    {
                        Logger.Info(" Installing DXVK 1.10.1...");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/assets/60669426");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
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
            else if (managedcheck.IsChecked == true) { launchoptions.Add("-managed"); Logger.Debug(" Added -managed."); }
            if (windowedcheck.IsEnabled)
            {
                Logger.Debug(" Setting up Borderless Windowed...");

                IniParser iniParser = new IniParser(iniModify);
                string borderlessWindowedValue;
                if (ffix == true)
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
                        if (ffix == true)
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
                    if (ffix == true)
                    {
                        iniParser.EditValue("MAIN", "BorderlessWindowed", "0");
                    }
                    else
                    {
                        iniParser.EditValue("Options", "BorderlessWindowed", "0");
                    }
                    iniParser.SaveFile();
                }

                /* Old parser, kept incase I'll need to revert it.
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(iniModify);
                if (zpatch)
                {
                    if (data["Options"]["BorderlessWindowed"] != "1")
                    {
                        data["Options"]["BorderlessWindowed"] = "1";
                        parser.WriteFile(iniModify, data);
                        Logger.Debug(" Set up Borderless Windowed for ZolikaPatch.");
                    }
                }
                else
                {
                    if (data["MAIN"]["BorderlessWindowed"] != "1")
                    {
                        data["MAIN"]["BorderlessWindowed"] = "1";
                        parser.WriteFile(iniModify, data);
                        Logger.Debug(" Set up Borderless Windowed for FusionFix.");
                    }
                }
                */
            }
            if (vidmemcheck.IsChecked == true)
            {
                Logger.Debug(" -availablevidmem checked, quering user's VRAM...");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string adapterRAM = obj["AdapterRAM"] != null ? obj["AdapterRAM"].ToString() : "N/A";
                    int tempvram = System.Convert.ToInt16(ByteSize.FromBytes(System.Convert.ToDouble(adapterRAM)).MebiBytes + 1);
                    bool h = false;
                    if (!h)
                    {
                        vram1 = tempvram;
                        h = true;
                    }
                    else if (tempvram > vram1 || tempvram > vram2)
                    {
                        vram2 = tempvram;
                    }
                }
                if (resultvk.Item3 || resultvk.Item4)
                {
                    if (vram1 > 3072) vram1 = 3072;
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
                    if (vram > 3072) vram = 3072;
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
            if (isretail == true)
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
                MessageBox.Show($"Following launch options have been set up automatically for you: \n\n{string.Join(" ", launchoptions)}\n\nDo not worry that the game only detects 3072MB of VRAM - that is intentional and you can change that if you need to.");
            }
            else
            {
                Logger.Info($" Game .exe is 1.2 or later - asked user to input the values on their own and copied them to clipboard: {string.Join(" ", launchoptions)}");
                MessageBox.Show($" The app can't set the launch options automatically, paste them in Steam's Launch Options manually (will be copied to clipboard after you press Ok):\n\n{string.Join(" ", launchoptions)}\n\nDo not worry that the game only detects 3072MB of VRAM - that is intentional and you can change that if you need to.");
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
