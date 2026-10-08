$ErrorActionPreference = 'Stop'
$serverExecutable = Join-Path $PSScriptRoot 'source/Server/.venv/Scripts/mcp-for-unity.exe'
try {
    $health = Invoke-RestMethod 'http://127.0.0.1:8080/health' -TimeoutSec 2
    if ($health) { Write-Output 'Unity MCP server is already running.'; exit 0 }
} catch { }
Start-Process -FilePath $serverExecutable -ArgumentList @('--transport', 'http', '--http-url', 'http://127.0.0.1:8080', '--default-instance', 'BeatWave1') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $PSScriptRoot 'server.stdout.log') -RedirectStandardError (Join-Path $PSScriptRoot 'server.stderr.log')
