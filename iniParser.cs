using System.Collections.Generic;
using System.IO;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you
// i couldn't find a good ini parser for my needs so i just begged chatgpt for one tbh, idk if it works for anything else but i'd rather not qusetion it

namespace GTAIVSetupUtilityWPF
{
    public class IniParser
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Dictionary<string, string> iniData = new Dictionary<string, string>();
        private List<string> originalLines = new List<string>();

        public string FilePath { get; }

        public IniParser(string filePath)
        {
            FilePath = filePath;
            OpenFile();
        }

        private void OpenFile()
        {
            if (File.Exists(FilePath))
            {
                var lines = File.ReadAllLines(FilePath);
                foreach (var line in lines)
                {
                    originalLines.Add(line);
                    var trimmedLine = line.Trim();
                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex >= 0)
                    {
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();
                        string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                        iniData[key] = value;
                    }
                }
            }
        }

        public string ReadValue(string key)
        {
            if (iniData.TryGetValue(key, out var value))
            {
                return value;
            }

            return "0";
        }

        public void EditValue(string key, string newValue)
        {
            iniData[key] = newValue;
        }

        public void SaveFile()
        {
            List<string> newLines = new List<string>();

            for (int i = 0; i < originalLines.Count; i++)
            {
                var originalLine = originalLines[i];
                var trimmedLine = originalLine.Trim();
                int separatorIndex = trimmedLine.IndexOf('=');

                if (separatorIndex >= 0)
                {
                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    string value = iniData.ContainsKey(key) ? iniData[key] : trimmedLine.Substring(separatorIndex + 1).Trim();
                    newLines.Add($"{key} = {value}");
                }
                else
                {
                    newLines.Add(originalLine);
                }
            }

            File.WriteAllLines(FilePath, newLines);
        }
    }
}
