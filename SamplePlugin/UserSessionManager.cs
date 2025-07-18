using System;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;

namespace Moonshare_Plugin;

public class UserSessionManager
{
    public string? LocalUserId { get; private set; }
    public string? ConnectedToUserId { get; private set; }

    public void Initialize()
    {
        var player = Plugin.ClientState.LocalPlayer;
        if (player == null)
        {
            Plugin.Log.Warning("LocalPlayer is null during UserSessionManager.Initialize");
            LocalUserId = "unknown";
            return;
        }

        // Verwende ToString(), da Name und HomeWorld öffentlich nicht zugänglich sind
        var raw = player.ToString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            LocalUserId = "unknown";
            return;
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        LocalUserId = Convert.ToHexString(hash).ToLower();
        Plugin.Log.Information($"UserSessionManager initialized. LocalUserId: {LocalUserId}");
    }

    public void ConnectTo(string otherUserId)
    {
        ConnectedToUserId = otherUserId;
        Plugin.Log.Information($"Connected to {otherUserId}");
    }

    public void Disconnect()
    {
        Plugin.Log.Information($"Disconnected from {ConnectedToUserId}");
        ConnectedToUserId = null;
    }

    public bool IsConnected => ConnectedToUserId != null;
}
