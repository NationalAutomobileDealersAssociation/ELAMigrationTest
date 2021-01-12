using System;
using System.Collections.Generic;
using System.Text;

namespace ELAMigrationTest.Model
{
    public class VenuesProd
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
