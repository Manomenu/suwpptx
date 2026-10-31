using Microsoft.Extensions.Logging;

namespace Suwpptx.Server.Tests;

internal sealed record CapturedLog(string Category, LogLevel Level, string Message, string? TraceId);
