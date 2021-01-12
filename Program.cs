using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using ELAMigrationTest.Venue;
using ELAMigrationTest.Model;

namespace ELAMigrationTest
{
    class Program
    {
        private static IConfiguration _iconfiguration;
        static void Main(string[] args)
        {
            GetAppSettingsFile();
            PrintProd();
            PrintVenues();
        }

        static void GetAppSettingsFile()
        {
            var builder = new ConfigurationBuilder()
                                 .SetBasePath("C:/Users/yneelam/Desktop/ELAMigrationTest")
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _iconfiguration = builder.Build();
        }
        static void PrintVenues()
        {
            var venues = new VenueHotel(_iconfiguration);
            var listVenueModel = venues.GetList();
            listVenueModel.ForEach(item =>
            {
                Console.WriteLine(item.ID + " " +item.HotelID + " " + item.HotelName + " " + item.VenueID + " " + item.VenueName + " " + item.NewVenue);
            });
            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();
        }

        static void PrintProd()
        {
            var venues = new ProdList(_iconfiguration);
            var listVenueModel = venues.GetList();
            listVenueModel.ForEach(item =>
            {
                Console.WriteLine(item.Id + " " + item.Name + " " + item.Location);
            });
            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();
        }
    }
}