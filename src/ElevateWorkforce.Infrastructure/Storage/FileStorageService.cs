using ElevateWorkforce.Application.Interfaces;

namespace ElevateWorkforce.Infrastructure.Storage;

public interface IFileStorageService
{
    Task<(string StoredName, string UniqueName)> SaveAsync(Stream stream, string originalFileName, string subFolder, string[] allowedExtensions, long maxBytes);
    Task DeleteAsync(string storedName, string subFolder);
    string ResolvePath(string storedName, string subFolder);
}

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;

    public FileStorageService(FileStorageOptions options) => _options = options;

    public async Task<(string StoredName, string UniqueName)> SaveAsync(
        Stream stream, string originalFileName, string subFolder, string[] allowedExtensions, long maxBytes)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            throw new InvalidOperationException("File type is not permitted.");

        if (stream.Length > maxBytes)
            throw new InvalidOperationException($"File exceeds the {maxBytes / (1024 * 1024)} MB limit.");

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var directory = Path.Combine(_options.RootPath, subFolder);
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, uniqueName);

        await using var file = File.Create(fullPath);
        await stream.CopyToAsync(file);

        var storedName = string.Join('/', new[] { subFolder.Trim('/', '\\'), uniqueName })
            .Replace('\\', '/');
        return (storedName, uniqueName);
    }

    public Task DeleteAsync(string storedName, string subFolder)
    {
        var fullPath = ResolvePath(storedName, subFolder);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string ResolvePath(string storedName, string subFolder)
    {
        var normalizedStoredName = storedName.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_options.RootPath, normalizedStoredName.TrimStart(Path.DirectorySeparatorChar));
    }
}

public sealed class FileStorageOptions
{
    public string RootPath { get; set; } = "wwwroot/uploads";
}