// ---------------------------------------------------------------------------
//  File        : PagedResponse.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : DTOs / Common
//  Description : Generic paged list envelope returned by list endpoints.
//                Both clients rely on the total count to render pagination
//                controls without a second request.
//  Author      : <member name - M1>
//  Created     : 2026-09-16
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Common;

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
