using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Plugins.Cleaners;

public sealed class PythonCleaner : ProjectCleanerBase
{
    public override string Name => "Python Cleaner";
    public override bool CanHandle(ProjectInsight insight) => insight.ProjectType == "Python";
}
