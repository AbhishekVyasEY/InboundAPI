using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateLeadsTestProject
{
    internal class RequestBody
    {
        public string LeadID { get; set; }
        public string ChannelType { get; set; }
        public string ProductCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string CityName { get; set; }
        public string BranchCode { get; set; }
        public string CustomerID { get; set; }
        public string Pincode { get; set; }
        public string MiddleName { get; set; }

    }
}
