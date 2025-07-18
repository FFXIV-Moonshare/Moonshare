using System;
using Dalamud.Configuration;

namespace Moonshare_Plugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        // Beispiel Properties für dein Plugin
        public bool IsConfigWindowMovable { get; set; } = true;
        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // Dummy Settings, die du später für dein Moonshare Plugin nutzen kannst:
        public string ServerAddress { get; set; } = "ws://localhost:5000/ws";
        public int ReconnectDelaySeconds { get; set; } = 10;
        public bool EnableAutoReconnect { get; set; } = true;
        public int MaxConcurrentTransfers { get; set; } = 3;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
