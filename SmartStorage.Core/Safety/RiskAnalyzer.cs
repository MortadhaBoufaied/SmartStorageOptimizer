using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Core.Safety;

public sealed class RiskAnalyzer
{
    public bool IsHighRisk(FileRecord record, IReadOnlyCollection<string> tags)
    {
        if (record.Path.Contains(Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return true;
        if (record.Path.Contains(Path.DirectorySeparatorChar + "Program Files", StringComparison.OrdinalIgnoreCase)) return true;
        if (tags.Contains("document") && !tags.Contains("unused")) return true;
        if (record.Extension is ".sln" or ".csproj" or ".py" or ".js" or ".ts") return true;
        return false;
    }
}
