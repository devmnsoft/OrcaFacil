using System;
using OrcaFacil.Application.Accounts;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

// ============================================================================
// CONTRATO DOS CONVITES DE EQUIPE POR E-MAIL (AccountInvitation)
//
// Regras puras em OrcaFacil.Application.Accounts.AccountInvitationRules +
// comportamento da entidade: token de uso ÚNICO com expiração, revogação que
// expira sem apagar histórico e cânon do limite de vagas — convite pendente
// OCUPO vaga, para o limite do plano nunca ser ultrapassado.
// Persistência, hash do token cru e outbox são testados no fluxo Web
// (mesma disciplina dos avisos de cobrança).
// ============================================================================

public sealed class AccountInvitationRulesTests
{
    // ---------- Cânon do limite (TeamCapReached) ----------

    [Fact]
    public void Sem_limite_configurado_nunca_bloqueia()
    {
        Assert.False(AccountInvitationRules.TeamCapReached(limit: null, activeMembers: 999, pendingInvitations: 999));
    }

    [Fact]
    public void Vagas_disponiveis_permitem_convite()
    {
        Assert.False(AccountInvitationRules.TeamCapReached(limit: 3, activeMembers: 1, pendingInvitations: 1));
    }

    [Fact]
    public void Exatamente_no_limite_bloqueia()
    {
        Assert.True(AccountInvitationRules.TeamCapReached(limit: 3, activeMembers: 3, pendingInvitations: 0));
    }

    [Fact]
    public void Convite_pendente_ocupa_vaga()
    {
        // 2 ativos + 1 pendente com limite 3: o próximo convite (4.ª vaga) é bloqueado.
        Assert.True(AccountInvitationRules.TeamCapReached(limit: 3, activeMembers: 2, pendingInvitations: 1));
    }

    [Fact]
    public void Acima_do_limite_bloqueia()
    {
        Assert.True(AccountInvitationRules.TeamCapReached(limit: 2, activeMembers: 5, pendingInvitations: 0));
    }

    // ---------- Perfis atribuíveis ----------

    [Fact]
    public void Perfil_atribuivel_passa()
    {
        var erro = AccountInvitationRules.ValidateRole("ReadOnly", new[] { "ReadOnly", "Member" });
        Assert.Null(erro);
    }

    [Fact]
    public void Perfil_nao_atribuivel_rejeita_com_mensagem()
    {
        var erro = AccountInvitationRules.ValidateRole("Owner", new[] { "ReadOnly", "Member" });
        Assert.False(string.IsNullOrWhiteSpace(erro));
    }

    // ---------- Contrato do token ----------

    [Fact]
    public void Convite_valido_sessenta_e_sete_dias_oito_horas()
    {
        // Validade anunciada: 7 dias. Contrato travado para não encolher silenciosamente.
        Assert.Equal(7, AccountInvitationRules.ValidityDays);
    }
}

public sealed class AccountInvitationEntityTests
{
    private static AccountInvitation NovoConvite(DateTime criadoEm) => new()
    {
        AccountId = Guid.NewGuid(),
        Email = "convidada@example.com",
        FullName = "Convidada de Exemplo",
        RoleCode = "Member",
        InvitedByUserId = Guid.NewGuid(),
        TokenHash = new string('a', 64),
        ExpiresAt = criadoEm.AddDays(7)
    };

    [Fact]
    public void Convite_novo_esta_disponivel_antes_de_expirar()
    {
        var now = DateTime.UtcNow;
        var invite = NovoConvite(now);
        Assert.True(invite.IsAvailable(now));
        Assert.True(invite.IsAvailable(now.AddDays(6)));
    }

    [Fact]
    public void Convite_expirado_nao_esta_disponivel()
    {
        var now = DateTime.UtcNow;
        var invite = NovoConvite(now.AddDays(-8));
        Assert.False(invite.IsAvailable(now));
    }

    [Fact]
    public void Aceite_e_unico_segundo_uso_nao_disponivel()
    {
        var now = DateTime.UtcNow;
        var invite = NovoConvite(now);
        var quemAceitou = Guid.NewGuid();
        invite.Use(now.AddDays(1), quemAceitou);
        Assert.Equal(quemAceitou, invite.UsedByUserId);
        Assert.NotNull(invite.UsedAt);
        Assert.False(invite.IsAvailable(now.AddDays(1)));
        // Uso repetido (double-claim em corrida) é recusado:
        Assert.Throws<InvalidOperationException>(() => invite.Use(now.AddDays(1), Guid.NewGuid()));
    }

    [Fact]
    public void Convite_usado_nao_pode_ser_expirado_por_revoke()
    {
        // Histórico preservado: aceite registrado nunca é "desfeito" por revogação.
        var now = DateTime.UtcNow;
        var invite = NovoConvite(now.AddDays(-1));
        invite.Use(now, Guid.NewGuid());
        var expiresAntes = invite.ExpiresAt;
        invite.Revoke(now.AddDays(1));
        Assert.Equal(expiresAntes, invite.ExpiresAt);
    }

    [Fact]
    public void Revoke_de_convite_pendente_expira_no_ato_sem_apagar()
    {
        var now = DateTime.UtcNow;
        var invite = NovoConvite(now);
        Assert.False(invite.IsDeleted);
        invite.Revoke(now.AddDays(1));
        Assert.False(invite.IsAvailable(now.AddDays(1)));
        Assert.Equal(now.AddDays(1), invite.ExpiresAt);
        // Sem DELETE físico: o registro continua lá para auditoria.
        Assert.False(invite.IsDeleted);
    }
}
