using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you
public class DXVKInstaller
{

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static void InstallDXVK(string downloadUrl, string installationDir, List<string> dxvkConf)
    {
        try
        {
            Logger.Debug(" Downloading the .tar.gz... (this entirely depends on user's internet)");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(downloadUrl, "./dxvk.tar.gz");
            }

            Logger.Debug(" Extracting the d3d9.dll from the archive...");
            using (FileStream fsIn = new FileStream("./dxvk.tar.gz", FileMode.Open))
            using (GZipInputStream gzipStream = new GZipInputStream(fsIn))
            using (TarInputStream tarStream = new TarInputStream(gzipStream))
            {
                TarEntry entry;
                while ((entry = tarStream.GetNextEntry()) != null)
                {
                    if (entry.Name.EndsWith("x32/d3d9.dll"))
                    {
                        using (FileStream fsOut = File.Create(Path.Combine(installationDir, "d3d9.dll")))
                        {
                            tarStream.CopyEntryContents(fsOut);
                            Logger.Debug(" d3d9.dll extracted into the game folder.");
                        }
                        break;
                    }
                }
            }

            Logger.Debug(" Deleting the .tar.gz...");
            File.Delete("dxvk.tar.gz");

            Logger.Debug(" Writing the dxvk.conf...");
            using (StreamWriter confWriter = File.CreateText(Path.Combine(installationDir, "dxvk.conf")))
            {
                foreach (string option in dxvkConf)
                {
                    confWriter.WriteLine(option);
                }
            }
            Logger.Debug(" dxvk.conf successfully written to game folder.");
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error installing DXVK");
        }
    }
}
