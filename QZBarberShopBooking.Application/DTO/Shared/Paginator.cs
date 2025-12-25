using QZBarberShopBooking.Application.Enums;
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
        public object value { get; set; }
        public MatchModeEnum matchMode { get; set; }
    }
    public class Pagination
    {
        public int Page { get; set; }
        public string? kw { get; set; }
        public Dictionary<string, List<FilterItem>> Filters { get; set; } = new();
    }
}
