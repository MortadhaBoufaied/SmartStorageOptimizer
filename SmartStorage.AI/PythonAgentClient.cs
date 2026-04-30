using System.Diagnostics;
using System.Text.Json;
using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.AI;

public sealed class PythonAgentClient : IAiAgentClient
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;

    public PythonAgentClient(string pythonExePath, string agentScriptPath)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{agentScriptPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        _process.Start();
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;
    }

    public async Task<AiAnalysisResponse?> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request);
        await _stdin.WriteLineAsync(json);
        await _stdin.FlushAsync();
        var line = await _stdout.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line)) return null;
        return JsonSerializer.Deserialize<AiAnalysisResponse>(line);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _stdin.WriteLineAsync("__shutdown__");
            await _stdin.FlushAsync();
        }
        catch { }
        if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        _process.Dispose();
    }
}
