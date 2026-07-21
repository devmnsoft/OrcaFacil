using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class IssuerProfile : Entity
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PixKey { get; set; }
    public string? LogoPath { get; set; }
}
