using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace QZBarberShopBooking.Application.DTO.Shared
{
    public class PaginationMetadata
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("firstItem")]
        public int FirstItem { get; set; }

        [JsonPropertyName("lastItem")]
        public int LastItem { get; set; }

        public static PaginationMetadata FromPaginatedResponse<T>(PaginatedResponse<T> response)
        {
            var items = response.Data?.ToList() ?? new List<T>();
            var firstItem = items.Count > 0 ? ((response.PageNumber - 1) * response.PageSize) + 1 : 0;
            var lastItem = firstItem + items.Count - 1;

            return new PaginationMetadata
            {
                PageNumber = response.PageNumber,
                PageSize = response.PageSize,
                TotalPages = response.TotalPages,
                TotalCount = response.TotalCount,
                HasPreviousPage = response.HasPreviousPage,
                HasNextPage = response.HasNextPage,
                FirstItem = firstItem,
                LastItem = lastItem
            };
        }
    }
}
