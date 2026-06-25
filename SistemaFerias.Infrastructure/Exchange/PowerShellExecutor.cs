using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SistemaFerias.Infrastructure.Exchange;

public interface IPowerShellExecutor
{
    Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(string command);
}

public class PowerShellExecutor : IPowerShellExecutor
{
    private readonly ILogger<PowerShellExecutor> _logger;

    public PowerShellExecutor(ILogger<PowerShellExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(string command)
    {
        try
        {
            _logger.LogInformation("Executando comando PowerShell: {Command}", command);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            var output = outputTask.Result.Trim();
            var error = errorTask.Result.Trim();
            var success = process.ExitCode == 0 && string.IsNullOrEmpty(error);

            if (!success)
            {
                _logger.LogError("Erro ao executar comando PowerShell. ExitCode: {ExitCode}. Erro: {Error}", process.ExitCode, error);
            }
            else
            {
                _logger.LogInformation("Comando PowerShell executado com sucesso.");
            }

            return (success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na inicialização do processo PowerShell.");
            return (false, string.Empty, ex.Message);
        }
    }
}
