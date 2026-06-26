using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SistemaFerias.Infrastructure.Services;

public interface IActiveDirectoryService
{
    bool BloquearConta(string loginAd);
    bool ReativarConta(string loginAd);
    bool UsuarioExiste(string loginAd);
}

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly bool _useMock;
    private readonly string _domain;
    private readonly string _container;
    private readonly string _adminUser;
    private readonly string _adminPassword;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(
        IConfiguration configuration,
        ILogger<ActiveDirectoryService> logger)
    {
        _logger = logger;
        _useMock = bool.TryParse(configuration["ActiveDirectory:UseMock"], out var mock) && mock;
        _domain        = configuration["ActiveDirectory:Domain"]        ?? throw new InvalidOperationException("AD Domain não configurado.");
        _container     = configuration["ActiveDirectory:Container"]     ?? throw new InvalidOperationException("AD Container não configurado.");
        _adminUser     = configuration["ActiveDirectory:AdminUser"]     ?? throw new InvalidOperationException("AD AdminUser não configurado.");
        _adminPassword = configuration["ActiveDirectory:AdminPassword"] ?? throw new InvalidOperationException("AD AdminPassword não configurado.");
    }

    public bool BloquearConta(string loginAd)
        => AlterarStatusConta(loginAd, desabilitar: true);

    public bool ReativarConta(string loginAd)
        => AlterarStatusConta(loginAd, desabilitar: false);

    public bool UsuarioExiste(string loginAd)
    {
        if (_useMock)
        {
            _logger.LogInformation("[MOCK] Usuário {Login} encontrado no AD.", loginAd);
            return true;
        }

        try
        {
            using var context = new PrincipalContext(
                ContextType.Domain,
                _domain,
                _container,
                _adminUser,
                _adminPassword);

            using var usuario = UserPrincipal.FindByIdentity(
                context,
                IdentityType.SamAccountName,
                loginAd);

            return usuario != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência do usuário {Login} no AD.", loginAd);
            return false;
        }
    }

    private bool AlterarStatusConta(string loginAd, bool desabilitar)
    {
        if (_useMock)
        {
            _logger.LogInformation(
                "[MOCK] Conta {Login} {Acao} no AD.",
                loginAd,
                desabilitar ? "bloqueada" : "reativada");
            return true;
        }

        try
        {
            using var context = new PrincipalContext(
                ContextType.Domain,
                _domain,
                _container,
                _adminUser,
                _adminPassword);

            using var usuario = UserPrincipal.FindByIdentity(
                context,
                IdentityType.SamAccountName,
                loginAd);

            if (usuario == null)
            {
                _logger.LogWarning("Usuário {Login} não encontrado no AD.", loginAd);
                return false;
            }

            usuario.Enabled = !desabilitar;
            usuario.Save();

            _logger.LogInformation(
                "Conta {Login} {Acao} no AD com sucesso.",
                loginAd,
                desabilitar ? "bloqueada" : "reativada");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao {Acao} conta {Login} no AD.",
                desabilitar ? "bloquear" : "reativar", loginAd);
            return false;
        }
    }
}