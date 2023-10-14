# Gillian's GTA IV Setup Utility
Semi-automatically installs DXVK and launch options for your GTA IV installation. It automatically checks your hardware and what options should be available (aswell as setting defaults).

This version is a re-write of the now-deprecated [Python version](https://github.com/SandeMC/GTAIVSetupUtility).

## Contribution

Contribution is highly welcome. I'm poorly experienced with C#, but this rewrite was needed for many reasons. And so, the current code is extremely clunky and works out of prayers.

## Attribution

Following NuGet packages were used to create this app:

- [ByteSize](https://github.com/omar/ByteSize) by Omar Khudeira - used to calculate and convert the VRAM correctly.
- [ini-parser](https://github.com/rickyah/ini-parser) by Ricardo Amores Harnandes - used to edit ZolikaPatch and FusionFix ini files.
- [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) by rpastric, contre, dahall - allows to create a Choose File dialogue box.
- [NLog](https://github.com/NLog/NLog) by Jarek Kowalski, Kim Chriestensen, Julian Verdurmen - used for logging.
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) by ICSharpCode - used for extracting a .tar.gz archive provided by DXVK.
- And Microsoft's official packages such as [System.Management](https://www.nuget.org/packages/System.Management/) for convenience and functional code.
