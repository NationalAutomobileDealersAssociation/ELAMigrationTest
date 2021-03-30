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
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Notes { get; set; }
        public string HotelContacts { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
