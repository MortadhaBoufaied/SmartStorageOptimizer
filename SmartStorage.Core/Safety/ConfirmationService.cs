using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Core.Safety;

public sealed class ConfirmationService
{
    public bool ShouldPrompt(Recommendation recommendation) => recommendation.RequiresConfirmation;
}
