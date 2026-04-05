using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Eva.Commons.Util;
using Eva.Node.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Node.NET.Configuration;

public class ConfigurationTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        EvaLogger.Init("Configuration Tests");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempFile(string name = "config.json") => Path.Combine(_tempDir, name);

    private void WriteJson<T>(string path, T obj) =>
        File.WriteAllText(path, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));

    // -------------------------------------------------------------------------
    // Classes de config de test
    // -------------------------------------------------------------------------

    private class TestConfig
    {
        public string Name { get; set; } = "default";
        public int Count { get; set; } = 42;
    }

    private class EmptyConfig { }

    // -------------------------------------------------------------------------
    // Chargement
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_FileExists_DeserializesCorrectly()
    {
        var path = TempFile();
        WriteJson(path, new TestConfig { Name = "hello", Count = 99 });

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.Equal("hello", config.Value.Name);
        Assert.Equal(99, config.Value.Count);
    }

    [Fact]
    public void Load_FileAbsent_UsesDefaultValues()
    {
        var path = TempFile("missing.json");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.Equal("default", config.Value.Name);
        Assert.Equal(42, config.Value.Count);
    }

    [Fact]
    public void Load_FileAbsent_CreatesFileWithDefaults()
    {
        var path = TempFile("missing.json");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.True(File.Exists(path));
        var written = JsonSerializer.Deserialize<TestConfig>(File.ReadAllText(path));
        Assert.NotNull(written);
        Assert.Equal("default", written!.Name);
        Assert.Equal(42, written.Count);
    }

    [Fact]
    public void Load_FileAbsent_CreatesSubdirectories()
    {
        var path = Path.Combine(_tempDir, "sub", "nested", "config.json");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Load_MalformedJson_FallsBackToDefaults()
    {
        var path = TempFile();
        File.WriteAllText(path, "{ this is not valid json }}}");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.Equal("default", config.Value.Name);
        Assert.Equal(42, config.Value.Count);
    }

    [Fact]
    public void Load_EmptyJson_FallsBackToDefaults()
    {
        var path = TempFile();
        File.WriteAllText(path, "");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.Equal("default", config.Value.Name);
    }

    [Fact]
    public void Load_PartialJson_UsesDefaultsForMissingFields()
    {
        var path = TempFile();
        File.WriteAllText(path, """{ "Name": "partial" }""");

        var config = new Configuration<TestConfig>(path, hotReload: false);

        Assert.Equal("partial", config.Value.Name);
        Assert.Equal(42, config.Value.Count); // valeur par défaut conservée
    }

    // -------------------------------------------------------------------------
    // Hot reload
    // -------------------------------------------------------------------------

    [Fact]
    public void HotReload_FileChanged_ReloadsValue()
    {
        var path = TempFile();
        WriteJson(path, new TestConfig { Name = "initial", Count = 1 });

        var config = new Configuration<TestConfig>(path, hotReload: true);
        Assert.Equal("initial", config.Value.Name);

        WriteJson(path, new TestConfig { Name = "updated", Count = 2 });

        // Laisse le FileSystemWatcher + Sleep(100) réagir
        Thread.Sleep(500);

        Assert.Equal("updated", config.Value.Name);
        Assert.Equal(2, config.Value.Count);
    }

    [Fact]
    public void HotReload_Disabled_DoesNotReloadOnChange()
    {
        var path = TempFile();
        WriteJson(path, new TestConfig { Name = "initial", Count = 1 });

        var config = new Configuration<TestConfig>(path, hotReload: false);

        WriteJson(path, new TestConfig { Name = "updated", Count = 2 });
        Thread.Sleep(500);

        Assert.Equal("initial", config.Value.Name);
    }

    [Fact]
    public void HotReload_MalformedJsonAfterChange_KeepsPreviousValue()
    {
        var path = TempFile();
        WriteJson(path, new TestConfig { Name = "stable", Count = 5 });

        var config = new Configuration<TestConfig>(path, hotReload: true);

        File.WriteAllText(path, "{ broken json");
        Thread.Sleep(500);

        // Doit garder l'ancienne valeur stable
        Assert.Equal("stable", config.Value.Name);
    }

    // -------------------------------------------------------------------------
    // Types génériques variés
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_EmptyConfigClass_Works()
    {
        var path = TempFile();

        var config = new Configuration<EmptyConfig>(path, hotReload: false);

        Assert.NotNull(config.Value);
    }
}