namespace ChefLan.Domain.Enums;

/// <summary>
/// Roles for INTERNAL staff accounts only (identity.users).
/// Customers are a separate population and have no entry here.
/// Values are persisted as integers — never renumber them.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Staff = 2,
}
