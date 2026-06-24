namespace SistemaFerias.Api.DTOs;

public class UpdateFeriasDto
{
    public string NomeFuncionario { get; set; } = string.Empty;

    public string LoginAd { get; set; } = string.Empty;

    public DateTime DataInicio { get; set; }

    public DateTime DataRetorno { get; set; }
}