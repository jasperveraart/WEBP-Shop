namespace PWebShop.Api.Dtos;

public class PagedResultDto<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
