using System;
using System.Collections.Generic;
using System.Text;

namespace ELAMigrationTest.Model
{
    public class VenueModel
    {
        public Guid ID { get; set; }
        public string HotelID { get; set; }
        public string HotelName { get; set; }
        public string VenueID { get; set; }
        public string VenueName { get; set; }
        public bool NewVenue { get; set; }
    }
}
