using Microsoft.EntityFrameworkCore;

namespace PartnerCommission.Shared.Pagination;

public static class QueryablePagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(this IQueryable<T> query, PageRequest page, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);

        var skip = (long)(page.Page - 1) * page.PageSize;

        if (skip >= totalCount)
            return new PagedResponse<T>([], page.Page, page.PageSize, totalCount);

        var items = await query
            .Skip((int)skip)
            .Take(page.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<T>(items, page.Page, page.PageSize, totalCount);
    }
}
