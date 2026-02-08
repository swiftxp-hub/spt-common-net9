using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.Common.NET9.Json;

#if NET9_0_OR_GREATER
[SPTarkov.DI.Annotations.Injectable(SPTarkov.DI.Annotations.InjectionType.Singleton)]
#endif
public class JsonFileSerializer : IJsonFileSerializer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<T?> DeserializeJsonFileAsync<T>(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("JSON file not found", filePath);

        using FileStream contentStream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return await JsonSerializer.DeserializeAsync<T>(
            contentStream,
            s_jsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SerializeJsonFileAsync<T>(
        string filePath,
        T value,
        CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using FileStream fileStream = new(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(
            fileStream,
            value,
            s_jsonOptions,
            cancellationToken).ConfigureAwait(false);
    }
}