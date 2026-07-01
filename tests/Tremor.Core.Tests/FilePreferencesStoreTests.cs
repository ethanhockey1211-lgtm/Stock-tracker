using Tremor.Core.Services.Persistence;
using Xunit;

namespace Tremor.Core.Tests;

public sealed class FilePreferencesStoreTests : IDisposable
{
    private readonly string _dir;

    public FilePreferencesStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tremor-tests-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }

    [Fact]
    public async Task GetReturnsNullForMissingKey()
    {
        var store = new FilePreferencesStore(_dir);
        Assert.Null(await store.GetAsync("nope"));
    }

    [Fact]
    public async Task SetThenGetRoundTrips()
    {
        var store = new FilePreferencesStore(_dir);
        await store.SetAsync("k", "v");
        Assert.Equal("v", await store.GetAsync("k"));
    }

    [Fact]
    public async Task PersistsAcrossInstances()
    {
        var store1 = new FilePreferencesStore(_dir);
        await store1.SetAsync("k", "persisted");

        var store2 = new FilePreferencesStore(_dir);
        Assert.Equal("persisted", await store2.GetAsync("k"));
    }

    [Fact]
    public async Task RemoveDeletesKey()
    {
        var store = new FilePreferencesStore(_dir);
        await store.SetAsync("k", "v");
        await store.RemoveAsync("k");
        Assert.Null(await store.GetAsync("k"));
    }

    [Fact]
    public async Task OverwritesExistingValue()
    {
        var store = new FilePreferencesStore(_dir);
        await store.SetAsync("k", "one");
        await store.SetAsync("k", "two");
        Assert.Equal("two", await store.GetAsync("k"));
    }
}
