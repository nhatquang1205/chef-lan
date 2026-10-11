using ChefLan.Commons.Results;
using ChefLan.Commons.Text;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Errors;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

internal sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ISlugGenerator slugGenerator) : ICategoryService
{
    private const int SlugMaxLength = 140;

    public async Task<IReadOnlyList<CategoryAdminResponse>> ListAsync(
        CategoryStatus? status,
        string? search,
        CancellationToken cancellationToken = default) =>
        (await categories.ListAsync(status, SlugAllocator.Clean(search), cancellationToken))
            .Select(c => c.ToAdminResponse())
            .ToList();

    public async Task<Result<CategoryAdminResponse>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        return category is null ? CategoryErrors.NotFound(id) : category.ToAdminResponse();
    }

    public async Task<Result<CategoryAdminResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var slug = await SlugAllocator.AllocateAsync(
            slugGenerator, name, "category", SlugMaxLength, categories.GetSlugsWithPrefixAsync, cancellationToken);

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = SlugAllocator.Clean(request.Description),
            ThumbnailUrl = SlugAllocator.Clean(request.ThumbnailUrl),
            DisplayOrder = request.DisplayOrder ?? 0,
            Status = request.Status,
        };

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToAdminResponse();
    }

    /// <summary>The slug is left alone on rename — changing it would break existing links.</summary>
    public async Task<Result<CategoryAdminResponse>> UpdateAsync(
        long id,
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return CategoryErrors.NotFound(id);
        }

        category.Name = request.Name.Trim();
        category.Description = SlugAllocator.Clean(request.Description);
        category.ThumbnailUrl = SlugAllocator.Clean(request.ThumbnailUrl);
        category.Status = request.Status;

        if (request.DisplayOrder is { } order)
        {
            category.DisplayOrder = order;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.ToAdminResponse();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound(id));
        }

        if (await categories.IsInUseAsync(id, cancellationToken))
        {
            return Result.Failure(CategoryErrors.InUse);
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
