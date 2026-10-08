# BeatWave1 Unity MCP

Installed on 2026-10-06: CoplayDev Unity MCP 10.3.0 (commit aa5fc638d623f56178d50329d9ad8c541a57fe66), uv 0.12.23, Python 3.12.15.

- Unity package: `Packages/com.coplaydev.unity-mcp` (embedded package).
- Python server and its isolated environment: `.unity-mcp/source/Server`.
- MCP endpoint: `http://127.0.0.1:8080/mcp`.
- Codex server name: `unityMCP`, registered in the user's Codex configuration.
- Unity automatically connects and starts the server when the project opens. Configure it from **Window > MCP for Unity**.
- Editor-only Roslyn libraries are in `Assets/Plugins/Roslyn`; they are excluded from player builds.

If the server is stopped, run this from the project directory:

```powershell
& .unity-mcp/start-server.ps1
```

Verify the connection without changing the scene:

```powershell
& .unity-mcp/source/Server/.venv/Scripts/python.exe .unity-mcp/verify-connection.py
```

Verification passed: 48 tools, connected instance `BeatWave1@8acd7a5e0391bccd`, active scene `Assets/Scenes/Stage2.unity`, and project info lookup.

If the current Codex chat has not loaded the new tools, use **Settings > MCP servers > Restart** or restart Codex.

The previous Codex config is backed up in `codex-config.before-unity.toml`. Machine-specific Python and Git directories were added to the user PATH; existing apps inherit those changes on their next launch. `Assets/Editor/UnityMcpSetup.cs` also supplies the execution paths in the current Unity session.

Sources: https://github.com/CoplayDev/unity-mcp and https://developers.openai.com/codex/mcp
