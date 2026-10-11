using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class CategoryCatalogService(ICategoryRepository categories) : ICategoryCatalogService
{
    public async Task<IReadOnlyList<CategoryResponse>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        (await categories.ListAsync(CategoryStatus.Active, search: null, cancellationToken))
            .Select(c => c.ToResponse())
            .ToList();
}
