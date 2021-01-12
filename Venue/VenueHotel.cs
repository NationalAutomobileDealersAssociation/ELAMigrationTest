using ELAMigrationTest.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace ELAMigrationTest.Venue
{
    public class VenueHotel
    {
        private string _connectionString;
        public VenueHotel(IConfiguration iconfiguration)
        {
            _connectionString = iconfiguration.GetConnectionString("Default");
        }
        public List<VenueModel> GetList()
        {
            var listVenueModel = new List<VenueModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {

                    var query = @"
                               SELECT [ID]
      ,[HotelID]
      ,[HotelName]
      ,[VenueID]
      ,[VenueName]
      ,[NewVenue]
  FROM [dbo].[VenueHotel]";
               
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.CommandText = query;
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        listVenueModel.Add(new VenueModel
                        {
                            ID = (Guid)rdr[0],
                            HotelID = rdr[1].ToString(),
                            HotelName = rdr[2].ToString(),
                            VenueID = rdr[3].ToString(),
                            VenueName = rdr[4].ToString(),
                            NewVenue = Convert.ToBoolean(rdr[5])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return listVenueModel;
        }
    }
}
