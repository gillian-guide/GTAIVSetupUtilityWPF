# Gillian's GTA IV Setup Utility
Semi-automatically installs DXVK and launch options for your GTA IV installation (+extra). It automatically checks your hardware and what options should be available (aswell as setting defaults).

![image](https://github.com/gillian-guide/GTAIVSetupUtilityWPF/assets/70141395/8f1cd70e-a6a2-4568-a222-853ca5820e0a)

This version is a re-write of the now-deprecated [Python version](https://github.com/SandeMC/GTAIVSetupUtility).

## Usage
- Launch the tool.
- Select your game folder, the one that includes `GTAIV.exe`.
- Press `Install DXVK`, after which, press `Setup launch options`.
- If experienced, play around with the toggles. Defaults should be fine, however, as they're automatically tailored to your hardware.
- Done!

## Features
- Automatically installing the best version of DXVK supported by your hardware by checking it's Vulkan capabilities.
- Automatically sets up your launch options, including monitor details and VRAM (VRAM not available for versions older than 1.0.8.0; have to paste options manually on 1.2)
- Accounting for multi-GPU setups, both during DXVK setup and setting up the launch options.
- Detects whether some features are unsupported by your hardware.
- Detects if ZolikaPatch and/or FusionFix are installed and edits their configuration files to be compatible with eachother.
- Properly enables/disables Borderless Fullscreen if using ZolikaPatch or FusionFix
- Warns the user if they have IVSDK .NET and DXVK installed at the same time as RTSS is enabled.
- Providing tips for what the launch options actually do. And *not* providing useless options.

## Contribution
Contribution is highly welcome. I'm poorly experienced with C#, but this rewrite was needed for many reasons. And so, the current code is extremely clunky and works out of prayers.

## Attribution
Following NuGet packages were used to create this app:

- [ByteSize](https://github.com/omar/ByteSize) by Omar Khudeira - used to calculate and convert the VRAM correctly.
- [Microsoft-WindowsAPICodePack-Shell](https://github.com/contre/Windows-API-Code-Pack-1.1) by rpastric, contre, dahall - allows to create a Choose File dialogue box.
- [NLog](https://github.com/NLog/NLog) by Jarek Kowalski, Kim Chriestensen, Julian Verdurmen - used for logging.
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) by ICSharpCode - used for extracting a .tar.gz archive provided by DXVK.
- And Microsoft's official packages such as [System.Management](https://www.nuget.org/packages/System.Management/) for convenience and functional code.

And these were used during development:

- [ini-parser](https://github.com/rickyah/ini-parser) by Ricardo Amores Harnandes - was used to edit ZolikaPatch and FusionFix ini files, replaced later due to issues with it.
