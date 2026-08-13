using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum DataImportType { Clients, Services }
public enum DataImportStatus { Uploaded, Validating, ReadyToImport, Imported, Failed, Canceled, RolledBack }

public sealed class DataImport : Entity
{
    public Guid AccountId { get; set; }
    public DataImportType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DataImportStatus Status { get; set; } = DataImportStatus.Uploaded;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int FailedRows { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Summary { get; set; }
    public string? StagedRowsJson { get; set; }
    public string? ErrorsJson { get; set; }
}
