using SmartStorage.Abstractions.Contracts;
using SmartStorage.Core.Analysis;
using Xunit;

namespace SmartStorage.Tests;

public sealed class ScoringEngineTests
{
    [Fact]
    public async Task UnusedTemporaryFile_ShouldScoreHigh()
    {
        var engine = new ScoringEngine();
        var record = new FileRecord("C:/temp/a.tmp", "a.tmp", false, 1024, ".tmp", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(-1), DateTime.UtcNow, null, new[] { "temporary" });
        var usage = new UsageProfile(record.Path, DateTime.UtcNow.AddYears(-1), 30, 0, 0.0, 0.0);
        var score = await engine.ScoreAsync(record, usage, new[] { "temporary", "unused" });
        Assert.True(score >= 0.80);
    }
}
