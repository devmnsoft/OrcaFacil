using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class UserUsage : Entity
{
    public Guid? AccountId { get; set; }
    public Guid UserId { get; set; }
    public string Period { get; set; } = string.Empty;
    public int DocumentsCreated { get; set; }
    public int BudgetsCreated { get; set; }
    public int ReceiptsCreated { get; set; }
    public int PdfGenerated { get; set; }
    public int PublicLinksCreated { get; set; }
    public int BackupExports { get; set; }
    public int ChatbotQuestions { get; set; }
}
