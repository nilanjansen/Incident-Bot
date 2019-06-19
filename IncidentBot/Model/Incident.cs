using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IncidentBot.Model
{
    public class Incident
    {
        public int IncidentId { get; set; }
        public string CreatorContact { get; set; }
        public string IssueType { get; set; }
        public string Location { get; set; }
        public byte[] Media { get; set; }
    }
}
