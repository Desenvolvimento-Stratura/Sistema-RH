using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SistemaFerias.Infrastructure.Exchange;

public class AutoReplyConfiguration
{
    public string AutoReplyState { get; set; } = "Disabled";
    public string ExternalAudience { get; set; } = "None";
    public string InternalMessage { get; set; } = string.Empty;
    public string ExternalMessage { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public bool IsEnabled =>
        AutoReplyState.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ||
        AutoReplyState.Equals("Scheduled", StringComparison.OrdinalIgnoreCase);
}

public interface IExchangeOnlineService
{
    Task<AutoReplyConfiguration> GetAutoReplyConfigurationAsync(string email);
    Task<bool> DisableAutoReplyAsync(string email);
}

public class ExchangeOnlineService : IExchangeOnlineService
{
    private readonly IPowerShellExecutor _executor;
    private readonly ILogger<ExchangeOnlineService> _logger;
    private readonly bool _useMock;
    private readonly string _adminUser;

    public ExchangeOnlineService(
        IPowerShellExecutor executor,
        IConfiguration configuration,
        ILogger<ExchangeOnlineService> logger)
    {
        _executor = executor;
        _logger = logger;
        _useMock = bool.TryParse(configuration["ExchangeOnline:UseMock"], out var mock) && mock;
        _adminUser = configuration["ExchangeOnline:AdminUser"] ?? string.Empty;
    }

    public async Task<AutoReplyConfiguration> GetAutoReplyConfigurationAsync(string email)
    {
        if (_useMock)
        {
            _logger.LogInformation("[MOCK] Buscando configuracao de AutoReply para: {Email}", email);
            return new AutoReplyConfiguration
            {
                AutoReplyState = "Scheduled",
                ExternalAudience = "All",
                InternalMessage = "<p>Estou em ferias (Interno) - Mocked</p>",
                ExternalMessage = "<p>I am on vacation (Externo) - Mocked</p>",
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(9)
            };
        }

        if (string.IsNullOrWhiteSpace(_adminUser))
            throw new InvalidOperationException("ExchangeOnline:AdminUser nao configurado.");

        var script = $@"
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName '{EscapePowerShellString(_adminUser)}' -ShowBanner:$false
Get-MailboxAutoReplyConfiguration -Identity '{EscapePowerShellString(email)}' | Select-Object AutoReplyState, ExternalAudience, InternalMessage, ExternalMessage, StartTime, EndTime | ConvertTo-Json -Depth 3
Disconnect-ExchangeOnline -Confirm:$false
";

        var (success, output, error) = await _executor.ExecuteCommandAsync(script);

        if (!success)
        {
            _logger.LogError("Falha ao obter AutoReply do Exchange. Erro: {Error}", error);
            throw new InvalidOperationException($"Erro ao obter AutoReply do Exchange: {error}");
        }

        try
        {
            var config = JsonSerializer.Deserialize<AutoReplyConfiguration>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config ?? new AutoReplyConfiguration();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao deserializar a saida do PowerShell: {Output}", output);
            throw new InvalidOperationException("A saida do PowerShell nao estava em formato JSON valido.", ex);
        }
    }

    public async Task<bool> DisableAutoReplyAsync(string email)
    {
        if (_useMock)
        {
            _logger.LogInformation("[MOCK] Desabilitando AutoReply para: {Email}", email);
            return true;
        }

        if (string.IsNullOrWhiteSpace(_adminUser))
            throw new InvalidOperationException("ExchangeOnline:AdminUser nao configurado.");

        var script = $@"
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName '{EscapePowerShellString(_adminUser)}' -ShowBanner:$false
Set-MailboxAutoReplyConfiguration -Identity '{EscapePowerShellString(email)}' -AutoReplyState Disabled
Disconnect-ExchangeOnline -Confirm:$false
";

        var (success, _, error) = await _executor.ExecuteCommandAsync(script);

        if (!success)
        {
            _logger.LogError("Falha ao desabilitar AutoReply para {Email}. Erro: {Error}", email, error);
            return false;
        }

        _logger.LogInformation("AutoReply desabilitado com sucesso para: {Email}", email);
        return true;
    }

    private static string EscapePowerShellString(string value)
        => value.Replace("'", "''");
}