using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.Common.Json;

public interface IJsonFileSerializer
{
    Task<T?> DeserializeJsonFileAsync<T>(string filePath, CancellationToken cancellationToken = default);

    Task SerializeJsonFileAsync<T>(string filePath, T value, CancellationToken cancellationToken = default);
}