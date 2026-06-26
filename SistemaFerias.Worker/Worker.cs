using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Entities;
using SistemaFerias.Domain.Enums;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Exchange;
using SistemaFerias.Infrastructure.Services;

namespace SistemaFerias.Worker;

public class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();
                var adService = scope.ServiceProvider
                    .GetRequiredService<IActiveDirectoryService>();
                var exchangeService = scope.ServiceProvider
                    .GetRequiredService<IExchangeOnlineService>();

                var hoje = DateTime.Today;

                var feriasPendentes = await context.Ferias
                    .Where(f =>
                        f.Status == StatusFerias.Pendente &&
                        f.DataInicio.Date <= hoje)
                    .ToListAsync(stoppingToken);

                foreach (var ferias in feriasPendentes)
                {
                    await ProcessarEntradaFeriasAsync(
                        context, adService, exchangeService, ferias, stoppingToken);
                }

                var feriasEmAndamento = await context.Ferias
                    .Where(f =>
                        f.Status == StatusFerias.EmFerias &&
                        f.DataRetorno.Date <= hoje)
                    .ToListAsync(stoppingToken);

                foreach (var ferias in feriasEmAndamento)
                {
                    await ProcessarRetornoFeriasAsync(
                        context, adService, ferias, stoppingToken);
                }

                await context.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Verificacao executada em {Data}", DateTime.Now);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no Worker");
            }
        }
    }

    private async Task ProcessarEntradaFeriasAsync(
        AppDbContext context,
        IActiveDirectoryService adService,
        IExchangeOnlineService exchangeService,
        Ferias ferias,
        CancellationToken stoppingToken)
    {
        try
        {
            var backup = await context.ExchangeAutoReplyBackups
                .FirstOrDefaultAsync(b => b.FeriasId == ferias.Id, stoppingToken);

            if (backup == null)
            {
                var config = await exchangeService.GetAutoReplyConfigurationAsync(ferias.LoginAd);

                backup = new ExchangeAutoReplyBackup
                {
                    FeriasId = ferias.Id,
                    LoginAd = ferias.LoginAd,
                    Email = ferias.LoginAd,
                    Enabled = config.IsEnabled,
                    ExternalAudience = config.ExternalAudience,
                    InternalReply = config.InternalMessage,
                    ExternalReply = config.ExternalMessage,
                    StartTime = config.StartTime,
                    EndTime = config.EndTime,
                    DataBackup = DateTime.UtcNow
                };

                context.ExchangeAutoReplyBackups.Add(backup);
            }

            var autoReplyDesligado = await exchangeService.DisableAutoReplyAsync(ferias.LoginAd);

            if (!autoReplyDesligado)
            {
                logger.LogWarning(
                    "Funcionario {Login}: falha ao desativar resposta automatica no Exchange.",
                    ferias.LoginAd);
                return;
            }

            var bloqueou = adService.BloquearConta(ferias.LoginAd);

            if (!bloqueou)
            {
                logger.LogWarning(
                    "Funcionario {Login}: falha ao bloquear no AD.",
                    ferias.LoginAd);
                return;
            }

            ferias.Status = StatusFerias.EmFerias;
            ferias.DataEntradaFerias = DateTime.Now;

            logger.LogInformation(
                "Funcionario {Login} entrou em ferias - resposta automatica desativada e conta bloqueada.",
                ferias.LoginAd);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Funcionario {Login}: falha ao processar entrada em ferias.",
                ferias.LoginAd);
        }
    }

    private async Task ProcessarRetornoFeriasAsync(
        AppDbContext context,
        IActiveDirectoryService adService,
        Ferias ferias,
        CancellationToken stoppingToken)
    {
        var reativou = adService.ReativarConta(ferias.LoginAd);

        if (!reativou)
        {
            logger.LogWarning(
                "Funcionario {Login}: falha ao reativar no AD.",
                ferias.LoginAd);
            return;
        }

        var backup = await context.ExchangeAutoReplyBackups
            .FirstOrDefaultAsync(b => b.FeriasId == ferias.Id && !b.BackupRestaurado, stoppingToken);

        if (backup != null)
        {
            backup.BackupRestaurado = true;
            backup.DataRestauracao = DateTime.UtcNow;
        }

        ferias.Status = StatusFerias.Finalizado;
        ferias.DataFinalizacaoFerias = DateTime.Now;

        logger.LogInformation(
            "Funcionario {Login} retornou das ferias - conta reativada.",
            ferias.LoginAd);
    }
}