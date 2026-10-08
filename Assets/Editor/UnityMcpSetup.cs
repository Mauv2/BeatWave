using System;
using System.IO;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// Applies this machine's MCP settings once; later changes in the MCP window remain intact.
[InitializeOnLoad]
internal static class UnityMcpSetup
{
    static UnityMcpSetup()
    {
        EditorApplication.delayCall += Configure;
    }

    private static async void Configure()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string toolsPath = Path.Combine(root, ".unity-mcp/tools/uv");
        string pythonPath = Path.Combine(root, ".unity-mcp/source/Server/.venv/Scripts");
        string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        if (currentPath.IndexOf(pythonPath, StringComparison.OrdinalIgnoreCase) < 0)
            Environment.SetEnvironmentVariable("PATH", pythonPath + ";" + toolsPath + ";C:\\Program Files\\Git\\cmd;" + currentPath);
        string marker = "BeatWave1.UnityMcpSetup.10.3.0.1." + root;
        if (EditorPrefs.GetBool(marker, false)) return;

        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorPrefs.SetString("MCPForUnity.UvxPath", Path.Combine(root, ".unity-mcp/tools/uv/uv.exe"));
        EditorPrefs.SetString("MCPForUnity.GitUrlOverride", Path.Combine(root, ".unity-mcp/source/Server"));
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
        EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
        EditorConfigurationCache.Instance.Refresh();
        EditorPrefs.SetBool(marker, true);
        Debug.Log("[Unity MCP Setup] Local HTTP connection configured; automatic connection enabled.");
        try
        {
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log("[Unity MCP Setup] Bridge connected: " + connected);
        }
        catch (Exception error)
        {
            EditorPrefs.SetBool(marker, false);
            Debug.LogException(error);
        }
    }
}
