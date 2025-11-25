using System;
using System.Diagnostics;
using System.IO;
using NLog;

namespace GTAIVSetupUtilityWPF.Common
{
    public static class AppVersionGrabber
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
        public static string GetFileVersion(string filePath)
        {
            const string defaultVersion = "0.0.0.0";
        
            return (File.Exists(filePath), GetVersionInfo(filePath)) switch
            {
                (false, _) => defaultVersion,
                (true, { } version) when !string.IsNullOrEmpty(version) => version,
                _ => defaultVersion
            };
        }
    
        private static string? GetVersionInfo(string filePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return versionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error retrieving file version for {FilePath}", filePath);
                return null;
            }
        }
    }
}