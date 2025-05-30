using System;

namespace MISReports_Api.Models
{
    public class MaterialReagionStock
    {
        public string Region { get; set; }
        public string MatCd { get; set; }
        public decimal QtyOnHand { get; set; }
    }
}
