using System.Threading.Tasks;
using SistemaFerias.Domain.Entities;

namespace SistemaFerias.Domain.Interfaces;

public interface IExchangeAutoReplyBackupRepository
{
    Task<ExchangeAutoReplyBackup?> ObterPorIdAsync(int id);
    Task<ExchangeAutoReplyBackup?> ObterPorFeriasIdAsync(int feriasId);
    Task<ExchangeAutoReplyBackup?> ObterUltimoBackupAtivoPorLoginAsync(string loginAd);
    Task CriarAsync(ExchangeAutoReplyBackup backup);
    Task AtualizarAsync(ExchangeAutoReplyBackup backup);
}