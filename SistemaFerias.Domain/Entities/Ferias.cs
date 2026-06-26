using SistemaFerias.Domain.Enums;

namespace SistemaFerias.Domain.Entities;

public class Ferias
{
    public int Id { get; set; }

    public string NomeFuncionario { get; set; } = string.Empty;

    public string LoginAd { get; set; } = string.Empty;

    public DateTime DataInicio { get; set; }

    public DateTime DataRetorno { get; set; }

    public StatusFerias Status { get; set; }

    public DateTime? DataEntradaFerias { get; set; }

    public DateTime? DataFinalizacaoFerias { get; set; }

    public DateTime? DELETED_AT { get; set; }
}