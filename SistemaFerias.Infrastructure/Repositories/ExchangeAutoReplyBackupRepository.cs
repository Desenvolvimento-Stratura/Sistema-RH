using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Entities;
using SistemaFerias.Domain.Interfaces;
using SistemaFerias.Infrastructure.Data;

namespace SistemaFerias.Infrastructure.Repositories;

public class ExchangeAutoReplyBackupRepository : IExchangeAutoReplyBackupRepository
{
    private readonly AppDbContext _context;

    public ExchangeAutoReplyBackupRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExchangeAutoReplyBackup?> ObterPorIdAsync(int id)
    {
        return await _context.ExchangeAutoReplyBackups
            .Include(b => b.Ferias)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<ExchangeAutoReplyBackup?> ObterPorFeriasIdAsync(int feriasId)
    {
        return await _context.ExchangeAutoReplyBackups
            .Include(b => b.Ferias)
            .FirstOrDefaultAsync(b => b.FeriasId == feriasId);
    }

    public async Task<ExchangeAutoReplyBackup?> ObterUltimoBackupAtivoPorLoginAsync(string loginAd)
    {
        return await _context.ExchangeAutoReplyBackups
            .Where(b => b.LoginAd == loginAd && !b.BackupRestaurado)
            .OrderByDescending(b => b.DataBackup)
            .FirstOrDefaultAsync();
    }

    public async Task CriarAsync(ExchangeAutoReplyBackup backup)
    {
        await _context.ExchangeAutoReplyBackups.AddAsync(backup);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(ExchangeAutoReplyBackup backup)
    {
        _context.ExchangeAutoReplyBackups.Update(backup);
        await _context.SaveChangesAsync();
    }
}
