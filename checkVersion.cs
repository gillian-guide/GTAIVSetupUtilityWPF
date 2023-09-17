using System;
using System.Diagnostics;

public class checkVersion
{
    public static string GetFileVersion(string filePath)
    {
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                string version = fileVersionInfo.FileVersion;
                return !string.IsNullOrEmpty(version) ? version : "0.0.0.0";
            }
            else
            {
                return "0.0.0.0";
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions here if needed
            Console.WriteLine("Error: " + ex.Message);
            return "0.0.0.0";
        }
    }
}
