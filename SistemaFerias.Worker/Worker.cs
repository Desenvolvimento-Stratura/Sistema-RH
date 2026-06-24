using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Enums;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Services;

namespace SistemaFerias.Worker;

public class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
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

                var hoje = DateTime.Today;

                // Pendente -> EmFerias: bloquear conta no AD
                var feriasPendentes = await context.Ferias
                    .Where(f =>
                        f.Status == StatusFerias.Pendente &&
                        f.DataInicio.Date <= hoje)
                    .ToListAsync(stoppingToken);

                foreach (var ferias in feriasPendentes)
                {
                    var bloqueou = adService.BloquearConta(ferias.LoginAd);

                    if (bloqueou)
                    {
                        ferias.Status = StatusFerias.EmFerias;
                        ferias.DataEntradaFerias = DateTime.Now;
                        logger.LogInformation(
                            "Funcionário {Login} entrou em férias — conta bloqueada.",
                            ferias.LoginAd);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Funcionário {Login}: falha ao bloquear no AD.",
                            ferias.LoginAd);
                    }
                }

                // EmFerias -> Finalizado: reativar conta no AD
                var feriasEmAndamento = await context.Ferias
                    .Where(f =>
                        f.Status == StatusFerias.EmFerias &&
                        f.DataRetorno.Date <= hoje)
                    .ToListAsync(stoppingToken);

                foreach (var ferias in feriasEmAndamento)
                {
                    var reativou = adService.ReativarConta(ferias.LoginAd);

                    if (reativou)
                    {
                        ferias.Status = StatusFerias.Finalizado;
                        ferias.DataFinalizacaoFerias = DateTime.Now;
                        logger.LogInformation(
                            "Funcionário {Login} retornou das férias — conta reativada.",
                            ferias.LoginAd);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Funcionário {Login}: falha ao reativar no AD.",
                            ferias.LoginAd);
                    }
                }

                await context.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "Verificação executada em {Data}", DateTime.Now);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no Worker");
            }
        }
    }
}