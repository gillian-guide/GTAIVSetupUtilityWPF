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
        static string ConvertApiVersion(uint apiversion)
        {
            uint major = apiversion >> 22;
            uint minor = (apiversion >> 12) & 0x3ff;
            return $"{major}.{minor}";
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
                        break;
                }

                i++;
            }

            for (int x = 0; x < i; x++)
            {
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
                                string vulkanVer = ConvertApiVersion(apiVersion);

                                Debug.WriteLine($"{deviceName}'s supported Vulkan version is: {vulkanVer}");
                                if (deviceName.Contains("NVIDIA"))
                                {
                                    nvidiaGpu = true;
                                }
                                try
                                {
                                    if (capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_robustness2", out _)
                                        && capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_transform_feedback", out _)
                                        && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT").GetProperty("robustBufferAccess2").GetBoolean()
                                        && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT").GetProperty("nullDescriptor").GetBoolean())
                                    {
                                        Debug.WriteLine("This GPU supports Latest DXVK.");
                                        dxvkSupport = 2;
                                    }
                                    else
                                    {
                                        throw new System.Exception();
                                    }
                                }
                                catch
                                {
                                    if (vulkanVer.CompareTo("1.1") < 0)
                                    {
                                        Debug.WriteLine("This GPU is not supported by DXVK.");
                                    }
                                    else if (vulkanVer.CompareTo("1.1") >= 0 && vulkanVer.CompareTo("1.3") < 0)
                                    {
                                        Debug.WriteLine("This GPU only supports Legacy DXVK.");
                                        dxvkSupport = 1;
                                    }
                                }

                                if (capabilities.GetProperty("properties").GetProperty("VkPhysicalDeviceProperties")
                                    .GetProperty("deviceType").GetString() == "VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU" && dxvkSupport > vkDgpuDxvkSupport)
                                {
                                    vkDgpuDxvkSupport = dxvkSupport;
                                    igpuOnly = false;
                                }
                                else if (dxvkSupport > vkIgpuDxvkSupport)
                                {
                                    vkIgpuDxvkSupport = dxvkSupport;
                                    dgpuOnly = false;
                                    if (deviceName.Contains("Intel"))
                                    {
                                        intelIgpu = true;
                                    }
                                }
                            }

                            else if (root.TryGetProperty("VkPhysicalDeviceProperties", out JsonElement n))
                            {
                                // special condition for weird ass gpu's, probably only applies to intel igpu's
                                JsonElement deviceName = n.GetProperty("deviceName");
                                JsonElement vulkanVer = root.GetProperty("comments").GetProperty("vulkanApiVersion");

                                Debug.WriteLine($"{deviceName}'s supported Vulkan version is: {vulkanVer}");
                                if (deviceName.ToString().Contains("HD Graphics"))
                                {
                                    dgpuOnly = false;
                                    intelIgpu = true;
                                }
                                if (System.Convert.ToInt16(vulkanVer.ToString().Split('.')[0]) >= 1 && System.Convert.ToInt16(vulkanVer.ToString().Split('.')[1]) >= 1)
                                {
                                    vkIgpuDxvkSupport = 1;
                                }
                            }
                        }
                    }
                    File.Delete($"data{x}.json");
                }
                else { break; }
            }

            return (vkDgpuDxvkSupport, vkIgpuDxvkSupport, igpuOnly, dgpuOnly, intelIgpu, nvidiaGpu);
        }
    }
}