namespace OrcaFacil.Application.Abstractions;

/// <summary>Creates non-reversible, keyed fingerprints for technical abuse-prevention data.</summary>
public interface ITechnicalFingerprintService
{
    string Create(string value);
}
