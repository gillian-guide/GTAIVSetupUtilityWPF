using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Windows.Input;
using System.Management;
using Microsoft.WindowsAPICodePack.Dialogs;
using ByteSizeLib;
using IniParser;
using IniParser.Model;
// hi here, i'm an awful coder, so please clean up for me if it really bothers you

namespace GTAIVSetupUtilityWPF
{
    public partial class MainWindow : Window
    {
        (int,int,bool,bool,bool,bool) resultvk = VulkanChecker.VulkanCheck();
        int installdxvk = 0;
        bool dxvkonigpu;
        int vram1;
        int vram2;
        string iniModify;
        bool is1080;
        bool zpatch;


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
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("DXVK with async should provide better performance for most, but under some conditions it may provide worse performance instead. Without async, you might stutter the first time you see different areas. It won't stutter the next time in the same area.\n\nNote, however, that performance on NVIDIA when using DXVK 2.0+ may be worse. Feel free to experiment by re-installing DXVK.");
            }
        }
        private void vsync_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("The in-game VSync implementation produces framepacing issues. DXVK's VSync implementation should be preferred.\n\nIt's recommended to keep this on and in-game's implementation off.");
            }

        }
        private void latency_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("This option may help avoiding further framepacing issues. It's recommended to keep this on.");
            }
        }
        private void norestrictions_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("This option allows you to set any in-game settings independently of what the game restricts you to. It's recommended to keep this on. ");
            }
        }
        private void managed_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("-managed may improve performance when using DXVK, but it's not compatible with -nomemrestrict. It's recommended to choose -nomemrestrict that allows the game to use all the memory resources up to it's limits.");
            }
        }
        private void windowed_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("This option allows to use Borderless Fullscreen instead of Exclusive Fullscreen. Provides better experience and sometimes better performance. It's recommended to keep this on.");
            }
        }
        private void vidmem_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("This option forces a specific value of video memory due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
            }
        }
        private void monitordetail_Click(object sender, RoutedEventArgs e)
        {
            if (tipscheck.IsChecked == true)
            {
                MessageBox.Show("This option forces a specific resolution and refresh rate due to the game not being able to do so automatically sometimes. It's recommended to keep this at default.");
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            if (resultvk.Item6 && resultvk.Item1 == 2)
            { asynccheckbox.IsChecked = false; }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
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
            while (true) {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\Grand Theft Auto IV\\GTAIV";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    if (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0") || (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1.2")))
                    {
                        if (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0, 8")){ is1080 = true; }
                        else { is1080 = false; }
                        if (checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0") && !checkVersion.GetFileVersion($"{dialog.FileName}\\GTAIV.exe").StartsWith("1, 0, 8"))
                        { vidmemcheck.IsEnabled = false; }

                        directorytxt.Text = "Game Directory:";
                        gamedirectory.Text = dialog.FileName;
                        launchoptionsPanel.IsEnabled = true;
                        if (resultvk.Item1 == 0 && resultvk.Item2 == 0)
                        { dxvkPanel.IsEnabled = false; }
                        else
                        {
                            if (File.Exists($"{dialog.FileName}\\d3d9.dll"))
                            {
                                installdxvkbtn.Content = "Reinstall DXVK";
                            }
                            dxvkPanel.IsEnabled = true;
                        }
                        if (!(File.Exists($"{dialog.FileName}\\ZolikaPatch.asi")) && (!(File.Exists($"{dialog.FileName}\\plugins\\ZolikaPatch.asi")) && !(File.Exists($"{dialog.FileName}\\GTAIV.EFLC.FusionFix.asi"))) && !(File.Exists($"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.asi")))
                        {
                            windowedcheck.IsEnabled = false;
                            windowedcheck.Content = "Borderless Windowed";
                        }
                        else
                        {
                            if (File.Exists($"{dialog.FileName}\\ZolikaPatch.ini"))
                            {
                                zpatch = true;
                                iniModify = $"{dialog.FileName}\\ZolikaPatch.ini";
                            }
                            else if (File.Exists($"{dialog.FileName}\\plugins\\ZolikaPatch.ini"))
                            {
                                zpatch = true;
                                iniModify = $"{dialog.FileName}\\plugins\\ZolikaPatch.ini";
                            }
                            else if (File.Exists($"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini"))
                            {
                                iniModify = $"{dialog.FileName}\\GTAIV.EFLC.FusionFix.ini";
                            }
                            else if (File.Exists($"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.ini"))
                            {
                                iniModify = $"{dialog.FileName}\\plugins\\GTAIV.EFLC.FusionFix.ini";
                            }
                        }

                        break;
                    }
                    else
                    {
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
            dxvkPanel.IsEnabled = false;
            installdxvkbtn.Content = "Installing...";
            int dgpu_dxvk_support = resultvk.Item1;
            int igpu_dxvk_support = resultvk.Item2;
            bool igpuonly = resultvk.Item3;
            bool dgpuonly = resultvk.Item4;
            bool inteligpu = resultvk.Item5;

            if (igpuonly && !dgpuonly)
            {
                // User's PC only has an iGPU.
                switch (igpu_dxvk_support)
                {
                    case 1:
                        installdxvk = 1;
                        break;
                    case 2:
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && dgpuonly)
            {
                // User's PC only has a GPU.
                switch (dgpu_dxvk_support)
                {
                    case 1:
                        installdxvk = 1;
                        break;
                    case 2:
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && !dgpuonly)
            {
                // User's PC has both a GPU and an iGPU. Doing further checks...
                switch ((dgpu_dxvk_support, igpu_dxvk_support))
                {
                    case (0, 1): case (0, 2):
                        var result = MessageBox.Show("Your iGPU supports DXVK but your GPU doesn't - do you still wish to install?", "Install DXVK?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            dxvkonigpu = true;
                            switch (igpu_dxvk_support)
                            {
                                case 1:
                                    installdxvk = 1;
                                    break;
                                case 2:
                                    installdxvk = 2;
                                    break;
                            }
                        }
                        break;
                    case (1, 2):
                        var resultVer = MessageBox.Show("Your iGPU supports a greater version of DXVK than your GPU - which version do you wish to install?\n\nPress 'Yes' to install the version matching your GPU.\n\nPress 'No' to install the version matching your iGPU instead.", "Which DXVK version to install?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (resultVer == MessageBoxResult.Yes)
                        {
                            installdxvk = 1;
                        }
                        else
                        {
                            dxvkonigpu = true;
                            installdxvk = 2;
                        }
                        break;
                    case (2, 2): case (1, 1): case (2, 1): case (2, 0):
                        // User's GPU supports the same or a better version of DXVK as the iGPU.
                        switch (dgpu_dxvk_support)
                        {
                            case 1:
                                installdxvk = 1;
                                break;
                            case 2:
                                installdxvk = 2;
                                break;
                        }
                        break;
                }
            }

            if (inteligpu && igpuonly)
            {
                MessageBoxResult result = MessageBox.Show("Your PC only has an Intel iGPU on it. While it does support more modern versions on paper, it's reported that DXVK 1.10.1 might be your only supported version. Do you wish to install it?\n\nIf 'No' is selected, DXVK will be installed following the normal procedure.", "Message", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    installdxvk = 3;
                }
            }

            List<string> dxvkconf = new List<string> { };

            if (vsynccheckbox.IsChecked == true)
            {
                dxvkconf.Add("d3d9.presentInterval = 1");
                dxvkconf.Add("d3d9.numBackBuffers = 3");
            }
            if (framelatencycheckbox.IsChecked == true)
            {
                dxvkconf.Add("d3d9.maxFrameLatency = 1");
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

            switch (installdxvk)
            {
                case 1:
                    /// we're using the "if" in each case because of the async checkbox
                    if (asynccheckbox.IsChecked == true)
                    {
                        dxvkconf.Add("dxvk.enableAsync = true");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/73567231");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK-async 1.10.3 has been installed!");
                    }
                    else
                    {
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/assets/73461736");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK 1.10.3 has been installed!");
                    }
                    break;
                case 2:
                    if (asynccheckbox.IsChecked == true)
                    {
                        dxvkconf.Add("dxvk.enableAsync = true");
                        dxvkconf.Add("dxvk.gplAsyncCache = true");
                        var firstResponse = await httpClient.GetAsync("https://gitlab.com/api/v4/projects/43488626/releases/");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement[0].GetProperty("assets").GetProperty("links")[0].GetProperty("url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"Latest DXVK-gplasync has been installed!");
                    }
                    else
                    {
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/latest");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"Latest DXVK has been installed!");
                    }
                    break;
                case 3:
                    if (asynccheckbox.IsChecked == true)
                    {
                        dxvkconf.Add("dxvk.enableAsync = true");
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/Sporif/dxvk-async/releases/assets/60677007");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK-async 1.10.1 has been installed!");
                    }
                    else
                    {
                        var firstResponse = await httpClient.GetAsync("https://api.github.com/repos/doitsujin/dxvk/releases/assets/60669426");
                        firstResponse.EnsureSuccessStatusCode();
                        var firstResponseBody = await firstResponse.Content.ReadAsStringAsync();
                        var downloadUrl = JsonDocument.Parse(firstResponseBody).RootElement.GetProperty("browser_download_url").GetString();
                        DXVKInstaller.InstallDXVK(downloadUrl, gamedirectory.Text, dxvkconf);
                        MessageBox.Show($"DXVK 1.10.1 has been installed!", "Information");
                    }
                    break;
            }
            installdxvkbtn.Content = "Reinstall DXVK";
            dxvkPanel.IsEnabled = true;
            windowedcheck.IsChecked = true;
            vidmemcheck.IsChecked = true;
            monitordetailcheck.IsChecked = true;

        }
        private void setuplaunchoptions_Click(object sender, RoutedEventArgs e)
        {
            List<string> launchoptions = new List<string> { };
            if (norestrictionscheck.IsChecked == true) { launchoptions.Add("-norestrictions"); }
            if (nomemrestrictcheck.IsChecked == true) { launchoptions.Add("-nomemrestrict"); }
            else if (managedcheck.IsChecked == true) { launchoptions.Add("-managed"); }
            if (windowedcheck.IsChecked == true && windowedcheck.IsEnabled)
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(iniModify);
                if (zpatch)
                {
                    if (data["Options"]["BorderlessWindowed"] != "1")
                    {
                        data["Options"]["BorderlessWindowed"] = "1";
                        parser.WriteFile(iniModify, data);
                    }
                }
                else
                {
                    if (data["MAIN"]["BorderlessWindowed"] != "1")
                    {
                        data["MAIN"]["BorderlessWindowed"] = "1";
                        parser.WriteFile(iniModify, data);
                    }
                }
                launchoptions.Add("-windowed");
            }
            if (vidmemcheck.IsChecked == true)
            {
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
                    launchoptions.Add($"-availablevidmem {vram1}");
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
                    launchoptions.Add($"-availablevidmem {vram}");
                }
            }
            if (monitordetailcheck.IsChecked == true)
            {
                int width, height, refreshRate;
                DisplayInfo.GetPrimaryDisplayInfo(out width, out height, out refreshRate);
                launchoptions.Add($"-width {width}");
                launchoptions.Add($"-height {height}");
                launchoptions.Add($"-refreshrate {refreshRate}");

                // old code incase the chatgpt:tm: code breaks and i have no better solutions

                /*
                Screen primaryScreen = Screen.PrimaryScreen;
                string deviceName = primaryScreen.DeviceName;
                launchoptions.Add($"-width {primaryScreen.Bounds.Width}");
                launchoptions.Add($"-height {primaryScreen.Bounds.Height}");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["CurrentRefreshRate"] != null)
                        {
                            launchoptions.Add($"-refreshrate {int.Parse(obj["CurrentRefreshRate"].ToString())}");
                        }
                    }
                }
                */
            }
            if (is1080)
            {
                if (File.Exists($"{gamedirectory.Text}\\commandline.txt"))
                {
                    File.Delete($"{gamedirectory.Text}\\commandline.txt");
                }
                using (StreamWriter writer = new StreamWriter($"{gamedirectory.Text}\\commandline.txt"))
                {
                    foreach (string line in launchoptions)
                    {
                        writer.WriteLine(line);
                    }
                }
                MessageBox.Show($"Following launch options have been set up automatically for you: \n\n{string.Join(" ", launchoptions)}");
            }
            else
            {
                MessageBox.Show($"The app can't set the launch options automatically, paste them in Steam's Launch Options manually (will be copied to clipboard after you press Ok):\n\n{string.Join(" ", launchoptions)}");
                Clipboard.SetText(string.Join(" ", launchoptions));
            }

        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            }
            catch
            {
                System.Diagnostics.Process.Start("cmd",$"/c start {e.Uri.AbsoluteUri}");
            }
        }
    }
}
