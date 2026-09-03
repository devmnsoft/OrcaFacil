namespace OrcaFacil.Web.Services;

/// <summary>Human-readable, actionable copy for feedback that may be presented to end users.</summary>
public static class UserFeedbackMessageCatalog
{
    public const string Saved = "Tudo certo: as informações foram salvas. Você já pode continuar.";
    public const string Updated = "Alterações salvas com sucesso. Os dados exibidos já estão atualizados.";
    public const string Deleted = "Registro excluído com sucesso. O histórico permitido foi preservado.";
    public const string Cancelled = "A ação foi cancelada. Nenhuma alteração foi realizada.";
    public const string PermissionDenied = "Você não tem permissão para concluir esta ação. Solicite acesso a um administrador.";
    public const string NotFound = "Não encontramos este registro. Volte à lista e escolha outro item.";
    public const string Duplicate = "Já existe um cadastro com estes dados. Revise o registro encontrado antes de continuar.";
    public const string IntegrationMissing = "Esta integração ainda não está configurada. Abra Configurações para concluir a conexão.";
    public const string SchemaOutdated = "A estrutura de dados precisa ser atualizada. Consulte o System Health antes de tentar novamente.";
    public const string TemporaryError = "Não foi possível concluir agora. Aguarde um instante e tente novamente.";
    public const string CriticalAction = "Revise o impacto desta ação. Os dados que puderem ser preservados continuarão no histórico.";
    public const string EmptyState = "Ainda não há itens por aqui. Use a ação principal para começar.";
    public const string LoadingState = "Estamos preparando as informações. Isso pode levar alguns segundos.";
}

public static class ValidationMessageCatalog
{
    public const string ReviewHighlightedFields = "Não foi possível salvar porque há campos obrigatórios pendentes. Revise os campos destacados e tente novamente.";
    public const string Required = "Informe {0} para continuar.";
    public const string InvalidValue = "Revise {0}: o valor informado não está no formato esperado.";
    public const string EndBeforeStart = "A data final não pode ser menor que a data inicial.";
    public const string ClientRequired = "Informe o nome do cliente para continuar.";
    public const string ServiceRequired = "Selecione um serviço antes de salvar o orçamento.";
}

public sealed class FriendlyMessageService(ILogger<FriendlyMessageService> logger)
{
    public string ForKnownFailure(Exception exception, string operation, string? correlationId = null)
    {
        logger.LogError(exception, "User operation {Operation} failed. CorrelationId: {CorrelationId}", operation, correlationId);
        return string.IsNullOrWhiteSpace(correlationId)
            ? UserFeedbackMessageCatalog.TemporaryError
            : $"{UserFeedbackMessageCatalog.TemporaryError} Código de atendimento: {correlationId}.";
    }
}
