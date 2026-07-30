namespace OrcaFacil.Application.Abstractions;

/// <summary>Creates non-reversible, deployment-specific fingerprints for security telemetry.</summary>
public interface ITechnicalFingerprintService
{
    string Create(string value);
}
