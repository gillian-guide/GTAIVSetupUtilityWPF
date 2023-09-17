using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GTAIVSetupUtilityWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        public (int,int,bool,bool,bool,bool) resultvk = VulkanChecker.VulkanCheck();

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }
        public MainWindow()
        {
            InitializeComponent();
            VersionTextBlock.Text = $"Version: {GetAssemblyVersion()}";
            DebugOutput6.Text = $"dGPU DXVK Support: {resultvk.Item1}";
            DebugOutput5.Text = $"iGPU DXVK Support: {resultvk.Item2}";
            DebugOutput4.Text = $"iGPU Only: {resultvk.Item3}";
            DebugOutput3.Text = $"dGPU Only: {resultvk.Item4}";
            DebugOutput2.Text = $"Intel iGPU: {resultvk.Item5}";
            DebugOutput.Text = $"NVIDIA GPU: {resultvk.Item6}";
            if (resultvk.Item6 && resultvk.Item1 == 2) {
                asynccheckbox.IsChecked = false;
            };
            if (resultvk.Item1 == 0 && resultvk.Item2 == 0)
            {
                asynccheckbox.IsEnabled = false;
                installdxvkbtn.IsEnabled = false;
            }
        }
        private void async_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DXVK with async should provide better performance for most, but under some conditions it may provide worse performance instead. Without async, you might stutter the first time you see different areas. It won't stutter the next time in the same area.\n\nNote, however, that performance on NVIDIA when using DXVK 2.0+ may be worse. Feel free to experiment by re-installing DXVK.");
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
                        gamedirectory.Text = dialog.FileName;
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

        private void installdxvkbtn_Click(object sender, RoutedEventArgs e)
        {
            int installdxvk = 0;
            int dgpu_dxvk_support = resultvk.Item1;
            int igpu_dxvk_support = resultvk.Item2;
            bool igpuonly = resultvk.Item3;
            bool dgpuonly = resultvk.Item4;

            if (igpuonly && !dgpuonly)
            {
                // User's PC only has an iGPU.
                switch (igpu_dxvk_support)
                {
                    case 0:
                        MessageBox.Show("Your PC only has an iGPU and it does not support DXVK.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    case 1:
                        MessageBox.Show("Your PC only has an iGPU but it supports Legacy DXVK. Installing...", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        installdxvk = 1;
                        break;
                    case 2:
                        MessageBox.Show("Your PC only has an iGPU but it supports Latest DXVK. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && dgpuonly)
            {
                // User's PC only has a GPU.
                switch (dgpu_dxvk_support)
                {
                    case 0:
                        MessageBox.Show("Your GPU does not support DXVK.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    case 1:
                        MessageBox.Show("Your GPU only supports Legacy DXVK. Installing...", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        installdxvk = 1;
                        break;
                    case 2:
                        MessageBox.Show("Your GPU supports Latest DXVK. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        installdxvk = 2;
                        break;
                }
            }
            else if (!igpuonly && !dgpuonly)
            {
                // User's PC has both a GPU and an iGPU. Doing further checks...
                switch ((dgpu_dxvk_support, igpu_dxvk_support))
                {
                    case (0, 0):
                        MessageBox.Show("None of the your GPUs support DXVK.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    case (0, 1):
                    case (0, 2):
                        var result = MessageBox.Show("Your iGPU supports DXVK but your GPU doesn't - do you still wish to install?", "Install DXVK", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            switch (igpu_dxvk_support)
                            {
                                case 1:
                                    MessageBox.Show("You chose to install the version matching your iGPU - Legacy. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                    installdxvk = 1;
                                    break;
                                case 2:
                                    MessageBox.Show("You chose to install the version matching their iGPU - Latest. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                    installdxvk = 2;
                                    break;
                            }
                        }
                        break;
                    case (1, 2):
                        var resultVer = MessageBox.Show("Your iGPU supports a greater version of DXVK than your GPU - which version do you wish to install? Press 'Version 1' to install the version matching your GPU or 'Version 2' to install the version matching your iGPU instead.", "Install DXVK Version", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (resultVer == MessageBoxResult.Yes)
                        {
                            MessageBox.Show("User chose to install the version matching their GPU - Legacy. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            installdxvk = 1;
                        }
                        else
                        {
                            MessageBox.Show("User chose to install the version matching their iGPU - Latest. Installing...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            installdxvk = 2;
                        }
                        break;
                    case (2, 2):
                    case (1, 1):
                    case (2, 1):
                    case (2, 0):
                        // User's GPU supports the same or a better version of DXVK as the iGPU.
                        switch (dgpu_dxvk_support)
                        {
                            case 1:
                                MessageBox.Show("Installing Legacy version...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                installdxvk = 1;
                                break;
                            case 2:
                                MessageBox.Show("Installing Latest version...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                installdxvk = 2;
                                break;
                        }
                        break;
                }
            }
            DebugOutput.Text = installdxvk.ToString();
        }
    }
}
