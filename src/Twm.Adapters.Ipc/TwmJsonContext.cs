using System.Text.Json.Serialization;

namespace Twm.Adapters.Ipc;

/// <summary>
/// Source-generated JSON metadata for IPC payloads. Serializing through this
/// context instead of reflection-based <c>JsonSerializer</c> keeps
/// serialization trim/AOT-safe.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(TreeNode))]
internal sealed partial class TwmJsonContext : JsonSerializerContext { }
