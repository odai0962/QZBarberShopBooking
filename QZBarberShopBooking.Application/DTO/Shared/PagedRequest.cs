using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace QZBarberShopBooking.Application.DTO.Shared
{
    public class PagedRequest
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        [JsonPropertyName("page")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
        }

        [JsonPropertyName("search")]
        public string? SearchTerm { get; set; }

        [JsonPropertyName("sortBy")]
        public string? SortBy { get; set; }

        [JsonPropertyName("sortDesc")]
        public bool SortDescending { get; set; }

        [JsonPropertyName("filters")]
        public Dictionary<string, object>? Filters { get; set; }
    }
}
