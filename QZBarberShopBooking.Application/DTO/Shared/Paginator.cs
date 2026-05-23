using QZBarberShopBooking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Shared
{
    public class Paginator<T>
    {
        public Paginator(IEnumerable<T> data, int totalCount)
        {
            Data = data.ToList();
            TotalCount = totalCount;
        }
        public Paginator() { }

        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
    }
    public class FilterItem
    {
        public string Value { get; set; } = string.Empty;
        public MatchModeEnum MatchMode { get; set; }
    }
    public class Pagination
    {
        public int Page { get; set; }
        public string? Keyword { get; set; }
        public Dictionary<string, List<FilterItem>> Filters { get; set; } = new();
    }
}