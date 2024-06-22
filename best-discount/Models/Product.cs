using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Models
{
    [FirestoreData]
    public class Product
    {
        [FirestoreProperty]
        public string Image { get; set; }

        [FirestoreProperty]
        public string FullTitle { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        public string AvailableDate { get; set; }

        [FirestoreProperty]
        public string DiscountPercentage { get; set; }

        [FirestoreProperty]
        public string OriginalPrice { get; set; }

        [FirestoreProperty]
        public string CurrentPrice { get; set; }

        [FirestoreProperty]
        public string ProductUrl { get; set; }

        [FirestoreProperty]
        public string ProductImg { get; set; }
    }
}
