using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManageCaseTestProject
{
    internal class RequestBody
    {
        public string AccountNumber { get; set; }
        public string Classification { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string ChannelType { get; set; }
        public string CaseType { get; set; }
        public string LeadID { get; set; }
        public string Subject { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public string UCIC { get; set; }
    }
}
