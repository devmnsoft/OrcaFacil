namespace OrcaFacil.Application.Accounts;

/// <summary>
/// Regras puras dos convites de equipe por e-mail (sem dependências, testáveis
/// isoladamente): validade do token, limite de pessoas do plano e perfis
/// atribuíveis. A política é intencionalmente conservadora — convite pendente
/// também ocupa vaga, para o limite nunca ser ultrapassado.
/// </summary>
public static class AccountInvitationRules
{
    /// <summary>Validade padrão do convite: link único durante 7 dias.</summary>
    public const int ValidityDays = 7;

    public static string? ValidateRole(string roleCode, IReadOnlyCollection<string> assignableRoles) =>
        assignableRoles.Contains(roleCode) ? null : "Você não pode atribuir este perfil.";

    /// <summary>
    /// Limite de pessoas atingido? Membros ativos + convites pendentes conta
    /// para a vaga; sem limite configurado no plano, nunca há bloqueio.
    /// </summary>
    public static bool TeamCapReached(int? limit, int activeMembers, int pendingInvitations) =>
        limit.HasValue && activeMembers + pendingInvitations >= limit.Value;
}
