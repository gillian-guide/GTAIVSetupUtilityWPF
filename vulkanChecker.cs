using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you
// this code accounts for ALL gpu's in the system and tries to work out the best conditions for installing DXVK
// so please don't strip the functionality
namespace GTAIVSetupUtilityWPF
{
    public class VulkanChecker
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static double ConvertApiVersion(uint apiversion)
        {
            uint major = apiversion >> 22;
            uint minor = (apiversion >> 12) & 0x3ff;
            return double.Parse($"{major},{minor}");
        }

        public static (int, int, bool, bool, bool, bool) VulkanCheck()
        {
            int i = 0;
            int vkDgpuDxvkSupport = 0;
            int vkIgpuDxvkSupport = 0;
            bool igpuOnly = true;
            bool dgpuOnly = true;
            bool intelIgpu = false;
            bool nvidiaGpu = false;

            while (true)
            {
                Logger.Debug($" Running vulkaninfo on GPU{i}... If this infinitely loops, your GPU is weird!");
                Process process = new Process();
                process.StartInfo.FileName = "vulkaninfo";
                process.StartInfo.Arguments = $"--json={i} --output data{i}.json";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();
                if (process.ExitCode != 0 && output.Contains("The selected gpu"))
                {
                    Logger.Debug($" GPU{i} doesn't exist, moving on");
                    break;
                }
                else if (process.ExitCode != 0)
                {
                    MessageBox.Show("The vulkaninfo check failed. This usually means your GPU does not support Vulkan. Make sure your drivers are up-to-date. DXVK is not available.");
                    Logger.Error($" Running vulkaninfo on GPU{i} failed! User likely has outdated drivers or an extremely old GPU.");
                    return (0, 0, false, false, false, false);
                }

                i++;
            }

            Logger.Debug($" Analyzing the vulkaninfo for every .json generated...");
            for (int x = 0; x < i; x++)
            {
                Logger.Debug($" Checking data{x}.json...");
                if (File.Exists($"data{x}.json"))
                {
                    using (StreamReader file = File.OpenText($"data{x}.json"))
                    {

                        int dxvkSupport = 0;
                        using (JsonDocument doc = JsonDocument.Parse(file.ReadToEnd()))
                        {
                            JsonElement root = doc.RootElement;
                            if (root.TryGetProperty("capabilities", out JsonElement h))
                            {
                                JsonElement capabilities = h.GetProperty("device");
                                string deviceName = capabilities.GetProperty("properties").GetProperty("VkPhysicalDeviceProperties").GetProperty("deviceName").GetString();
                                uint apiVersion = capabilities.GetProperty("properties").GetProperty("VkPhysicalDeviceProperties").GetProperty("apiVersion").GetUInt32();
                                double vulkanVer = ConvertApiVersion(apiVersion);

                                Logger.Info($"{deviceName}'s supported Vulkan version is: {vulkanVer}");
                                if (deviceName.Contains("NVIDIA"))
                                {
                                    Logger.Info($" GPU{x} is an NVIDIA GPU.");
                                    nvidiaGpu = true;
                                }
                                try
                                {
                                    Logger.Debug($" Checking if GPU{x} supports DXVK 2.x...");
                                    if (capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_robustness2", out _)
                                        && capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_transform_feedback", out _)
                                        && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT").GetProperty("robustBufferAccess2").GetBoolean()
                                        && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT").GetProperty("nullDescriptor").GetBoolean())
                                    {
                                        Logger.Info($" GPU{x} supports DXVK 2.x, yay!");
                                        dxvkSupport = 2;
                                    }
                                    else
                                    {
                                        Logger.Debug($" GPU{x} doesn't support DXVK 2.x, throwing an exception because doing it any other way is annoying...");
                                        throw new System.Exception();
                                    }
                                }
                                catch
                                {
                                    Logger.Debug($" Catched an exception, this means GPU{x} doesn't support DXVK 2.x, checking other versions...");
                                    if (vulkanVer < 1.1)
                                    {
                                        Logger.Info($" GPU{x} doesn't support DXVK or has  outdated drivers.");
                                    }
                                    else if (vulkanVer > 1.1 && vulkanVer < 1.3)
                                    {
                                        Logger.Info($" GPU{x} supports Legacy DXVK 1.x.");
                                        dxvkSupport = 1;
                                    }
                                }

                                if (capabilities.GetProperty("properties").GetProperty("VkPhysicalDeviceProperties").GetProperty("deviceType").GetString() == "VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU" && dxvkSupport > vkDgpuDxvkSupport)
                                {
                                    Logger.Info($" GPU{x} is a discrete GPU.");
                                    vkDgpuDxvkSupport = dxvkSupport;
                                    igpuOnly = false;
                                }
                                else if (dxvkSupport > vkIgpuDxvkSupport)
                                {
                                    Logger.Info($" GPU{x} is an integrated GPU.");
                                    vkIgpuDxvkSupport = dxvkSupport;
                                    dgpuOnly = false;
                                    if (deviceName.Contains("Intel"))
                                    {
                                        Logger.Info($" GPU{x} is an integrated Intel iGPU.");
                                        intelIgpu = true;
                                    }
                                }
                            }

                            else if (root.TryGetProperty("VkPhysicalDeviceProperties", out JsonElement n))
                            {
                                Logger.Debug($" Couldn't check the json normally, user likely has an Intel iGPU. Performing alternative check...");
                                JsonElement deviceName = n.GetProperty("deviceName");
                                JsonElement vulkanVer = root.GetProperty("comments").GetProperty("vulkanApiVersion");

                                Logger.Info($"{deviceName}'s supported Vulkan version is: {vulkanVer}");
                                if (deviceName.ToString().Contains("HD Graphics"))
                                {
                                    Logger.Info($" GPU{x} is an integrated Intel iGPU.");
                                    dgpuOnly = false;
                                    intelIgpu = true;
                                }
                                if (System.Convert.ToInt16(vulkanVer.ToString().Split('.')[0]) >= 1 && System.Convert.ToInt16(vulkanVer.ToString().Split('.')[1]) >= 1)
                                {
                                    Logger.Info($" GPU{x} supports Legacy DXVK 1.x.");
                                    vkIgpuDxvkSupport = 1;
                                }
                            }
                            else
                            {
                                Logger.Error($" Failed to read data{x}.json. Setting default values assuming the user has an Intel iGPU.");
                                MessageBox.Show("Failed to read the json. Make sure your drivers are up-to-date - don't rely on Windows Update drivers, either.\n\nThe app will proceed assuming you have an Intel iGPU with outdated drivers, but that may not be the case.");
                                igpuOnly = true;
                                dgpuOnly = false;
                                intelIgpu = true;
                                vkIgpuDxvkSupport = 1;
                            }
                        }
                    }
                    Logger.Debug($" Removing data{x}.json...");
                    File.Delete($"data{x}.json");
                }
                else { break; }
            }

            return (vkDgpuDxvkSupport, vkIgpuDxvkSupport, igpuOnly, dgpuOnly, intelIgpu, nvidiaGpu);
        }
    }
}
