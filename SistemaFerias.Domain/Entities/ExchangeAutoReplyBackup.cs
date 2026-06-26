using System;

namespace SistemaFerias.Domain.Entities;

public class ExchangeAutoReplyBackup
{
    public int Id { get; set; }

    public int FeriasId { get; set; }
    public Ferias? Ferias { get; set; }

    public string LoginAd { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string ExternalAudience { get; set; } = "None";

    public string InternalReply { get; set; } = string.Empty;

    public string ExternalReply { get; set; } = string.Empty;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime DataBackup { get; set; } = DateTime.UtcNow;

    public bool BackupRestaurado { get; set; }

    public DateTime? DataRestauracao { get; set; }
}