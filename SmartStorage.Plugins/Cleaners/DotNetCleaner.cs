using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Plugins.Cleaners;

public sealed class DotNetCleaner : ProjectCleanerBase
{
    public override string Name => ".NET Cleaner";
    public override bool CanHandle(ProjectInsight insight) => insight.ProjectType == ".NET";
}
