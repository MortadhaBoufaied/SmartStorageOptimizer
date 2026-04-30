using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IActionExecutor
{
    Task ExecuteAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
