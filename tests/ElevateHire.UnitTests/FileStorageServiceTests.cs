using ElevateWorkforce.Infrastructure.Storage;

namespace ElevateWorkforce.UnitTests;

public class FileStorageServiceTests : IDisposable
{
    private readonly string _root;

    public FileStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "elevateworkforce-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private FileStorageService CreateService() =>
        new(new FileStorageOptions { RootPath = _root });

    private static MemoryStream Stream(byte[] content) => new(content);

    [Fact]
    public async Task SaveAsync_StoresFile_AndReturnsPath()
    {
        var service = CreateService();
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        var (storedName, uniqueName) = await service.SaveAsync(
            Stream(bytes), "resume.pdf", "resumes", new[] { ".pdf" }, 5 * 1024 * 1024);

        Assert.StartsWith("resumes/", storedName);
        Assert.EndsWith(".pdf", uniqueName);
        var fullPath = service.ResolvePath(storedName, "resumes");
        Assert.True(File.Exists(fullPath));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(fullPath));
    }

    [Fact]
    public async Task SaveAsync_RejectsForbiddenExtension()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(Stream(new byte[] { 1 }), "resume.exe", "resumes", new[] { ".pdf" }, 5 * 1024 * 1024));
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var service = CreateService();
        var big = new byte[6 * 1024 * 1024];

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(Stream(big), "resume.pdf", "resumes", new[] { ".pdf" }, 5 * 1024 * 1024));
    }

    [Fact]
    public async Task DeleteAsync_RemovesStoredFile()
    {
        var service = CreateService();
        var (storedName, _) = await service.SaveAsync(
            Stream(new byte[] { 1 }), "doc.doc", "uploads", new[] { ".doc" }, 5 * 1024 * 1024);

        await service.DeleteAsync(storedName, "uploads");

        Assert.False(File.Exists(service.ResolvePath(storedName, "uploads")));
    }
}