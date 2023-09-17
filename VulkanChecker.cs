using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;


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

            try
            {
                while (true)
                {
                    if (i == 0)
                    {
                        Console.WriteLine($"Running vulkaninfo on GPU0...");
                    }
                    else
                    {
                        Console.WriteLine($"Attempting to run vulkaninfo on GPU{i} if it exists...");
                    }

                    Process process = new Process();
                    process.StartInfo.FileName = "vulkaninfo";
                    process.StartInfo.Arguments = $"--json={i} --output data{i}.json";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        if (output.Contains("The selected gpu"))
                        {
                            break;
                        }
                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            for (int x = 0; x < i; x++)
            {
                try
                {
                    using (StreamReader file = File.OpenText($"data{x}.json"))
                    {
                        int dxvkSupport = 0;
                        using (JsonDocument doc = JsonDocument.Parse(file.ReadToEnd()))
                        {
                            JsonElement root = doc.RootElement;
                            JsonElement capabilities = root.GetProperty("capabilities").GetProperty("device");
                            string deviceName = capabilities.GetProperty("properties")
                                .GetProperty("VkPhysicalDeviceProperties").GetProperty("deviceName").GetString();
                            uint apiVersion = capabilities.GetProperty("properties")
                                .GetProperty("VkPhysicalDeviceProperties").GetProperty("apiVersion").GetUInt32();
                            string vulkanVer = ConvertApiVersion(apiVersion);

                            Console.WriteLine($"{deviceName}'s supported Vulkan version is: {vulkanVer}");
                            if (deviceName.Contains("NVIDIA"))
                            {
                                nvidiaGpu = true;
                            }
                            try
                            {
                                if (capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_robustness2", out _)
                                    && capabilities.GetProperty("extensions").TryGetProperty("VK_EXT_transform_feedback", out _)
                                    && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT")
                                        .GetProperty("robustBufferAccess2").GetBoolean()
                                    && capabilities.GetProperty("features").GetProperty("VkPhysicalDeviceRobustness2FeaturesEXT")
                                        .GetProperty("nullDescriptor").GetBoolean())
                                {
                                    Console.WriteLine("This GPU supports Latest DXVK.");
                                    dxvkSupport = 2;
                                }
                            }
                            catch
                            {
                                if (vulkanVer.CompareTo("1.1") < 0)
                                {
                                    Console.WriteLine("This GPU is not supported by DXVK.");
                                }
                                else if (vulkanVer.CompareTo("1.1") >= 0 && vulkanVer.CompareTo("1.3") < 0)
                                {
                                    Console.WriteLine("This GPU only supports Legacy DXVK.");
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
                    }

                    File.Delete($"data{x}.json");
                }
                catch
                {
                    break;
                }
            }

            return (vkDgpuDxvkSupport, vkIgpuDxvkSupport, igpuOnly, dgpuOnly, intelIgpu, nvidiaGpu);
        }
    }
}