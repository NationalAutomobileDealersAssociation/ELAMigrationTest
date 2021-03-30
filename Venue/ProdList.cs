using ELAMigrationTest.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ELAMigrationTest.Venue
{
    public class ProdList
    {
        private string _connectionString;
        private string _migrationConnectionString;
        public ProdList(IConfiguration iconfiguration)
        {
            _connectionString = iconfiguration.GetConnectionString("Prod");
            _migrationConnectionString = iconfiguration.GetConnectionString("Default");
        }
        public List<VenuesProd> GetList()
        {
            var listVenuesProdModel = new List<VenuesProd>();
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                using (SqlConnection migrationCon = new SqlConnection(_migrationConnectionString))
                {

                    var queryNonDuplicateHotels = @"SELECT * 
                    FROM [dbo].[Hotels]
                    WHERE [Hotels].Name != 'Wynn Las Vegas' AND [Hotels].Name != 'Encore Las Vegas' AND [Hotels].Name != 'JW Marriott'";

                    var queryGetDuplicateHotels = @" SELECT *
                    FROM [dbo].[Venues] INNER JOIN [dbo].[Hotels] ON [Venues].[Name] = [Hotels].Name 
                    WHERE [Hotels].Id = '8C0B6503-1145-4667-8A2F-08D6C33E96E2' OR [Hotels].Id = '07B4D7DA-5936-47B3-0ECD-08D70AF2BE3E' OR [Hotels].Id = 'A57FB1E4-E6C0-4068-8A36-08D6C33E96E2'";

                    //-----------DUPLICATE VENUES & HOTELS----------//
                    SqlCommand duplicateHotelsCmd = new SqlCommand(queryGetDuplicateHotels, con);
                    con.Open();
                    DataTable duplicateHotels = new DataTable();
                    duplicateHotels.Load(duplicateHotelsCmd.ExecuteReader());

                    foreach (DataRow duplicatesRow in duplicateHotels.Rows)
                    {
                        //insert venue and hotel name into migration database
                        Guid id = Guid.NewGuid();
                        var queryHotelVenue = @"INSERT INTO [ELAMigration].[dbo].[VenueHotel]
                        ([ID]
                        ,[HotelID]
                        ,[HotelName]
                        ,[VenueID]
                        ,[VenueName]
                        ,[NewVenue])";
                        queryHotelVenue += "VALUES('" + id + "','" + duplicatesRow[13].ToString() + "','" + duplicatesRow[17].ToString() + "','" + duplicatesRow[0].ToString() + "','" + duplicatesRow[2].ToString() + "','" + false + "')";
                        SqlCommand addEntry = new SqlCommand(queryHotelVenue, migrationCon);
                        //addEntry.CommandText = queryHotelVenue;
                        migrationCon.Open();
                        SqlDataReader rdr4 = addEntry.ExecuteReader();

                        //update venues
                        var updateVenue = @"UPDATE [dbo].[Venues]
      SET [Address] = '" + duplicatesRow[14].ToString() + "',[City] = '" + duplicatesRow[15].ToString() + "',[Notes] = '" + duplicatesRow[18].ToString() + "',[State] = '" + duplicatesRow[19].ToString() +
      "',[Zip] = '" + duplicatesRow[20].ToString() + "',[ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "' WHERE [Id] = '" + duplicatesRow[0] + "'";
                        SqlCommand updateVenueProd = new SqlCommand(updateVenue, con);
                        SqlDataReader rdr5 = updateVenueProd.ExecuteReader();

                        //Get duplicate Venue Rooms
                        var queryGetDuplicateRooms = @"SELECT *
  FROM[dbo].[HotelRooms] INNER JOIN[dbo].[VenueRooms] ON[HotelRooms].Name = [VenueRooms].RoomNumber
  WHERE[HotelRooms].[HotelId] = '" + duplicatesRow[13] + "'";
                        SqlCommand getDuplicateRooms = new SqlCommand(queryGetDuplicateRooms, con);
                        DataTable getDuplicateRoomsTable = new DataTable();
                        getDuplicateRoomsTable.Load(getDuplicateRooms.ExecuteReader());
                        foreach (DataRow duplicateRoomsRow in getDuplicateRoomsTable.Rows)
                        {
                            //insert venue rooms and hotel rooms name into migration database
                            Guid vrid = Guid.NewGuid();
                            var queryRooms = @"INSERT INTO [ELAMigration].[dbo].[Rooms]
                        ([ID]
                      ,[HotelRoomID]
           ,[HotelRoomName]
           ,[VenueRoomID]
           ,[VenueRoomName]
                        ,[NewRoom])";
                            queryRooms += "VALUES('" + vrid + "','" + duplicateRoomsRow[0].ToString() + "','" + duplicateRoomsRow[11].ToString() + "','" + 
                                duplicateRoomsRow[22].ToString() + "','" + duplicateRoomsRow[24].ToString() + "','" + false + "')";
                            SqlCommand addRoom = new SqlCommand(queryRooms, migrationCon);
                            //addEntry.CommandText = queryHotelVenue;
                            migrationCon.Open();
                            SqlDataReader queryRoomsRdr = addRoom.ExecuteReader();

                            //update venue rooms
                            var queryUpdateRooms = @"UPDATE [dbo].[VenueRooms]
                         SET [BanquetCapacity] = '" + duplicateRoomsRow[1] + "',[CeilingHeight] = '" + duplicateRoomsRow[2] + "',[ClassroomCapacity] = '" + duplicateRoomsRow[3] + "',[ConferenceCapacity] = '" + duplicateRoomsRow[4] +
                         "',[Dimensions] = '" + duplicateRoomsRow[6] + "',[HollowSqCapacity] = '" + duplicateRoomsRow[7] + "',[OtherCapacity] = '" + duplicateRoomsRow[13] + "',[ReceptionCapacity] = '" + duplicateRoomsRow[14] +
                         "',[SquareFeet] = '" + duplicateRoomsRow[15] + "',[TheatreCapacity] = '" + duplicateRoomsRow[16] + "',[UshapeCapacity] = '" + duplicateRoomsRow[17] + "',[IsMasterRoom] = '" + duplicateRoomsRow[9] +
                         "',[Notes] = '" + duplicateRoomsRow[12] + "',[ModifiedBy] = 'First Get Duplicate VR', [ModifiedDate] = '" + DateTime.Now + "'";

                            bool master = (bool)duplicateRoomsRow[9];

                            if (master == false)
                            {
                                //get master room
                                var getmasterroom = @"SELECT TOP 1 * FROM[dbo].[HotelRooms] INNER JOIN [dbo].[VenueRooms] ON [HotelRooms].Name = [VenueRooms].RoomNumber WHERE [HotelRooms].[Id] = '" + duplicateRoomsRow[10] + "'";
                                SqlCommand masterId = new SqlCommand(getmasterroom, con);
                                DataTable masterTable = new DataTable();
                                masterTable.Load(masterId.ExecuteReader());
                                foreach (DataRow masterRow in masterTable.Rows)
                                {
                                    queryUpdateRooms += @",[MasterRoomId] = '" + masterRow[22] + "'";
                                }
                            }
                            
                            queryUpdateRooms += @"WHERE [VenueRooms].[Id] = '" + duplicateRoomsRow[22] + "'";

                            SqlCommand updateRoom = new SqlCommand(queryUpdateRooms, con);
                            SqlDataReader rdruvr = updateRoom.ExecuteReader();

                            // get convention venue id
                            var queryGetcv = @"SELECT * FROM [dbo].[ConventionVenues] WHERE [ConventionId] = '" + duplicatesRow[16] + "' AND [VenueId] = '" + duplicatesRow[0] + "'";
                            SqlCommand getConventionVenueId = new SqlCommand(queryGetcv, con);
                            DataTable getcvTable = new DataTable();
                            getcvTable.Load(getConventionVenueId.ExecuteReader());
                            foreach (DataRow cvid in getcvTable.Rows)
                            {
                                //update contracted spaces
                                var queryUpdatecs = @"UPDATE [dbo].[ContractedSpaces]
                        SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "',[ConventionVenueId] = '" + cvid[0] + "', [VenueId] = '" + duplicatesRow[0] + "', [VenueRoomId] = '" + duplicateRoomsRow[22] +
                        "' WHERE [HotelRoomId] = '" + duplicateRoomsRow[0] + "'";
                                SqlCommand updatecs = new SqlCommand(queryUpdatecs, con);
                                SqlDataReader rdrcs1 = updatecs.ExecuteReader();
                            }
                            //update function space request
                            var queryUpdatefsrvr = @"UPDATE [dbo].[FunctionSpaceRequests]
                            SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "', [VenueRoomAssignedId] = '" + duplicateRoomsRow[22] + "' WHERE [RoomAssignedId] = '" + duplicateRoomsRow[0] + "'";
                            SqlCommand updatefsrvr = new SqlCommand(queryUpdatefsrvr, con);
                            SqlDataReader rdrfsrvr = updatefsrvr.ExecuteReader();
                        }

                        //get non-duplicate Venue Rooms

                        var queryGetNonDuplicateRooms = @"SELECT *
                            FROM [dbo].[HotelRooms] WHERE [HotelRooms].[Name] NOT IN(SELECT [VenueRooms].[RoomNumber] FROM [VenueRooms]) AND [HotelRooms].[HotelId] = '" + duplicatesRow[13] + "'";
                        SqlCommand getNonDuplicateRooms = new SqlCommand(queryGetNonDuplicateRooms, con);
                        DataTable getNonDuplicateRoomsTable = new DataTable();
                        getNonDuplicateRoomsTable.Load(getNonDuplicateRooms.ExecuteReader());
                        foreach (DataRow nonDuplicateRoomsRow in getNonDuplicateRoomsTable.Rows)
                        {

                            // get convention venue id
                            var queryGetcv = @"SELECT * FROM [dbo].[ConventionVenues] WHERE [ConventionId] = '" + duplicatesRow[16] + "' AND [VenueId] = '" + duplicatesRow[0] + "'";
                            SqlCommand getConventionVenueId = new SqlCommand(queryGetcv, con);
                            DataTable getcvTable = new DataTable();
                            getcvTable.Load(getConventionVenueId.ExecuteReader());
                            foreach (DataRow cvid in getcvTable.Rows)
                            {
                                Guid newVenueRoomId = Guid.NewGuid();

                                //insert venue rooms and hotel rooms name into migration database
                                Guid vrid = Guid.NewGuid();
                                var queryRooms = @"INSERT INTO [ELAMigration].[dbo].[Rooms]
                                                ([ID]
                                                ,[HotelRoomID]
                                                ,[HotelRoomName]
                                                ,[VenueRoomID]
                                                ,[VenueRoomName]
                                                ,[NewRoom])";
                                queryRooms += "VALUES('" + vrid + "','" + nonDuplicateRoomsRow[0].ToString() + "','" + nonDuplicateRoomsRow[11].ToString() + "','" +
                                    newVenueRoomId + "','" + nonDuplicateRoomsRow[11].ToString() + "','" + true + "')";
                                SqlCommand addRoom = new SqlCommand(queryRooms, migrationCon);
                                //addEntry.CommandText = queryHotelVenue;
                                migrationCon.Open();
                                SqlDataReader queryRoomsRdr = addRoom.ExecuteReader();

                                //Guid conventionVenueId = cvid[0];
                                // insert new venue rooms
                                var queryInsertRooms = @"INSERT INTO [dbo].[VenueRooms]
                            ( [Id]
                                ,[RoomNumber]
                                ,[RoomSquareFeet]
                                ,[VenueId]    
                                ,[CreatedBy]
                                ,[CreatedDate]
                                ,[ModifiedBy]
                                ,[ConventionVenueId]
                                ,[BanquetCapacity]
                                ,[CeilingHeight]
                                ,[ClassroomCapacity]
                                ,[ConferenceCapacity]
                                ,[Dimensions]
                                ,[HollowSqCapacity]
                                ,[OtherCapacity]
                                ,[ReceptionCapacity]
                                ,[SquareFeet]
                                ,[TheatreCapacity]
                                ,[UshapeCapacity]
                                ,[IsMasterRoom]";

                                //queryInsertRooms += "VALUES('" + newVenueRoomId + "','" + nonDuplicateRoomsRow[11].ToString() + "','" + nonDuplicateRoomsRow[15] + "','" + duplicatesRow[0] +
                                //                    "','Merge','" + DateTime.Now + "', '" + cvid[0] + "','" + nonDuplicateRoomsRow[1] + "','"+ nonDuplicateRoomsRow[2] + "','" + nonDuplicateRoomsRow[3] + "','" +
                                //                    nonDuplicateRoomsRow[4] + "','" + nonDuplicateRoomsRow[6] + "','" + nonDuplicateRoomsRow[7] + "','" + nonDuplicateRoomsRow[13] + "','" + nonDuplicateRoomsRow[14] + "','" +
                                //                    nonDuplicateRoomsRow[15] + "','" + nonDuplicateRoomsRow[16] + "','" + nonDuplicateRoomsRow[17] + "','" + nonDuplicateRoomsRow[9] + "')";

                                //update venue rooms master room id

                                bool master = (bool)nonDuplicateRoomsRow[9];
                                var sqft = nonDuplicateRoomsRow[15].ToString().Replace(",", "");
                                int sq = 0;
                                if (sqft == "")
                                { sq = 0; }
                                else
                                { sq = Convert.ToInt32(sqft); }

                                if (master == false)
                                {
                                    //get master room name 
                                    var getmasterroom = @"SELECT TOP 1 * FROM[dbo].[HotelRooms] INNER JOIN [dbo].[VenueRooms] ON [HotelRooms].Name = [VenueRooms].RoomNumber WHERE [HotelRooms].[Id] = '" + nonDuplicateRoomsRow[10] + "'";
                                    SqlCommand masterId = new SqlCommand(getmasterroom, con);
                                    DataTable masterTable = new DataTable();
                                    masterTable.Load(masterId.ExecuteReader());
                                    foreach (DataRow masterRow in masterTable.Rows)
                                    {
                                        queryInsertRooms += @",[MasterRoomId])";
                                        queryInsertRooms += "VALUES('" + newVenueRoomId + "','" + nonDuplicateRoomsRow[11].ToString() + "','" + sq + "','" + duplicatesRow[0] +
                                                  "','Merge3','" + DateTime.Now + "','First Non-dup VR', '" + cvid[0] + "','" + nonDuplicateRoomsRow[1] + "','" + nonDuplicateRoomsRow[2] + "','" + nonDuplicateRoomsRow[3] + "','" +
                                                  nonDuplicateRoomsRow[4] + "','" + nonDuplicateRoomsRow[6] + "','" + nonDuplicateRoomsRow[7] + "','" + nonDuplicateRoomsRow[13] + "','" + nonDuplicateRoomsRow[14] + "','" +
                                                  sq + "','" + nonDuplicateRoomsRow[16] + "','" + nonDuplicateRoomsRow[17] + "','" + nonDuplicateRoomsRow[9] + "','" + masterRow[22] + "')";
                                    }
                                }
                                else
                                {
                                    queryInsertRooms += ")VALUES('" + newVenueRoomId + "','" + nonDuplicateRoomsRow[11].ToString() + "','" + sq + "','" + duplicatesRow[0] +
                                                        "','Merge3','" + DateTime.Now + "','First Non-dup VR', '" + cvid[0] + "','" + nonDuplicateRoomsRow[1] + "','" + nonDuplicateRoomsRow[2] + "','" + nonDuplicateRoomsRow[3] + "','" +
                                                        nonDuplicateRoomsRow[4] + "','" + nonDuplicateRoomsRow[6] + "','" + nonDuplicateRoomsRow[7] + "','" + nonDuplicateRoomsRow[13] + "','" + nonDuplicateRoomsRow[14] + "','" +
                                                        sq + "','" + nonDuplicateRoomsRow[16] + "','" + nonDuplicateRoomsRow[17] + "','" + nonDuplicateRoomsRow[9] + "')";
                                }

                                SqlCommand insertVenueRooms = new SqlCommand(queryInsertRooms, con);
                                SqlDataReader rdrVenueRooms = insertVenueRooms.ExecuteReader();

                                //update contracted spaces
                                var queryUpdatecs = @"UPDATE [dbo].[ContractedSpaces]
                        SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "',[ConventionVenueId] = '" + cvid[0] + "', [VenueId] = '" + duplicatesRow[0] + "', [VenueRoomId] = '" + newVenueRoomId +
                        "' WHERE [HotelRoomId] = '" + nonDuplicateRoomsRow[0] + "'";
                                SqlCommand updatecs = new SqlCommand(queryUpdatecs, con);
                                SqlDataReader rdrcs1 = updatecs.ExecuteReader();

                                //  update function space request
                                var queryUpdatefsrvr = @"UPDATE [dbo].[FunctionSpaceRequests]
                            SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "', [VenueRoomAssignedId] = '" + newVenueRoomId + "' WHERE [RoomAssignedId] = '" + nonDuplicateRoomsRow[0] + "'";
                                SqlCommand updatefsrvr = new SqlCommand(queryUpdatefsrvr, con);
                                SqlDataReader rdrfsrvr = updatefsrvr.ExecuteReader();
                            }
                        }

                        //update hotel contacts
                        var updateHotelContacts = @"UPDATE [dbo].[HotelContacts]
                            SET [ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "', [VenueId] = '" + duplicatesRow[0] + "' WHERE [HotelId] = '" + duplicatesRow[13] + "'";
                        SqlCommand updateHotelContactsProd = new SqlCommand(updateHotelContacts, con);
                        SqlDataReader rdrhc = updateHotelContactsProd.ExecuteReader();

                        //update function space requests
                        //HOTEL1
                        var updateFunctionSpaceRequests = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "', [Venue1Id] = '" + duplicatesRow[0] + "' WHERE [Hotel1Id] = '" + duplicatesRow[13] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd = new SqlCommand(updateFunctionSpaceRequests, con);
                        SqlDataReader rdrfsr = updateFunctionSpaceRequestsProd.ExecuteReader();

                        //HOTEL2
                        var updateFunctionSpaceRequests2 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "', [Venue2Id] = '" + duplicatesRow[0] + "' WHERE [Hotel2Id] = '" + duplicatesRow[13] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd2 = new SqlCommand(updateFunctionSpaceRequests2, con);
                        SqlDataReader rdrfsr2 = updateFunctionSpaceRequestsProd2.ExecuteReader();

                        //HOTEL3
                        var updateFunctionSpaceRequests3 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "', [Venue3Id] = '" + duplicatesRow[0] + "' WHERE [Hotel3Id] = '" + duplicatesRow[13] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd3 = new SqlCommand(updateFunctionSpaceRequests3, con);
                        SqlDataReader rdrfsr3 = updateFunctionSpaceRequestsProd3.ExecuteReader();

                        //HOTEL ASSIGNED
                        var updateFunctionSpaceRequests4 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge3', [ModifiedDate] = '" + DateTime.Now + "', [VenueAssignedId] = '" + duplicatesRow[0] + "' WHERE [HotelAssignedId] = '" + duplicatesRow[13] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd4 = new SqlCommand(updateFunctionSpaceRequests4, con);
                        SqlDataReader rdrfsr4 = updateFunctionSpaceRequestsProd4.ExecuteReader();

                        migrationCon.Close();
                    }

                    //------------NON-DUPLICATE VENUES & HOTELS------------//
                    SqlCommand nonDuplicateHotelsCmd = new SqlCommand(queryNonDuplicateHotels, con);
                    DataTable nonDuplicatesTable = new DataTable();
                    nonDuplicatesTable.Load(nonDuplicateHotelsCmd.ExecuteReader());

                    foreach (DataRow nonDuplicatesRow in nonDuplicatesTable.Rows)
                    {
                        //insert venue and hotel name into migration table
                        Guid id = Guid.NewGuid();
                        var queryInsertMigration = @"INSERT INTO [ELAMigration].[dbo].[VenueHotel]
                        ([ID]
                        ,[HotelID]
                        ,[HotelName]
                        ,[VenueID]
                        ,[VenueName]
                        ,[NewVenue])";
                        queryInsertMigration += "VALUES('" + id + "','" + nonDuplicatesRow[0].ToString() + "','" + (nonDuplicatesRow[4].ToString().Replace("'", "/")) + "','" + null + "','" + null + "','" + true + "')";
                        SqlCommand addEntry = new SqlCommand(queryInsertMigration, migrationCon);
                        //addEntry.CommandText = queryHotelVenue;
                        migrationCon.Open();
                        SqlDataReader insertMigrationRdr = addEntry.ExecuteReader();
                        migrationCon.Close();

                        //    //insert new venue 
                        var location = nonDuplicatesRow[2].ToString() + " " + nonDuplicatesRow[6].ToString();
                        var vid = Guid.NewGuid();
                        var queryNewVenue = @"INSERT INTO [dbo].[Venues]
                        ([ID]
                        ,[Location]
                        ,[Name]
                        ,[CreatedBy]
                        ,[CreatedDate]
                        ,[Address]
                        ,[City]
                        ,[State]
                        ,[Zip])";
                        queryNewVenue += "VALUES('" + vid + "','" + location + "','" + (nonDuplicatesRow[4].ToString().Replace("'", "/")) + "','" + "Merge Process" + "','" + DateTime.Now + "','" + nonDuplicatesRow[1] + "','" +
                            nonDuplicatesRow[2] + "','" + nonDuplicatesRow[6] + "','" + nonDuplicatesRow[7] + "')";
                        SqlCommand addVenue = new SqlCommand(queryNewVenue, con);
                        //addEntry.CommandText = queryHotelVenue;
                        SqlDataReader rdr6 = addVenue.ExecuteReader();

                        //insert convention venue
                        
                        Guid? newcvid = Guid.Empty;
                        var conventionId = nonDuplicatesRow[3].ToString();
                        if ((conventionId != null) || (conventionId == ""))
                        {
                            newcvid = Guid.NewGuid();
                        var insertcv = @"INSERT INTO [dbo].[ConventionVenues]
                        ([Id]
                        ,[ConventionId]
                        ,[CreatedBy]
                        ,[CreatedDate]
                        ,[ModifiedDate]
                        ,[IsFunctionApproved]
                        ,[SalesTaxPercent]
                        ,[ServiceChargeIsTaxable]
                        ,[ServiceChargePercentTaxable]
                        ,[ServiceChargePercentNonTaxable]
                        ,[VenueId])";
                        insertcv += "VALUES('" + newcvid + "','" + nonDuplicatesRow[3] + "','Merge Process','" + DateTime.Now + "','" + DateTime.Now + "','" + false + "','0','0','0','0','" + vid + "')";
                        SqlCommand insertcvprod = new SqlCommand(insertcv, con);
                        SqlDataReader rdrcv = insertcvprod.ExecuteReader();
                        }
                        

                        //get masterrooms first 

                        var queryGetMasterRooms = @"SELECT * FROM [dbo].[HotelRooms] WHERE [HotelId] = '" + nonDuplicatesRow[0] + "' AND [IsMasterRoom] = 'true'";
                        SqlCommand getMasterRooms = new SqlCommand(queryGetMasterRooms, con);
                        DataTable getMasterRoomsTable = new DataTable();
                        getMasterRoomsTable.Load(getMasterRooms.ExecuteReader());
                        foreach (DataRow masterRoomsRow in getMasterRoomsTable.Rows)
                        {
                            Guid newVenueRoomId = Guid.NewGuid();

                            //insert venue rooms and hotel rooms name into migration database
                            Guid vrid = Guid.NewGuid();
                            var queryRooms = @"INSERT INTO [ELAMigration].[dbo].[Rooms]
                                                ([ID]
                                                ,[HotelRoomID]
                                                ,[HotelRoomName]
                                                ,[VenueRoomID]
                                                ,[VenueRoomName]
                                                ,[NewRoom])";
                            queryRooms += "VALUES('" + vrid + "','" + masterRoomsRow[0].ToString() + "','" + masterRoomsRow[11].ToString() + "','" +
                                newVenueRoomId + "','" + masterRoomsRow[11].ToString().Replace("'", "/") + "','" + true + "')";
                            SqlCommand addRoom = new SqlCommand(queryRooms, migrationCon);
                            //addEntry.CommandText = queryHotelVenue;
                            migrationCon.Open();
                            SqlDataReader queryRoomsRdr = addRoom.ExecuteReader();

                            // insert new venue rooms
                            var queryInsertRooms = @"INSERT INTO [dbo].[VenueRooms]
                            ( [Id]
                                ,[RoomNumber]
                                ,[RoomSquareFeet]
                                ,[VenueId]    
                                ,[CreatedBy]
                                ,[CreatedDate]
                                ,[ConventionVenueId]
                                ,[BanquetCapacity]
                                ,[CeilingHeight]
                                ,[ClassroomCapacity]
                                ,[ConferenceCapacity]
                                ,[Dimensions]
                                ,[HollowSqCapacity]
                                ,[OtherCapacity]
                                ,[ReceptionCapacity]
                                ,[SquareFeet]
                                ,[TheatreCapacity]
                                ,[UshapeCapacity]
                                ,[IsMasterRoom]";

                            var sqft = masterRoomsRow[15].ToString().Replace(",", "");
                            int sq = 0;
                            if (sqft.Length == 0)
                            { sq = 0; }
                            else if (sqft == "n/a")
                            { sq = 0; }
                            else int.TryParse(sqft, out sq);                            
                          
                            bool master = (bool)masterRoomsRow[9];
                          
                            queryInsertRooms += ")VALUES('" + newVenueRoomId + "','" + masterRoomsRow[11].ToString().Replace("'", "/") + "','" + sq + "','" + vid +
                                                   "','Merge Process','" + DateTime.Now + "','" + newcvid + "','" + masterRoomsRow[1] + "','" + masterRoomsRow[2] + "','" + masterRoomsRow[3] + "','" +
                                                   masterRoomsRow[4] + "','" + masterRoomsRow[6] + "','" + masterRoomsRow[7] + "','" + masterRoomsRow[13] + "','" + masterRoomsRow[14] + "','" +
                                                   sq + "','" + masterRoomsRow[16] + "','" + masterRoomsRow[17] + "','" + masterRoomsRow[9] + "')";

                            SqlCommand insertVenueRooms = new SqlCommand(queryInsertRooms, con);
                            SqlDataReader rdrVenueRooms = insertVenueRooms.ExecuteReader();

                            //update contracted spaces
                            var queryUpdatecs = @"UPDATE [dbo].[ContractedSpaces]
                        SET [ModifiedBy] = 'Merge Process' ,[ModifiedDate] = '" + DateTime.Now + "',[ConventionVenueId] = '" + newcvid + "', [VenueId] = '" + vid + "', [VenueRoomId] = '" + newVenueRoomId +
                    "' WHERE [HotelRoomId] = '" + masterRoomsRow[0] + "'";
                            SqlCommand updatecs = new SqlCommand(queryUpdatecs, con);
                            SqlDataReader rdrcs1 = updatecs.ExecuteReader();

                            //  update function space request
                            var queryUpdatefsrvr = @"UPDATE [dbo].[FunctionSpaceRequests]
                            SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "', [VenueRoomAssignedId] = '" + newVenueRoomId + "' WHERE [RoomAssignedId] = '" + masterRoomsRow[0] + "'";
                            SqlCommand updatefsrvr = new SqlCommand(queryUpdatefsrvr, con);
                            SqlDataReader rdrfsrvr = updatefsrvr.ExecuteReader();
                        }


                        //get rooms
                        var queryGetRooms = @"SELECT * FROM [dbo].[HotelRooms] WHERE [HotelId] = '" + nonDuplicatesRow[0] + "' AND IsMasterRoom = 'false' ORDER BY [CreatedDate] ASC";
                        SqlCommand getRooms = new SqlCommand(queryGetRooms, con);
                        DataTable getRoomsTable = new DataTable();
                        getRoomsTable.Load(getRooms.ExecuteReader());
                        foreach (DataRow roomsRow in getRoomsTable.Rows)
                        {
                            Guid newVenueRoomId = Guid.NewGuid();

                            //insert venue rooms and hotel rooms name into migration database
                            Guid vrid = Guid.NewGuid();
                            var queryRooms = @"INSERT INTO [ELAMigration].[dbo].[Rooms]
                                                ([ID]
                                                ,[HotelRoomID]
                                                ,[HotelRoomName]
                                                ,[VenueRoomID]
                                                ,[VenueRoomName]
                                                ,[NewRoom])";
                            queryRooms += "VALUES('" + vrid + "','" + roomsRow[0].ToString() + "','" + roomsRow[11].ToString() + "','" +
                                newVenueRoomId + "','" + roomsRow[11].ToString()+ "','" + true + "')";
                            SqlCommand addRoom = new SqlCommand(queryRooms, migrationCon);
                            //addEntry.CommandText = queryHotelVenue;
                            migrationCon.Open();
                            SqlDataReader queryRoomsRdr = addRoom.ExecuteReader();

                            // insert new venue rooms
                            var queryInsertRooms = @"INSERT INTO [dbo].[VenueRooms]
                            ( [Id]
                                ,[RoomNumber]
                                ,[RoomSquareFeet]
                                ,[VenueId]    
                                ,[CreatedBy]
                                ,[CreatedDate]
                                ,[ConventionVenueId]
                                ,[BanquetCapacity]
                                ,[CeilingHeight]
                                ,[ClassroomCapacity]
                                ,[ConferenceCapacity]
                                ,[Dimensions]
                                ,[HollowSqCapacity]
                                ,[OtherCapacity]
                                ,[ReceptionCapacity]
                                ,[SquareFeet]
                                ,[TheatreCapacity]
                                ,[UshapeCapacity]
                                ,[IsMasterRoom]";

                            var sqft = roomsRow[15].ToString().Replace(",", "");
                            int sq = 0;
                            if (sqft.Length == 0)
                            { sq = 0; }
                            else if (sqft == "n/a")
                            { sq = 0; }
                            else
                            { sq = Convert.ToInt32(sqft); }
                            //queryInsertRooms += ")VALUES('" + newVenueRoomId + "','" + roomsRow[11].ToString() + "','" + sq + "','" + vid +
                            //                        "','Merge Process','" + DateTime.Now + "','" + newcvid + "','" + roomsRow[1] + "','" + roomsRow[2] + "','" + roomsRow[3] + "','" +
                            //                        roomsRow[4] + "','" + roomsRow[6] + "','" + roomsRow[7] + "','" + roomsRow[13] + "','" + roomsRow[14] + "','" +
                            //                        sq + "','" + roomsRow[16] + "','" + roomsRow[17] + "','" + roomsRow[9] + "')";

                            // venue rooms master room id

                            bool master = (bool)roomsRow[9];
                            Guid? masterid = null;
                            if (master == false)
                            {
                                //get master room name 
                                var getmasterroom = @"SELECT TOP 1 * FROM[dbo].[HotelRooms] INNER JOIN [dbo].[VenueRooms] ON [HotelRooms].Name = [VenueRooms].RoomNumber WHERE [HotelRooms].[Id] = '" + roomsRow[10] + "'";
                                SqlCommand masterId = new SqlCommand(getmasterroom, con);
                                DataTable masterTable = new DataTable();
                                masterTable.Load(masterId.ExecuteReader());
                                foreach (DataRow masterRow in masterTable.Rows)
                                {
                                    queryInsertRooms += ", [MasterRoomId]";
                                    masterid = (Guid)masterRow[22];   
                                    if (masterid == null)
                                    { masterid = null; }
                                }
                            }

                            queryInsertRooms += ")VALUES('" + newVenueRoomId + "','" + roomsRow[11].ToString() + "','" + sq + "','" + vid +
                                                   "','Merge Process','" + DateTime.Now + "','" + newcvid + "','" + roomsRow[1] + "','" + roomsRow[2] + "','" + roomsRow[3] + "','" +
                                                   roomsRow[4] + "','" + roomsRow[6] + "','" + roomsRow[7] + "','" + roomsRow[13] + "','" + roomsRow[14] + "','" +
                                                   sq + "','" + roomsRow[16] + "','" + roomsRow[17] + "','" + roomsRow[9] + "'";

                            if (master == false)
                            { queryInsertRooms += ",'" + masterid + "'"; }

                            queryInsertRooms += ")";

                            SqlCommand insertVenueRooms = new SqlCommand(queryInsertRooms, con);
                            SqlDataReader rdrVenueRooms = insertVenueRooms.ExecuteReader();

                            //update contracted spaces
                            var queryUpdatecs = @"UPDATE [dbo].[ContractedSpaces]
                        SET [ModifiedBy] = 'Merge Process' ,[ModifiedDate] = '" + DateTime.Now + "',[ConventionVenueId] = '" + newcvid + "', [VenueId] = '" + vid + "', [VenueRoomId] = '" + newVenueRoomId +
                    "' WHERE [HotelRoomId] = '" + roomsRow[0] + "'";
                            SqlCommand updatecs = new SqlCommand(queryUpdatecs, con);
                            SqlDataReader rdrcs1 = updatecs.ExecuteReader();

                            //  update function space request
                            var queryUpdatefsrvr = @"UPDATE [dbo].[FunctionSpaceRequests]
                            SET [ModifiedBy] = 'Merge3' ,[ModifiedDate] = '" + DateTime.Now + "', [VenueRoomAssignedId] = '" + newVenueRoomId + "' WHERE [RoomAssignedId] = '" + roomsRow[0] + "'";
                            SqlCommand updatefsrvr = new SqlCommand(queryUpdatefsrvr, con);
                            SqlDataReader rdrfsrvr = updatefsrvr.ExecuteReader();
                        }

                        //update hotel contacts
                        var updateHotelContacts = @"UPDATE [dbo].[HotelContacts]
                            SET [ModifiedBy] = 'Merge Process', [ModifiedDate] = '" + DateTime.Now + "', [VenueId] = '" + vid + "' WHERE [HotelId] = '" + nonDuplicatesRow[0] + "'";
                        SqlCommand updateHotelContactsProd = new SqlCommand(updateHotelContacts, con);
                        SqlDataReader rdrhc = updateHotelContactsProd.ExecuteReader();

                        //update function space requests
                        //HOTEL1
                        var updateFunctionSpaceRequests = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge Process', [ModifiedDate] = '" + DateTime.Now + "', [Venue1Id] = '" + vid + "' WHERE [Hotel1Id] = '" + nonDuplicatesRow[0] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd = new SqlCommand(updateFunctionSpaceRequests, con);
                        SqlDataReader rdrfsr = updateFunctionSpaceRequestsProd.ExecuteReader();

                        //HOTEL2
                        var updateFunctionSpaceRequests2 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge Process', [ModifiedDate] = '" + DateTime.Now + "', [Venue2Id] = '" + vid + "' WHERE [Hotel2Id] = '" + nonDuplicatesRow[0] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd2 = new SqlCommand(updateFunctionSpaceRequests2, con);
                        SqlDataReader rdrfsr2 = updateFunctionSpaceRequestsProd2.ExecuteReader();

                        //HOTEL3
                        var updateFunctionSpaceRequests3 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge Process', [ModifiedDate] = '" + DateTime.Now + "', [Venue3Id] = '" + vid + "' WHERE [Hotel3Id] = '" + nonDuplicatesRow[0] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd3 = new SqlCommand(updateFunctionSpaceRequests3, con);
                        SqlDataReader rdrfsr3 = updateFunctionSpaceRequestsProd3.ExecuteReader();

                        //HOTEL ASSIGNED
                        var updateFunctionSpaceRequests4 = @"UPDATE [dbo].[FunctionSpaceRequests]
                        SET [ModifiedBy] = 'Merge Process', [ModifiedDate] = '" + DateTime.Now + "', [VenueAssignedId] = '" + vid + "' WHERE [HotelAssignedId] = '" + nonDuplicatesRow[0] + "'";
                        SqlCommand updateFunctionSpaceRequestsProd4 = new SqlCommand(updateFunctionSpaceRequests4, con);
                        SqlDataReader rdrfsr4 = updateFunctionSpaceRequestsProd4.ExecuteReader();


                        ////update venue room master room id 
                        ////get rooms
                        //var queryGetRooms2 = @"SELECT * FROM [dbo].[HotelRooms] WHERE [HotelId] = '" + nonDuplicatesRow[0] + "'";
                        //SqlCommand getRooms2 = new SqlCommand(queryGetRooms2, con);
                        //DataTable getRoomsTable2 = new DataTable();
                        //getRoomsTable2.Load(getRooms2.ExecuteReader());
                        //foreach (DataRow roomsRow2 in getRoomsTable2.Rows)
                        //{                           
                        //    //update venue rooms master room id

                        //    bool master = (bool)roomsRow2[9];

                        //    if (master == false)
                        //    {
                        //        //get master room name 
                        //        var getmasterroom = @"SELECT * FROM[dbo].[HotelRooms] INNER JOIN [dbo].[VenueRooms] ON [HotelRooms].Name = [VenueRooms].RoomNumber WHERE [HotelRooms].[Id] = '" + roomsRow2[0] + "'";
                        //        SqlCommand masterId = new SqlCommand(getmasterroom, con);
                        //        DataTable masterTable = new DataTable();
                        //        masterTable.Load(masterId.ExecuteReader());
                        //        foreach (DataRow masterRow in masterTable.Rows)
                        //        {
                        //            var queryUpdateRooms = @"UPDATE [dbo].[VenueRooms]
                        // SET [MasterRoomId] = '" + masterRow[22] + "',[ModifiedBy] = 'Master Room'";

                        //    SqlCommand insertVenueRooms = new SqlCommand(queryUpdateRooms, con);
                        //    SqlDataReader rdrVenueRooms = insertVenueRooms.ExecuteReader();
                        //        }
                        //    }
                        //}


                        var hotel = new VenuesProd();
                        var hotelname = nonDuplicatesRow[4];
                        hotel.Name = hotelname.ToString();
                        listVenuesProdModel.Add(hotel);

                        //DataTable tb = new DataTable();
                        //tb.Load(rdr2);
                        //    listVenuesProdModel.Add(new VenuesProd
                        //{
                        //    Name = rdr2[2].ToString()
                        //});                      
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return listVenuesProdModel;
        }

        public static string StripHTML(string strHtml)
        {
            //// strips off all non-ASCII characters
            //strHtml = strHtml.Replace("<(.|\n)*?>", "");

            //// erases all the ASCII control characters
            //strHtml = strHtml.Replace("&amp;", "&");

            //return strHtml.Trim();
            if (strHtml != null)
            {
                string strText = Regex.Replace(strHtml, "<(.|\n)*?>", String.Empty);
                strText = System.Web.HttpUtility.HtmlDecode(strText);
                strText = Regex.Replace(strText, @"\s+", " ");
                strText = Regex.Replace(strText, @"&amp;", "&");
                strText = Regex.Replace(strText, @"'", "");
                return strText;
            }
            else
            {
                return " ";
            }
        }
    }
}
