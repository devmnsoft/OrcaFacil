using System.Security.Cryptography;
using OrcaFacil.Application.Files;

namespace OrcaFacil.Infrastructure.Files;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    public LocalFileStorageService(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Guid accountId, string originalFileName, Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = FileUploadPolicy.ValidateAndGetExtension(originalFileName, content.CanSeek ? content.Length : 1);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relative = Path.Combine(accountId.ToString("N"), DateTime.UtcNow.ToString("yyyyMM"), storedName);
        var fullPath = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var hash = SHA256.Create();
        var buffer = new byte[81920]; long total = 0; int read;
        while ((read = await content.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > FileUploadPolicy.MaximumBytes) { output.Close(); File.Delete(fullPath); throw new ArgumentException("O arquivo excede 10 MB."); }
            await output.WriteAsync(buffer.AsMemory(0, read), ct); hash.TransformBlock(buffer, 0, read, null, 0);
        }
        if (total == 0) { output.Close(); File.Delete(fullPath); throw new ArgumentException("Arquivo vazio não é permitido."); }
        hash.TransformFinalBlock([], 0, 0);
        return new(storedName, relative.Replace(Path.DirectorySeparatorChar, '/'), total, Convert.ToHexString(hash.Hash!).ToLowerInvariant());
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));
    public Task DeleteAsync(string relativePath, CancellationToken ct = default) { File.Delete(Resolve(relativePath)); return Task.CompletedTask; }
    private string Resolve(string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new UnauthorizedAccessException("Caminho de armazenamento inválido.");
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Caminho fora do armazenamento privado.");
        return full;
    }
}
