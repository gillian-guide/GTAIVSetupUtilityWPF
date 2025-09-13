using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Policy;
using System.Text.Json;
using System.Windows;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you (and like, this code is *really* stupid, sorry)
// this code accounts for ALL gpu's in the system and tries to work out the best conditions for installing DXVK
// so please don't strip the functionality
namespace GTAIVSetupUtilityWPF.Functions
{
    public static class VulkanChecker
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static (int, int) ConvertApiVersion(uint apiversion)
        {
            uint major = apiversion >> 22;
            uint minor = apiversion >> 12 & 0x3ff;
            return (Convert.ToInt32(major), Convert.ToInt32(minor));
        }
        public static (int, int, int, bool, bool, bool, bool) VulkanCheck()
        {
            int gpucount = 0;
            int gplSupport = 0;
            int vkDgpuDxvkSupport = 0;
            int vkIgpuDxvkSupport = 0;
            bool igpuOnly = true;
            bool dgpuOnly = true;
            bool intelIgpu = false;
            bool enableasync = false;
            bool atLeastOneGPUSucceededVulkanInfo = false;
            bool atLeastOneGPUSucceededJson = false;
            bool atLeastOneGPUFailed = false;
            bool atLeastOneGPUFailedGPL = false;
            bool atLeastOneGPUFailedFL = false;
            bool nvidia50series = false;
            bool maintenance4 = false;
            bool maintenance5 = false;
            List<int> listOfFailedGPUs = new List<int>();
            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_VideoController");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection videoControllers = searcher.Get();
                gpucount = videoControllers.Count;
            }
            catch (Exception ex)
            {
                Logger.Error($" Ran into error: ", ex);
                throw;
            }
            for (int i = 0; i < gpucount; i++)
            {
                try
                {
                    Logger.Debug($" Running vulkaninfo on GPU{i}... If this infinitely loops, your GPU is weird!");
                    using var process = new Process();
                    process.StartInfo.FileName = "vulkaninfo";
                    process.StartInfo.Arguments = $"--json={i} --output data{i}.json";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();

                    if (!process.WaitForExit(10))
                    {
                        atLeastOneGPUFailed = true;
                        listOfFailedGPUs.Add(i);
                    }
                    else if (!File.Exists($"data{i}.json"))
                    {
                        Logger.Debug($" Failed to run vulkaninfo via the first method, trying again...");
                        process.StartInfo.Arguments = $"--json={i}";
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();
                        if (!process.WaitForExit(10) || string.IsNullOrEmpty(output))
                        {
                            atLeastOneGPUFailed = true;
                            listOfFailedGPUs.Add(i);
                        }
                        else
                        {
                            File.WriteAllText($"data{i}.json", output);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(" Ran into error: ", ex);
                    atLeastOneGPUFailed = true;
                    listOfFailedGPUs.Add(i);
                }

                if (!listOfFailedGPUs.Contains(i))
                {
                    atLeastOneGPUSucceededVulkanInfo = true;
                }
                else
                {
                    Logger.Error($" Running vulkaninfo on GPU{i} failed! User likely has outdated drivers or an extremely old GPU.");
                }
            }
            if (!atLeastOneGPUSucceededVulkanInfo)
            {
                MessageBox.Show("The vulkaninfo check failed entirely. This usually means none of your GPU's support Vulkan. Make sure your drivers are up-to-date - don't rely on Windows Update drivers, either.\n\nDXVK is not available.");
                Logger.Error($" Running vulkaninfo failed entirely! User likely has outdated drivers or an extremely old GPU.");
                return (0, 0, 0, false, false, false, false);
            }

            Logger.Debug($" Analyzing the vulkaninfo for every .json generated...");
            for (int x = 0; x < gpucount; x++)
            {
                if (listOfFailedGPUs.Contains(x))
                {
                    Logger.Debug($" GPU{x} is in the failed list, skipping this iteration of the loop...");
                    continue;
                }

                Logger.Debug($" Checking data{x}.json...");
                if (File.Exists($"data{x}.json"))
                {
                    using (StreamReader file = File.OpenText($"data{x}.json"))
                    {
                        int dxvkSupport = 0;
                        JsonDocument doc;
                        try
                        {
                            doc = JsonDocument.Parse(file.ReadToEnd());
                        }
                        catch (JsonException)
                        {
                            Logger.Error($" Failed to read data{x}.json.");
                            atLeastOneGPUFailed = true;
                            listOfFailedGPUs.Add(x);
                            Logger.Debug($" Removing data{x}.json...");
                            file.Close();
                            File.Delete($"data{x}.json");
                            continue;
                        }

                        JsonElement root = doc.RootElement;
                        JsonElement properties;
                        JsonElement physicalDeviceProperties;
                        JsonElement extensions;
                        JsonElement features;

                        if (root.TryGetProperty("capabilities", out var capabilities))
                        {
                            var deviceCapabilities = capabilities.GetProperty("device");
                            properties = deviceCapabilities.GetProperty("properties");
                            physicalDeviceProperties = properties.GetProperty("VkPhysicalDeviceProperties");

                            extensions = deviceCapabilities.GetProperty("extensions");
                            features = deviceCapabilities.GetProperty("features");
                        }
                        else if (root.TryGetProperty("VkPhysicalDeviceProperties", out physicalDeviceProperties)
                                 && root.TryGetProperty("ArrayOfVkExtensionProperties", out extensions))
                        {
                            properties = root;
                            features = root;
                        }
                        else
                        {
                            Logger.Error($" Failed to read data{x}.json.");
                            atLeastOneGPUFailed = true;
                            Logger.Debug($" Removing data{x}.json...");
                            File.Delete($"data{x}.json");
                            continue;
                        }

                        string deviceName = physicalDeviceProperties.GetProperty("deviceName").GetString();
                        uint apiVersion = physicalDeviceProperties.GetProperty("apiVersion").GetUInt32();
                        (int, int) vulkanVer = ConvertApiVersion(apiVersion);
                        int vulkanVerMajor = vulkanVer.Item1;
                        int vulkanVerMinor = vulkanVer.Item2;

                        Logger.Info($" {deviceName}'s supported Vulkan version is: {vulkanVerMajor}.{vulkanVerMinor}");
                        Logger.Debug($" Checking if GPU{x} supports DXVK 2.x...");
                        if (CheckIfExtensionExists(extensions, "VK_EXT_robustness2")
                            && CheckIfExtensionExists(extensions,"VK_EXT_transform_feedback")
                            && features.TryGetProperty("VkPhysicalDeviceRobustness2FeaturesEXT", out var robustnessFeatures)
                            && robustnessFeatures.TryGetProperty("robustBufferAccess2", out var robustBufferAccess)
                            && robustBufferAccess.GetBoolean()
                            && robustnessFeatures.TryGetProperty("nullDescriptor", out var nullDescriptor)
                            && nullDescriptor.GetBoolean())
                        {
                            atLeastOneGPUSucceededJson = true;
                            Logger.Info($" GPU{x} supports DXVK 2.x, yay!");
                            dxvkSupport = 3;
                        }
                        else
                        {
                            Logger.Debug($" GPU{x} doesn't support DXVK 2.x, checking other versions...");
                            if (vulkanVerMajor == 1 && vulkanVerMinor <= 1)
                            {
                                atLeastOneGPUSucceededJson = true;
                                Logger.Info($" GPU{x} doesn't support DXVK or has outdated drivers.");
                            }
                            else if (vulkanVerMajor == 1 && vulkanVerMinor < 3)
                            {
                                atLeastOneGPUSucceededJson = true;
                                Logger.Info($" GPU{x} supports Legacy DXVK 1.x.");
                                dxvkSupport = 1;
                            }
                        }

                        try
                        {
                            if (features.TryGetProperty("VkPhysicalDeviceVulkan13Features", out var vk13features) &&
                                vk13features.TryGetProperty("maintenance4", out var maintenance4Prop))
                            {
                                maintenance4 = maintenance4Prop.GetBoolean();
                            }
                            else
                            {
                                maintenance4 = false;
                            }

                            if (extensions.TryGetProperty("VK_KHR_maintenance5", out var maintenance5Property))
                            {
                                maintenance5 = maintenance5Property.GetInt16() == 1;
                            }
                            else if (features.TryGetProperty("VkPhysicalDeviceMaintenance5FeaturesKHR", out var maintenance5Property2) &&
                                     maintenance5Property2.TryGetProperty("maintenance5", out var maintenance5Prop))
                            {
                                maintenance5 = maintenance5Prop.GetBoolean();
                            }
                            else
                            {
                                maintenance5 = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"Caught an exception while checking maintenance features for GPU{x}: {ex.Message}");
                            maintenance4 = false;
                            maintenance5 = false;
                        }

                        if (dxvkSupport == 3 && (!maintenance4 || !maintenance5))
                        {
                            Logger.Info($"GPU{x} highest supported DXVK version is DXVK 2.6.2. Versions 2.7 onwards require maintenance4 and maintenance5 extensions.");
                            dxvkSupport = 2;
                        }


                        var deviceType = physicalDeviceProperties.GetProperty("deviceType");
                        var deviceIsDiscreteGpu = deviceType.ValueKind switch
                        {
                            JsonValueKind.String => deviceType.GetString() == "VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU",
                            JsonValueKind.Number => deviceType.GetByte() == 2,
                            _ => throw new InvalidOperationException($"Unsupported value type {deviceType.ValueKind}"),
                        };

                        if (deviceIsDiscreteGpu && dxvkSupport > vkDgpuDxvkSupport)
                        {
                            Logger.Info($" GPU{x} is a discrete GPU.");
                            vkDgpuDxvkSupport = dxvkSupport;
                            igpuOnly = false;
                            if (deviceName.Contains("RTX 50"))
                            {
                                Logger.Info($" GPU{x} is a 50-series NVIDIA GPU.");
                                nvidia50series = true;
                            }
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

                        try
                        {
                            if (properties.TryGetProperty("VkPhysicalDeviceGraphicsPipelineLibraryPropertiesEXT", out var pipelinePropsExt))
                            {
                                if (pipelinePropsExt.TryGetProperty("graphicsPipelineLibraryIndependentInterpolationDecoration", out var gplVar) && gplVar.GetBoolean())
                                {
                                    Logger.Info($" GPU{x} supports GPL.");

                                    if (pipelinePropsExt.TryGetProperty("graphicsPipelineLibraryFastLinking", out var flVar) && flVar.GetBoolean())
                                    {
                                        Logger.Info($" GPU{x} supports Fast Linking.");
                                        if (gplSupport < 2)
                                            gplSupport = 2;
                                    }
                                    else
                                    {
                                        Logger.Debug($" GPU{x} doesn't support Fast Linking.");
                                        atLeastOneGPUFailedFL = true;
                                        if (gplSupport < 1)
                                            gplSupport = 1;
                                    }
                                }
                                else
                                {
                                    Logger.Info($" GPU{x} doesn't support GPL.");
                                    atLeastOneGPUFailedGPL = true;
                                }
                            }
                            else
                            {
                                Logger.Info($" GPU{x} doesn't support GPL.");
                                atLeastOneGPUFailedGPL = true;
                            }
                        }
                        catch
                        {
                            Logger.Debug($" Caught an exception, this likely means GPU{x} doesn't support GPL.");
                            atLeastOneGPUFailedGPL = true;
                        }
                    }
                    Logger.Debug($" Removing data{x}.json...");
                    File.Delete($"data{x}.json");
                }
                else { break; }
            }

            string messagetext = "";
            if (!atLeastOneGPUSucceededJson)
            {
                messagetext = messagetext + "The vulkaninfo check failed partially. This usually means one of your GPU's may support Vulkan but have outdated drivers - the tool will proceed assuming so, but installing DXVK is not recommended.";
                Logger.Error($" Running vulkaninfo failed partially. User likely has outdated drivers or an old GPU.");
                igpuOnly = true;
                dgpuOnly = false;
                intelIgpu = true;
                enableasync = true;
                vkIgpuDxvkSupport = 1;
            }
            else
            {
                if (atLeastOneGPUFailed && igpuOnly)
                {
                    if (messagetext != "") { messagetext = messagetext + "\n\n"; }
                    messagetext = messagetext + "The vulkaninfo check failed for discrete GPU but succeeded for the integrated GPU. This usually means your discrete GPU does not support Vulkan.\n\nDXVK is available, but with the assumption that you're going to be playing off the integrated GPU, not the dedicated one.";
                }
                else if (atLeastOneGPUFailed && !igpuOnly)
                {
                    if (messagetext != "") { messagetext = messagetext + "\n\n"; }
                    messagetext = messagetext + "The vulkaninfo check failed for one of the GPUs but succeeded for the rest. This usually means one of your discrete GPUs does not support Vulkan.\n\nDXVK is available, but with the assumption that you're going to be playing off the supported GPU.";
                }
                if ((atLeastOneGPUFailedGPL || atLeastOneGPUFailedFL) && gplSupport == 2)
                {
                    if (messagetext != "") { messagetext = messagetext + "\n\n"; }
                    messagetext = messagetext + "The GPL check failed for one of the GPUs but Fast Linking is supported by at least one of them. This usually means one of your discrete GPUs or the iGPU does not support DXVK in full.\n\nThe tool will proceed with the assumption that you're going to be playing off the GPU that didn't fail the GPL check (usually your main GPU), but provide options for async just incase.";
                    enableasync = true;
                }
                if (nvidia50series)
                {
                    if (messagetext != "") { messagetext = messagetext + "\n\n"; }
                    messagetext = messagetext + "Due to your (likely main) discrete GPU being a 50-series NVIDIA GPU, make sure your drivers are up-to-date, as DXVK may not work on outdated drivers.";
                }
                if (messagetext != "") { MessageBox.Show(messagetext + "\n\nMake sure your drivers are up-to-date - don't rely on Windows Update drivers, either."); }
            }
            return (vkDgpuDxvkSupport, vkIgpuDxvkSupport, gplSupport, igpuOnly, dgpuOnly, intelIgpu, enableasync);
        }
        private static bool CheckIfExtensionExists(JsonElement extensionElement, string extensionName)
        {
            switch (extensionElement.ValueKind)
            {
                case JsonValueKind.Object:
                    return extensionElement.TryGetProperty(extensionName, out _);
                case JsonValueKind.Array:
                    {
                        foreach (var extension in extensionElement.EnumerateArray())
                        {
                            if (extension.GetProperty("extensionName").GetString() == extensionName)
                            {
                                return true;
                            }
                        }
                        return false;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(extensionElement), $"Unknown extension element kind {extensionElement.ValueKind}");
            }
        }
    }
}
