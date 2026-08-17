using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class OnboardingApplicationService(
    OrcaFacilDbContext db,
    ICurrentAccountService current,
    IAuditService audit) : IOnboardingApplicationService
{
    private async Task<(AccountOnboardingState? State, OperationResult? Error)> Resolve(CancellationToken ct)
    {
        await current.EnsureAccountAccessAsync(ct);

        if (current.AccountId is not Guid accountId)
        {
            return (null, OperationResult.Failure(
                "access_denied",
                "Não foi possível identificar seu espaço."));
        }

        // The unique account/user index is the concurrency boundary. An upsert avoids two
        // simultaneous first requests creating duplicate onboarding rows.
        const string initialStep = "Welcome";
        var now = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO orcafacil.account_onboarding_states
                (id, account_id, user_id, current_step, last_seen_at, created_at, is_deleted)
            VALUES
                ({Guid.NewGuid()}, {accountId}, {current.UserId}, {initialStep}, {now}, {now}, false)
            ON CONFLICT (account_id, user_id) DO UPDATE
                SET is_deleted = false,
                    last_seen_at = EXCLUDED.last_seen_at,
                    updated_at = EXCLUDED.last_seen_at
            """, ct);

        var state = await db.AccountOnboardingStates.SingleAsync(
            x => x.AccountId == accountId &&
                 x.UserId == current.UserId &&
                 !x.IsDeleted,
            ct);

        state.LastSeenAt = DateTime.UtcNow;
        return (state, null);
    }

    public async Task<OperationResult<OnboardingStateView>> GetAsync(CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return OperationResult<OnboardingStateView>.Failure(error.Code!, error.Message!);
        }

        await db.SaveChangesAsync(ct);

        var client = await db.Clients
            .Where(x => x.AccountId == state!.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        var service = await db.ServiceCatalogItems
            .Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        var clientAt = await db.Clients.Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var serviceAt = await db.ServiceCatalogItems.Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var budgetAt = await db.Documents.Where(x => x.AccountId == state.AccountId &&
                x.Type == DocumentType.Budget && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var sentBudgetAt = await db.DocumentRevisions.Where(x => x.AccountId == state.AccountId &&
                (x.Status == DocumentRevisionStatus.Sent || x.SentAt != null) && !x.IsDeleted)
            .OrderBy(x => x.SentAt ?? x.CreatedAt).Select(x => (DateTime?)(x.SentAt ?? x.CreatedAt)).FirstOrDefaultAsync(ct);
        var decisionAt = await db.PublicDocumentDecisions.Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var paymentAt = await db.ManualPayments.Where(x => x.AccountId == state.AccountId &&
                x.Status == FinancialRecordStatus.Active && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var receiptAt = await db.Receipts.Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var activation = new ActivationStepView[]
        {
            new("company", "Complete os dados da empresa", "Esses dados identificam sua empresa nos documentos.", "/Profile/Index", "Completar empresa", state.BusinessProfileCompletedAt.HasValue && state.IssuerProfileCompletedAt.HasValue, state.IssuerProfileCompletedAt),
            new("client", "Cadastre o primeiro cliente", "Organize contato e dados usados na proposta.", "/Clients/Create", "Cadastrar cliente", client.HasValue, state.FirstClientCompletedAt ?? clientAt),
            new("service", "Cadastre o primeiro serviço", "Monte seu catálogo com preço e prazo padrão.", "/Services/Create", "Cadastrar serviço", service.HasValue, state.FirstServiceCompletedAt ?? serviceAt),
            new("budget", "Crie o primeiro orçamento", "Reúna cliente, serviços e condições comerciais.", "/Documents/New", "Criar orçamento", budgetAt.HasValue, state.FirstBudgetCompletedAt ?? budgetAt),
            new("send", "Gere o link público", "Compartilhe uma versão registrada com o cliente.", "/Documents/Index", "Abrir orçamentos", sentBudgetAt.HasValue, sentBudgetAt),
            new("approval", "Acompanhe a aprovação", "Veja a resposta registrada pelo cliente.", "/Documents/Index", "Acompanhar propostas", decisionAt.HasValue, decisionAt),
            new("payment", "Registre o pagamento", "Mantenha o financeiro ligado ao trabalho realizado.", "/Payments/Index", "Abrir pagamentos", paymentAt.HasValue, paymentAt),
            new("receipt", "Emita o recibo", "Formalize um pagamento que já foi registrado.", "/Receipts/Index", "Abrir recibos", receiptAt.HasValue, receiptAt)
        };
        return OperationResult<OnboardingStateView>.Success(new OnboardingStateView(
            state.CurrentStep,
            activation.Count(x => x.IsCompleted),
            activation.Length,
            state.CompletedAt.HasValue,
            state.SkippedAt,
            Next(state.CurrentStep),
            client,
            service,
            activation));
    }

    public async Task<OperationResult> BeginAsync(CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return error;
        }

        if (state!.CompletedAt.HasValue)
        {
            return OperationResult.Success();
        }

        state.Advance(OnboardingStep.BusinessProfile);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SkipAsync(OnboardingStep step, CancellationToken ct = default)
    {
        if (step is OnboardingStep.BusinessProfile or OnboardingStep.DocumentIdentity)
        {
            return OperationResult.Failure(
                "required_step",
                "Esta etapa prepara os dados obrigatórios do orçamento.");
        }

        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return error;
        }

        state!.Skip();
        state.Advance(step == OnboardingStep.Welcome
            ? OnboardingStep.Welcome
            : (OnboardingStep)((int)step + 1));
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SaveBusinessAsync(
        BusinessProfileInput input,
        CancellationToken ct = default)
    {
        var errors = ValidateBusiness(input);
        if (errors.Count > 0)
        {
            return OperationResult.Failure(
                "validation",
                "Revise os campos destacados.",
                errors.ToArray());
        }

        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return error;
        }

        var account = await db.BusinessAccounts.SingleAsync(
            x => x.Id == state!.AccountId && !x.IsDeleted,
            ct);
        account.PersonType = input.PersonType;
        account.DisplayName = input.Name.Trim();
        account.DocumentType = input.PersonType == PersonType.Company
            ? BrazilianDocumentType.CNPJ
            : BrazilianDocumentType.CPF;
        account.DocumentNumber = BrazilianDocument.Normalize(input.Document);
        account.Phone = Digits(input.Phone);
        account.Email = input.Email.Trim().ToLowerInvariant();
        account.Touch();

        var billing = await db.BillingCustomerProfiles.SingleOrDefaultAsync(
            x => x.AccountId == account.Id && !x.IsDeleted,
            ct);
        if (billing is not null)
        {
            billing.Name = account.DisplayName;
            billing.PersonType = input.PersonType;
            billing.DocumentType = account.DocumentType;
            billing.DocumentNumber = account.DocumentNumber;
            billing.Phone = account.Phone;
            billing.Email = account.Email;
            billing.City = input.City.Trim();
            billing.State = input.State.Trim().ToUpperInvariant();
            billing.Touch();
        }

        state.BusinessProfileCompletedAt = DateTime.UtcNow;
        state.Advance(OnboardingStep.DocumentIdentity);
        await db.SaveChangesAsync(ct);
        await audit.RegisterAsync(
            current.UserId,
            "ONBOARDING_BUSINESS_COMPLETED",
            nameof(BusinessAccount),
            account.Id.ToString(),
            null,
            new { account.PersonType },
            null,
            ct,
            account.Id);

        return OperationResult.Success("Dados do negócio salvos.");
    }

    public async Task<OperationResult> SaveDocumentIdentityAsync(
        DocumentIdentityInput input,
        CancellationToken ct = default)
    {
        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(input.DisplayName))
        {
            errors.Add(new FieldError(
                "Input.DisplayName",
                "Informe o nome exibido no orçamento."));
        }

        if (!string.IsNullOrWhiteSpace(input.Email) && !ValidEmail(input.Email))
        {
            errors.Add(new FieldError("Input.Email", "Informe um e-mail válido."));
        }

        if (!string.IsNullOrWhiteSpace(input.Phone) && Digits(input.Phone)?.Length < 10)
        {
            errors.Add(new FieldError("Input.Phone", "Informe um telefone válido."));
        }

        if (errors.Count > 0)
        {
            return OperationResult.Failure(
                "validation",
                "Revise os campos destacados.",
                errors.ToArray());
        }

        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return error;
        }

        var issuer = await db.IssuerProfiles.SingleOrDefaultAsync(
            x => x.UserId == current.UserId && !x.IsDeleted,
            ct) ?? new IssuerProfile { UserId = current.UserId };
        if (db.Entry(issuer).State == EntityState.Detached)
        {
            db.Add(issuer);
        }

        issuer.BusinessName = input.DisplayName.Trim();
        issuer.Phone = Digits(input.Phone);
        issuer.Email = input.Email?.Trim().ToLowerInvariant();
        issuer.City = input.City?.Trim();
        issuer.Address = input.Address?.Trim();
        issuer.PixKey = input.PixKey?.Trim();
        issuer.Touch();

        state!.IssuerProfileCompletedAt = DateTime.UtcNow;
        state.Advance(OnboardingStep.FirstClient);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success("Identidade do documento salva.");
    }

    public async Task<OperationResult<Guid>> CreateClientAsync(
        FirstClientInput input,
        CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return OperationResult<Guid>.Failure(error.Code!, error.Message!);
        }

        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add(new FieldError("Input.Name", "Informe o nome do cliente."));
        }

        if (!string.IsNullOrWhiteSpace(input.Email) && !ValidEmail(input.Email))
        {
            errors.Add(new FieldError("Input.Email", "Informe um e-mail válido."));
        }

        var document = BrazilianDocument.Normalize(input.Document);
        var documentType = input.PersonType == PersonType.Company
            ? BrazilianDocumentType.CNPJ
            : BrazilianDocumentType.CPF;
        if (document is not null && !BrazilianDocument.HasValidCheckDigits(documentType, document))
        {
            errors.Add(new FieldError("Input.Document", "Informe um CPF ou CNPJ válido."));
        }

        if (errors.Count > 0)
        {
            return OperationResult<Guid>.Failure(
                "validation",
                "Revise os campos destacados.",
                errors.ToArray());
        }

        var normalizedName = input.Name.Trim();
        var duplicate = await db.Clients.AnyAsync(
            x => x.AccountId == state!.AccountId &&
                 !x.IsDeleted &&
                 (x.Name.ToLower() == normalizedName.ToLower() ||
                  (document != null && x.DocumentNumber == document)),
            ct);
        if (duplicate)
        {
            return OperationResult<Guid>.Failure(
                "duplicate",
                "Este cliente já está cadastrado.",
                new FieldError(
                    "Input.Name",
                    "Use outro nome ou documento."));
        }

        var client = new Client
        {
            AccountId = state!.AccountId,
            UserId = current.UserId,
            Name = normalizedName,
            PersonType = input.PersonType,
            DocumentType = documentType,
            DocumentNumber = document,
            Phone = Digits(input.Phone),
            Email = input.Email?.Trim().ToLowerInvariant(),
            City = input.City?.Trim()
        };
        db.Add(client);

        if (!string.IsNullOrWhiteSpace(input.Phone))
        {
            db.Add(new ClientContact
            {
                AccountId = state.AccountId,
                ClientId = client.Id,
                Name = client.Name,
                ContactType = ClientContactType.Phone,
                Value = Digits(input.Phone)!,
                IsPrimary = true,
                ReceivesQuotes = true
            });
        }

        state.FirstClientCompletedAt = DateTime.UtcNow;
        state.Advance(OnboardingStep.FirstService);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(client.Id);
    }

    public async Task<OperationResult<Guid>> CreateServiceAsync(
        FirstServiceInput input,
        CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return OperationResult<Guid>.Failure(error.Code!, error.Message!);
        }

        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add(new FieldError("Input.Name", "Informe o nome do serviço."));
        }

        if (string.IsNullOrWhiteSpace(input.Unit))
        {
            errors.Add(new FieldError("Input.Unit", "Informe a unidade."));
        }

        if (input.Price < 0)
        {
            errors.Add(new FieldError("Input.Price", "O preço não pode ser negativo."));
        }

        if (input.Cost is < 0)
        {
            errors.Add(new FieldError("Input.Cost", "O custo não pode ser negativo."));
        }

        if (errors.Count > 0)
        {
            return OperationResult<Guid>.Failure(
                "validation",
                "Revise os campos destacados.",
                errors.ToArray());
        }

        var normalizedName = input.Name.Trim();
        var duplicate = await db.ServiceCatalogItems.AnyAsync(
            x => x.AccountId == state!.AccountId &&
                 !x.IsDeleted &&
                 x.Name.ToLower() == normalizedName.ToLower(),
            ct);
        if (duplicate)
        {
            return OperationResult<Guid>.Failure(
                "duplicate",
                "Este serviço já está cadastrado.",
                new FieldError("Input.Name", "Escolha outro nome."));
        }

        var item = new ServiceCatalogItem
        {
            AccountId = state!.AccountId,
            Name = normalizedName,
            Description = input.Description?.Trim(),
            UnitCode = input.Unit.Trim().ToLowerInvariant(),
            StandardPrice = input.Price,
            EstimatedCost = input.Cost ?? 0,
            SuggestedDurationMinutes = input.DurationMinutes
        };
        db.Add(item);

        state.FirstServiceCompletedAt = DateTime.UtcNow;
        state.Advance(OnboardingStep.FirstBudget);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult<Guid?>> StartBudgetAsync(CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return OperationResult<Guid?>.Failure(error.Code!, error.Message!);
        }

        state!.FirstBudgetStartedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var client = await db.Clients
            .Where(x => x.AccountId == state.AccountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        return OperationResult<Guid?>.Success(client);
    }

    public async Task<OperationResult> CompleteAsync(CancellationToken ct = default)
    {
        var (state, error) = await Resolve(ct);
        if (error is not null)
        {
            return error;
        }

        if (state!.BusinessProfileCompletedAt is null || state.IssuerProfileCompletedAt is null)
        {
            return OperationResult.Failure(
                "required_step",
                "Conclua os dados do negócio e do documento.");
        }

        state.Complete();
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private static List<FieldError> ValidateBusiness(BusinessProfileInput input)
    {
        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            errors.Add(new FieldError("Input.Name", "Informe seu nome profissional."));
        }

        var documentType = input.PersonType == PersonType.Company
            ? BrazilianDocumentType.CNPJ
            : BrazilianDocumentType.CPF;
        if (!BrazilianDocument.HasValidCheckDigits(
                documentType,
                BrazilianDocument.Normalize(input.Document)))
        {
            errors.Add(new FieldError("Input.Document", "Informe um CPF ou CNPJ válido."));
        }

        if (!ValidEmail(input.Email))
        {
            errors.Add(new FieldError("Input.Email", "Informe um e-mail válido."));
        }

        if (Digits(input.Phone)?.Length < 10)
        {
            errors.Add(new FieldError("Input.Phone", "Informe um telefone válido."));
        }

        if (input.State?.Trim().Length != 2)
        {
            errors.Add(new FieldError("Input.State", "Informe uma UF válida."));
        }

        return errors;
    }

    private static bool ValidEmail(string? value)
    {
        try
        {
            return new System.Net.Mail.MailAddress(value ?? string.Empty).Address ==
                   (value ?? string.Empty).Trim();
        }
        catch
        {
            return false;
        }
    }

    private static string? Digits(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new string(value.Where(char.IsDigit).ToArray());

    private static NextActionDescriptor Next(OnboardingStep step) => step switch
    {
        OnboardingStep.Welcome => new NextActionDescriptor(
            "begin",
            "Começar configuração",
            "Leva cerca de cinco minutos.",
            "/Onboarding/Index"),
        OnboardingStep.BusinessProfile => new NextActionDescriptor(
            "business",
            "Dados do negócio",
            "Prepare os dados básicos.",
            "/Onboarding/Business"),
        OnboardingStep.DocumentIdentity => new NextActionDescriptor(
            "identity",
            "Identidade do documento",
            "Defina como sua marca aparece.",
            "/Onboarding/DocumentIdentity"),
        OnboardingStep.FirstClient => new NextActionDescriptor(
            "client",
            "Primeiro cliente",
            "Cadastre ou pule com segurança.",
            "/Onboarding/Client"),
        OnboardingStep.FirstService => new NextActionDescriptor(
            "service",
            "Primeiro serviço",
            "Monte seu catálogo.",
            "/Onboarding/Service"),
        OnboardingStep.FirstBudget => new NextActionDescriptor(
            "budget",
            "Primeiro orçamento",
            "Revise e crie sua proposta.",
            "/Onboarding/Budget"),
        _ => new NextActionDescriptor(
            "dashboard",
            "Ir ao dashboard",
            "Seu espaço está pronto.",
            "/Dashboard/Index")
    };
}
