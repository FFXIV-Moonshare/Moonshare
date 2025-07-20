🌙 Moonshare – WIP ALPHA


A Dalamud plugin enabling peer-to-peer file sharing and session linking in FINAL FANTASY XIV.
🔍 Overview

Moonshare is an experimental Dalamud plugin for FINAL FANTASY XIV that allows players to connect to each other using unique user IDs and share files securely. The plugin provides a clean, user-friendly interface to manage sessions and transfers, including:

    A unique local UserID to identify yourself

    Direct connection to other players via their UserID

    Live connection status and easy disconnect options

    Basic file transfer management (WIP)

✨ Features

    Intuitive graphical UI built with ImGui

    Quick access via the /moonshare slash command

    Automatic configuration saving to your plugin folder

    Clean session management between players

    Fully open source and extensible for custom use cases

🔧 Installation

    Make sure FINAL FANTASY XIV is set up with XIVLauncher and Dalamud.

    Download the latest Moonshare release from [your release archive or GitHub].

    Place the plugin .dll into the Development Plugins directory (/xlsettings → Experimental → Dev Plugin Locations).

    Enable the plugin via /xlplugins → Dev Tools → Installed Dev Plugins → Moonshare.

    Open the plugin with /moonshare.

🚀 How to Use

    Run /moonshare to open the plugin window.

    Your personal UserID is displayed at the top — share it to let others connect to you.

    Enter another player’s UserID and press Connect to establish a session.

    Once connected, you’ll be able to exchange files (depending on plugin implementation).

    Use Disconnect at any time to end the session.

🛠 Development & Customization

Moonshare is written in C# using the Dalamud API and ImGuiNET. The codebase is modular and cleanly structured:

    Plugin.cs: Main entry point, lifecycle, and hooks

    UserSessionManager.cs: Handles UserIDs, sessions, and transfer logic

    Windows/: Contains the ImGui-based UI (e.g., MainWindow.cs)

The plugin automatically creates and loads a config file under Moonshare/Moonshare.json.
📦 Requirements

    FINAL FANTASY XIV with Dalamud via XIVLauncher

    (Optional for devs) .NET 8 SDK for plugin development

    Internet access for file transfers

💬 Support & Contribution

Found a bug? Have ideas or suggestions? Join us on the Moonshare Discord server or open an issue on GitHub. Contributions are always welcome!
📄 License

Moonshare is licensed under the MIT License. See the LICENSE file in the repository for full details.

    ⚠️ Disclaimer: This plugin is not affiliated with or endorsed by Square Enix. Use at your own discretion.
