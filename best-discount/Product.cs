using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace best_discount
{
    public class Product
    {
        public string Image { get; set; }
        public string FullTitle { get; set; }
        public string Category { get; set; }
        public string AvailableDate { get; set; }
        public string DiscountPercentage { get; set; }
        public string OriginalPrice { get; set; }
        public string CurrentPrice { get; set; }
        public string ProductUrl { get; set; }
        public string ProductImg { get; set; }
    }
}
