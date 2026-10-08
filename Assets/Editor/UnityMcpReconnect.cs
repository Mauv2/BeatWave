using System;
using System.IO;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// Reconnect only when explicitly requested by the project helper or menu.
[InitializeOnLoad]
internal static class UnityMcpReconnect
{
    static readonly string RequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../.unity-mcp/reconnect.request"));
    static bool connecting;

    static UnityMcpReconnect()
    {
        EditorApplication.delayCall += CheckRequest;
    }

    static void CheckRequest()
    {
        if (File.Exists(RequestPath) && File.ReadAllText(RequestPath).Trim() == "requested")
            Connect();
    }

    [MenuItem("BeatWave/Reconnect Unity MCP")]
    internal static async void Connect()
    {
        if (connecting) return;
        connecting = true;
        try
        {
            File.WriteAllText(RequestPath, "connecting");
            EditorConfigurationCache.Instance.Refresh();
            await MCPServiceLocator.Bridge.StopAsync();
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            File.WriteAllText(RequestPath, connected ? "connected" : "failed");
            Debug.Log("[Unity MCP Reconnect] Connected: " + connected);
        }
        catch (Exception error)
        {
            File.WriteAllText(RequestPath, "failed: " + error.Message);
            Debug.LogException(error);
        }
        finally
        {
            connecting = false;
        }
    }
}
