using System.Collections.Generic;
using System.IO;

// hi here, i'm an awful coder, so please clean up for me if it really bothers you
// i couldn't find a good ini parser for my needs so i just begged chatgpt for one tbh, idk if it works for anything else but i'd rather not qusetion it

namespace GTAIVSetupUtilityWPF.Common;

public sealed class IniEditor
{
    private const string DefaultValue = "N/A";
        
    private readonly Dictionary<string, Dictionary<string, string>> _iniData = [];
    private readonly string? _filePath;

    public IniEditor(string? filePath)
    {
        _filePath = filePath;
        LoadFile();
    }

    private void LoadFile()
    {
        if (!File.Exists(_filePath))
            return;

        string? currentSection = null;

        foreach (var line in File.ReadLines(_filePath))
        {
            var trimmedLine = line.Trim();

            if (IsSection(trimmedLine))
            {
                currentSection = ExtractSectionName(trimmedLine);
                EnsureSectionExists(currentSection);
            }
            else if (currentSection is not null)
            {
                ParseLineIntoSection(trimmedLine, currentSection);
            }
        }
    }

    private static bool IsSection(string line) 
        => line.StartsWith('[') && line.EndsWith(']');

    private static string ExtractSectionName(string line) 
        => line[1..^1];

    private void EnsureSectionExists(string section)
    {
        if (!_iniData.ContainsKey(section))
        {
            _iniData[section] = [];
        }
    }

    private void ParseLineIntoSection(string line, string section)
    {
        int separatorIndex = line.IndexOf('=');
            
        if (separatorIndex >= 0)
        {
            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();
            _iniData[section][key] = value;
        }
        else
        {
            // Preserve non-key-value lines (comments, empty lines)
            if (!_iniData[section].ContainsKey(line))
            {
                _iniData[section][line] = null!;
            }
        }
    }

    /// <summary>
    /// Reads a value from the INI file.
    /// </summary>
    /// <param name="section">The section name (e.g., "MAIN")</param>
    /// <param name="key">The key name</param>
    /// <returns>The value if found, otherwise "N/A"</returns>
    public string ReadValue(string section, string key)
    {
        if (_iniData.TryGetValue(section, out var sectionData) && 
            sectionData.TryGetValue(key, out var value))
        {
            return value;
        }
            
        return DefaultValue;
    }

    /// <summary>
    /// Edits or creates a key-value pair in the specified section.
    /// </summary>
    /// <param name="section">The section name</param>
    /// <param name="key">The key name</param>
    /// <param name="newValue">The new value to set</param>
    public void EditValue(string section, string key, string newValue)
    {
        EnsureSectionExists(section);
        _iniData[section][key] = newValue;
    }

    /// <summary>
    /// Saves the INI data back to the file.
    /// </summary>
    public void SaveFile()
    {
        var lines = new List<string>();

        foreach (var (section, entries) in _iniData)
        {
            lines.Add($"[{section}]");
                
            foreach (var (key, value) in entries)
            {
                // Preserve non-key-value lines
                lines.Add($"{key}={value}");
            }
        }

        File.WriteAllLines(_filePath!, lines);
    }
}