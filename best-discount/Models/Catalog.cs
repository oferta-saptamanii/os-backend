using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Models
{
    [FirestoreData]
    public class Catalog
    {
        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public string AvailableDate { get; set; }
        [FirestoreProperty]
        public string Url { get; set; }
        [FirestoreProperty]
        public string Image { get; set; }
    }
}
