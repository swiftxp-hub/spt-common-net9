using System;
using System.IO;
using System.Threading.Tasks;
using SwiftXP.SPT.Common.NET9.Json;
using Xunit;

namespace SwiftXP.SPT.Common.Tests.NET9.Json;

public class JsonFileSerializerTests
{
    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirInfo { get; }

        public TempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            DirInfo = Directory.CreateDirectory(path);
        }

        public string GetPath(string fileName) => Path.Combine(DirInfo.FullName, fileName);

        public void Dispose()
        {
            if (DirInfo.Exists)
            {
                try { DirInfo.Delete(true); } catch { }
            }
        }
    }

    private sealed class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public string? HtmlContent { get; set; }
    }

    [Fact]
    public async Task SerializeJsonFileAsyncCreatesFileAndWritesData()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("data.json");
        TestData data = new() { Name = "Test", Id = 123 };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);

        Assert.True(File.Exists(filePath));
        string content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("Test", content);
        Assert.Contains("123", content);
    }

    [Fact]
    public async Task SerializeJsonFileAsyncCreatesMissingDirectories()
    {
        using TempDirectory temp = new();
        string filePath = Path.Combine(temp.DirInfo.FullName, "sub", "folder", "config.json");
        TestData data = new() { Name = "Deep" };
        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);

        Assert.True(File.Exists(filePath));
        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public async Task DeserializeJsonFileAsyncReadsDataCorrectly()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("input.json");

        await File.WriteAllTextAsync(filePath, "{ \"Name\": \"Reader\", \"Id\": 999 }");

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal("Reader", result.Name);
        Assert.Equal(999, result.Id);
    }

    [Fact]
    public async Task DeserializeJsonFileAsyncThrowsFileNotFoundIfFileMissing()
    {
        using TempDirectory temp = new();
        string missingPath = temp.GetPath("ghost.json");
        JsonFileSerializer serializer = new();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            serializer.DeserializeJsonFileAsync<TestData>(missingPath));
    }

    [Fact]
    public async Task OptionsCaseInsensitiveWorks()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("case.json");

        await File.WriteAllTextAsync(filePath, "{ \"name\": \"lowercase\", \"id\": 1 }");

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal("lowercase", result.Name);
    }

    [Fact]
    public async Task OptionsAllowTrailingCommasWorks()
    {
        using TempDirectory temp = new TempDirectory();
        string filePath = temp.GetPath("commas.json");

        await File.WriteAllTextAsync(filePath, "{ \"Name\": \"Comma\", \"Id\": 1, }");

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal("Comma", result.Name);
    }

    [Fact]
    public async Task OptionsReadCommentHandlingSkipsComments()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("comments.json");

        string jsonWithComments = @"
        { 
            // Dies ist ein Kommentar
            ""Name"": ""NoComments"", 
            /* Block Kommentar */
            ""Id"": 5
        }";

        await File.WriteAllTextAsync(filePath, jsonWithComments);

        JsonFileSerializer serializer = new();

        TestData? result = await serializer.DeserializeJsonFileAsync<TestData>(filePath);

        Assert.NotNull(result);
        Assert.Equal("NoComments", result.Name);
        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task OptionsUnsafeRelaxedJsonEscapingDoesNotEscapeHtml()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("html.json");

        TestData data = new() { Name = "HTML", HtmlContent = "<h1>Title</h1>" };

        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);
        string fileContent = await File.ReadAllTextAsync(filePath);

        Assert.Contains("<h1>Title</h1>", fileContent);
    }

    [Fact]
    public async Task OptionsWriteIndentedIsActive()
    {
        using TempDirectory temp = new();
        string filePath = temp.GetPath("pretty.json");

        TestData data = new() { Name = "A", Id = 1 };

        JsonFileSerializer serializer = new();

        await serializer.SerializeJsonFileAsync(filePath, data);
        string[] lines = await File.ReadAllLinesAsync(filePath);

        Assert.True(lines.Length > 2, "JSON should be multi-line (indented)");
    }
}