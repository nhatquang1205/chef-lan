namespace ChefLan.Domain.Enums;

/// <summary>
/// Whether a category is shown on the storefront. Distinct from soft delete: InActive is
/// reversible and keeps course links intact, which is what a seasonal category like
/// "Bánh Trung Thu" needs — hidden in March, shown in August.
///
/// Values are persisted as integers — never renumber them.
/// </summary>
public enum CategoryStatus
{
    Active = 1,
    InActive = 2,
}
