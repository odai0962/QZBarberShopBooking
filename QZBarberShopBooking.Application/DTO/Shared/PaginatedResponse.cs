using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace QZBarberShopBooking.Application.DTO.Shared
{
    public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
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
        public bool HasPreviousPage => PageNumber > 1;

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage => PageNumber < TotalPages;

        private PaginatedResponse() { }

        public static PaginatedResponse<T> Create(
            IEnumerable<T> data,
            int pageNumber,
            int pageSize,
            int totalCount,
            string message = "Data retrieved successfully")
        {
            return new PaginatedResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public static PaginatedResponse<T> Empty(string message = "No data found")
        {
            return new PaginatedResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = new List<T>(),
                PageNumber = 1,
                PageSize = 0,
                TotalCount = 0,
                TotalPages = 0
            };
        }
    }
}
