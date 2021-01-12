using ELAMigrationTest.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace ELAMigrationTest.Venue
{
    public class ProdList
    {
        private string _connectionString;
        public ProdList(IConfiguration iconfiguration)
        {
            _connectionString = iconfiguration.GetConnectionString("Prod");
        }
        public List<VenuesProd> GetList()
        {
            var listVenueModel = new List<VenuesProd>();
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {

                    var query = @"
                               SELECT [Id]
      ,[Location]
      ,[Name]
      ,[CreatedBy]
      ,[CreatedDate]
      ,[ModifiedBy]
      ,[ModifiedDate]
  FROM [dbo].[Venues]";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.CommandText = query;
                    con.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        listVenueModel.Add(new VenuesProd
                        {
                            Id = (Guid)rdr[0],
                            Name = rdr[1].ToString(),
                            Location = rdr[2].ToString()
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
