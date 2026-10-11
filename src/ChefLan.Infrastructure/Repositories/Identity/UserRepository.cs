using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Identity;

/// <summary>
/// Reads are tracked on purpose: the login path updates failed_login_count, locked_until and
/// last_login_at on the returned entity. The soft-delete query filter means only live
/// accounts are found, and email is citext so the match is case-insensitive.
/// </summary>
internal sealed class UserRepository(CoursesDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    /// <summary>SKELETON — arrives with the admin staff-management endpoints.</summary>
    public Task<PagedResult<User>> ListAsync(
        PagedRequest request,
        string? search,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository.ListAsync — admin user list.");

    public void Add(User user) => context.Users.Add(user);
}
