using PhotoSortingApp.Domain.Models;

namespace PhotoSortingApp.Core.Interfaces;

public interface IOrganizerPlanService
{
    Task<OrganizerPlanResult> CreatePlanAsync(OrganizerPlanRequest request, CancellationToken cancellationToken = default);

    Task<OrganizerApplyResult> ApplyPlanAsync(
        int scanRootId,
        IReadOnlyList<OrganizerPlanItem> planItems,
        CancellationToken cancellationToken = default);
}
