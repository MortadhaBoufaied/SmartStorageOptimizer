using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Plugins.Cleaners;

public sealed class FlutterCleaner : ProjectCleanerBase
{
    public override string Name => "Flutter Cleaner";
    public override bool CanHandle(ProjectInsight insight) => insight.ProjectType == "Flutter";
}
