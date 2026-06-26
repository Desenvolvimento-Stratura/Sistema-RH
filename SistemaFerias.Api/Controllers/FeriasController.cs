using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFerias.Api.DTOs;
using SistemaFerias.Domain.Entities;
using SistemaFerias.Domain.Enums;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Services;

namespace SistemaFerias.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeriasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IActiveDirectoryService _adService;

    public FeriasController(AppDbContext context, IActiveDirectoryService adService)
    {
        _context = context;
        _adService = adService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ferias>>> GetFerias()
    {
        return await _context.Ferias
            .OrderByDescending(f => f.DataInicio)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ferias>> GetFeriasById(int id)
    {
        var ferias = await _context.Ferias.FindAsync(id);
        if (ferias == null) return NotFound();
        return Ok(ferias);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Ferias>>> GetFeriasByStatus(StatusFerias status)
    {
        var ferias = await _context.Ferias
            .Where(f => f.Status == status)
            .OrderByDescending(f => f.DataInicio)
            .ToListAsync();
        return Ok(ferias);
    }

    [HttpGet("login/{login}")]
    public async Task<ActionResult<IEnumerable<Ferias>>> GetFeriasByLogin(string login)
    {
        var ferias = await _context.Ferias
            .Where(f => f.LoginAd == login)
            .OrderByDescending(f => f.DataInicio)
            .ToListAsync();
        return Ok(ferias);
    }

    [HttpGet("validar-login/{login}")]
    public ActionResult ValidarLogin(string login)
    {
        var existe = _adService.UsuarioExiste(login);
        if (existe)
            return Ok(new { existe = true, mensagem = "Usuário encontrado no AD." });
        return NotFound(new { existe = false, mensagem = "Usuário não encontrado no Active Directory." });
    }

    [HttpPost]
    public async Task<ActionResult<Ferias>> CreateFerias(CreateFeriasDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeFuncionario))
            return BadRequest("O nome do funcionário é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.LoginAd))
            return BadRequest("O Login AD é obrigatório.");

        if (dto.DataRetorno <= dto.DataInicio)
            return BadRequest("A data de retorno deve ser maior que a data de início.");

        var conflito = await _context.Ferias.AnyAsync(f =>
            f.LoginAd == dto.LoginAd &&
            dto.DataInicio <= f.DataRetorno &&
            dto.DataRetorno >= f.DataInicio);

        if (conflito)
            return BadRequest("Já existe um período de férias cadastrado para este funcionário nesse intervalo.");

        var ferias = new Ferias
        {
            NomeFuncionario = dto.NomeFuncionario,
            LoginAd = dto.LoginAd,
            DataInicio = dto.DataInicio,
            DataRetorno = dto.DataRetorno,
            Status = StatusFerias.Pendente
        };

        _context.Ferias.Add(ferias);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFeriasById), new { id = ferias.Id }, ferias);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFerias(int id, UpdateFeriasDto dto)
    {
        var ferias = await _context.Ferias.FindAsync(id);
        if (ferias == null) return NotFound();

        if (ferias.Status == StatusFerias.EmFerias)
            return BadRequest("Não é possível editar um registro em andamento.");

        if (string.IsNullOrWhiteSpace(dto.NomeFuncionario))
            return BadRequest("O nome do funcionário é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.LoginAd))
            return BadRequest("O Login AD é obrigatório.");

        if (dto.DataRetorno <= dto.DataInicio)
            return BadRequest("A data de retorno deve ser maior que a data de início.");

        var conflito = await _context.Ferias.AnyAsync(f =>
            f.Id != id &&
            f.LoginAd == dto.LoginAd &&
            dto.DataInicio <= f.DataRetorno &&
            dto.DataRetorno >= f.DataInicio);

        if (conflito)
            return BadRequest("Já existe um período de férias cadastrado para este funcionário nesse intervalo.");

        ferias.NomeFuncionario = dto.NomeFuncionario;
        ferias.LoginAd = dto.LoginAd;
        ferias.DataInicio = dto.DataInicio;
        ferias.DataRetorno = dto.DataRetorno;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/reativar")]
    public async Task<IActionResult> ReativarManual(int id)
    {
        var ferias = await _context.Ferias.FindAsync(id);
        if (ferias == null) return NotFound();

        if (ferias.Status != StatusFerias.EmFerias)
            return BadRequest("Apenas registros com status 'Em Férias' podem ser reativados manualmente.");

        var reativou = _adService.ReativarConta(ferias.LoginAd);
        if (!reativou)
            return StatusCode(502, "Falha ao reativar a conta no Active Directory.");

        var backup = await _context.ExchangeAutoReplyBackups
            .FirstOrDefaultAsync(b => b.FeriasId == ferias.Id && !b.BackupRestaurado);

        if (backup != null)
        {
            backup.BackupRestaurado = true;
            backup.DataRestauracao = DateTime.UtcNow;
        }

        ferias.Status = StatusFerias.Finalizado;
        ferias.DataFinalizacaoFerias = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = $"Conta {ferias.LoginAd} reativada e férias encerradas." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFerias(int id)
    {
        var ferias = await _context.Ferias.FindAsync(id);
        if (ferias == null) return NotFound();

        if (ferias.Status == StatusFerias.EmFerias)
            return BadRequest("Não é possível excluir um registro em andamento.");

        _context.Ferias.Remove(ferias);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}