using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Identity.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Identity;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the staff auth endpoints (step 2).
///
/// When implementing: queries honour the soft-delete filter, so a lookup by email finds
/// only live accounts. Use AsNoTracking for reads; the login path needs tracking because
/// it updates last_login_at and failed_login_count.
/// </summary>
internal sealed class UserRepository(CoursesDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository.GetByIdAsync — step 2 (staff auth).");

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository.GetByEmailAsync — step 2 (staff auth).");

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository.EmailExistsAsync — step 2 (staff auth).");

    public Task<PagedResult<User>> ListAsync(
        PagedRequest request,
        string? search,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository.ListAsync — step 2 (admin user list).");

    public void Add(User user) => context.Users.Add(user);
}
