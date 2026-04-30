using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Plugins.Cleaners;

public sealed class NodeCleaner : ProjectCleanerBase
{
    public override string Name => "Node Cleaner";
    public override bool CanHandle(ProjectInsight insight) => insight.ProjectType == "Node.js";
}
