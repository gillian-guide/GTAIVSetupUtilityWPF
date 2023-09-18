using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using System.Diagnostics;

public class DXVKInstaller
{
    public static void InstallDXVK(string downloadUrl, string installationDir, List<string> dxvkConf)
    {
        try
        {
            // downloading the archive
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(downloadUrl, "./dxvk.tar.gz");
            }

            // extracting d3d9.dll from the archive
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
                        }
                        break;
                    }
                }
            }

            File.Delete("dxvk.tar.gz");

            // writing dxvk.conf to the game directory
            using (StreamWriter confWriter = File.CreateText(Path.Combine(installationDir, "dxvk.conf")))
            {
                foreach (string option in dxvkConf)
                {
                    confWriter.WriteLine(option);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing DXVK: {ex.Message}");
        }
    }
}
