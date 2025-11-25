using System.Collections.Generic;
using System.IO;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you
// i couldn't find a good ini parser for my needs so i just begged chatgpt for one tbh, idk if it works for anything else but i'd rather not qusetion it

namespace GTAIVSetupUtilityWPF.Common
{
    public class IniEditor
    {
        private readonly Dictionary<string, Dictionary<string, string>> iniData = new Dictionary<string, Dictionary<string, string>>();
        private string FilePath { get; }

        public IniEditor(string? filePath)
        {
            FilePath = filePath;
            OpenFile();
        }

        private void OpenFile()
        {
            if (File.Exists(FilePath))
            {
                var lines = File.ReadAllLines(FilePath);
                string currentGroup = null;
                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // This is a group header
                        currentGroup = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!iniData.ContainsKey(currentGroup))
                        {
                            iniData[currentGroup] = new Dictionary<string, string>();
                        }
                    }
                    else if (currentGroup != null)
                    {
                        int separatorIndex = trimmedLine.IndexOf('=');
                        if (separatorIndex >= 0)
                        {
                            string key = trimmedLine.Substring(0, separatorIndex).Trim();
                            string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                            iniData[currentGroup][key] = value;
                        }
                        else
                        {
                            // Preserve non-key-value lines, like comments or empty lines
                            if (!iniData[currentGroup].ContainsKey(trimmedLine))
                            {
                                iniData[currentGroup][trimmedLine] = null!;
                            }
                        }
                    }
                }
            }
        }

        public string ReadValue(string group, string key)
        {
            if (iniData.TryGetValue(group, out var groupData) && groupData.TryGetValue(key, out var value))
            {
                return value;
            }
            return "N/A"; // Default value if the key or group is not found
        }

        public void EditValue(string group, string key, string newValue)
        {
            if (!iniData.ContainsKey(group))
            {
                iniData[group] = new Dictionary<string, string>();
            }

            iniData[group][key] = newValue;
        }

        public void SaveFile()
        {
            List<string> newLines = new List<string>();

            foreach (var group in iniData.Keys)
            {
                newLines.Add("[" + group + "]");
                foreach (var kvp in iniData[group])
                {
                    if (kvp.Value != null)
                    {
                        newLines.Add(kvp.Key + "=" + kvp.Value);
                    }
                    else
                    {
                        // Preserve non-key-value lines
                        newLines.Add(kvp.Key);
                    }
                }
            }

            File.WriteAllLines(FilePath, newLines);
        }
    }
}
