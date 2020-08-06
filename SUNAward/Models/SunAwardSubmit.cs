using SUNAward.Data;
using System.Collections.Generic;

namespace SUNAward.Models
{
    public class SunAwardSubmit
    {
        public Person Recipient { get; set; }

        public Person Supervisor { get; set; }
        
        public string For { get; set; }
        
        public string CustomCategory { get; set; }
        
        public List<int> Categories { get; set; }
    }
}