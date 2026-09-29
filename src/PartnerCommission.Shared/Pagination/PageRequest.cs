using System.ComponentModel.DataAnnotations;

namespace PartnerCommission.Shared.Pagination;

public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}
