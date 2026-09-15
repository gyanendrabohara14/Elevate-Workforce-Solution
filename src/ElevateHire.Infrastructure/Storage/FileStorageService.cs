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

        return (Path.Combine(subFolder, uniqueName), uniqueName);
    }

    public Task DeleteAsync(string storedName, string subFolder)
    {
        var fullPath = Path.Combine(_options.RootPath, storedName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string ResolvePath(string storedName, string subFolder) =>
        Path.Combine(_options.RootPath, storedName);
}

public sealed class FileStorageOptions
{
    public string RootPath { get; set; } = "wwwroot/uploads";
}