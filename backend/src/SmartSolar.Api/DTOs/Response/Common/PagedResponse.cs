namespace SmartSolar.Api.DTOs.Response.Common;

public record PagedResponse<T>(
    // The subset of items for the requested page.
    List<T> Items,

    // Total matching records across all pages.
    int Total,

    // 1-based page number returned.
    int Page,

    // Max items per page.
    int PageSize
);
