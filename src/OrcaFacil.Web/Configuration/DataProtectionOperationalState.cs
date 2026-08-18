namespace OrcaFacil.Web.Configuration;

/// <summary>Exposes only non-secret Data Protection diagnostics to protected operational pages.</summary>
public sealed record DataProtectionOperationalState(string KeysPath, bool IsPersistent);
