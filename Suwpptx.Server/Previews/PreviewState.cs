using System.Text.Json.Serialization;

namespace Suwpptx.Server.Previews;

[JsonConverter(typeof(JsonStringEnumConverter<PreviewState>))]
internal enum PreviewState
{
    Rendering,
    Ready,
    Failed,
}
