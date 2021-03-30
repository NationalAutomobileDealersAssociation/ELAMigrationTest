using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using ELAMigrationTest.Venue;
using ELAMigrationTest.Model;
using System.Data;

namespace ELAMigrationTest
{
    class Program
    {
        private static IConfiguration _iconfiguration;
        static void Main(string[] args)
        {
            GetAppSettingsFile();
            PrintProd();         
        }

        static void GetAppSettingsFile()
        {
            var builder = new ConfigurationBuilder()
                                 .SetBasePath("C:/Users/yneelam/Documents/GitHub/ELAMigrationTest")
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _iconfiguration = builder.Build();
        }

        static void PrintProd()
        {
            var venues = new ProdList(_iconfiguration);
            var listVenuesProdModel = venues.GetList();
            listVenuesProdModel.ForEach(item =>
            {
                Console.WriteLine(item.Name);
            });
            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();
        }
    }
}