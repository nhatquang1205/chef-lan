using ChefLan.Commons.Pagination;
using ChefLan.Domain.Identity.Entities;

namespace ChefLan.Logic.Identity.Interfaces;

/// <summary>
/// Port for internal STAFF accounts. Returns entities and paged results — never IQueryable,
/// which would leak EF Core into the application layer.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<PagedResult<User>> ListAsync(PagedRequest request, string? search, CancellationToken cancellationToken = default);

    void Add(User user);
}
