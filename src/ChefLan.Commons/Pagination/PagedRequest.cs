using ChefLan.Commons.Constants;

namespace ChefLan.Commons.Pagination;

public record PagedRequest
{
    private int _page = 1;
    private int _pageSize = ChefLanConstants.DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => ChefLanConstants.DefaultPageSize,
            > ChefLanConstants.MaxPageSize => ChefLanConstants.MaxPageSize,
            _ => value,
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
