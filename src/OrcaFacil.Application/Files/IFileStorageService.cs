namespace OrcaFacil.Application.Files;

public sealed record StoredFile(string StoredFileName, string RelativePath, long SizeInBytes, string Sha256Hash);

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Guid accountId, string originalFileName, Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}

public static class FileUploadPolicy
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".csv", ".txt" };

    public static string ValidateAndGetExtension(string fileName, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            throw new ArgumentException("Nome de arquivo inválido.", nameof(fileName));
        if (length <= 0 || length > MaximumBytes) throw new ArgumentException("O arquivo deve ter entre 1 byte e 10 MB.", nameof(length));
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new ArgumentException("Extensão de arquivo não permitida.", nameof(fileName));
        return extension;
    }
}
