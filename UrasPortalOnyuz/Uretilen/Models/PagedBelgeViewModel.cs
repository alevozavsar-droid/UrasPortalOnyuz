using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class PagedBelgeViewModel
    {
        public List<BelgeViewModel> Belgeler { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        public bool HasMoreData { get; set; } // Bu satırı ekleyin!

    }
}