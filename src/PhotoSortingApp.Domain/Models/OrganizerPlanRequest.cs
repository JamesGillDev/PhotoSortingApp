using PhotoSortingApp.Domain.Enums;

namespace PhotoSortingApp.Domain.Models;

public class OrganizerPlanRequest
{
    public int ScanRootId { get; set; }

    public OrganizeRuleType RuleType { get; set; } = OrganizeRuleType.YearMonthFolders;
}
