namespace ChefLan.Logic.Customers.Contracts;

/// <summary>Derived, never stored. Rules in docs/customer-invitations/spec.md.</summary>
public enum CustomerStatus
{
    Active = 1,
    Invited = 2,
    InviteExpired = 3,
    NotInvited = 4,
    Inactive = 5,
}

/// <summary>Create and update share this shape. <c>SendInvite</c> is accepted and ignored until invitations exist.</summary>
public sealed record CustomerAdminRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? FacebookName,
    string? FacebookProfileUrl,
    string? Note,
    bool SendInvite = false);

public sealed record CustomerAdminQuery(string? Q, CustomerStatus? Status, int Page = 1, int PageSize = 20);

public sealed record CustomerAdminListItem(
    long Id,
    string FullName,
    string? Email,
    string? Phone,
    string? FacebookName,
    CustomerStatus Status,
    DateTime? InvitedAt,
    DateTime? ActivatedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    int CourseCount,
    int LessonCount);

public sealed record CustomerAdminDetail(
    long Id,
    string FullName,
    string? Email,
    string? Phone,
    string? FacebookName,
    string? FacebookProfileUrl,
    string? Note,
    bool IsActive,
    CustomerStatus Status,
    DateTime? InvitedAt,
    DateTime? InviteExpiresAt,
    DateTime? ActivatedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

/// <summary>Active, unexpired grants a customer holds.</summary>
public readonly record struct CustomerAccessCounts(int Courses, int Lessons);
