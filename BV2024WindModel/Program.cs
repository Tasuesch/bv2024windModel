using System;
using BV2024WindModel.Logic;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Linq;
using BV2024WindModel.Data;
using System.Threading;
using System.Diagnostics;

namespace BV2024WindModel
{

    public class Windmodel
    {

        static void Main(string[] args)
        {
           
            var containersFromFile = ReadCSV.ReadFromCsv("C:\\windLoadFiles\\wind9.csv");

            var calculator = new BV2024WindCalculator();

            var windExposedFrontSurfaces = calculator.Calculate(containersFromFile);

            var windCalculationResults = WindCalculationResultFactory.Create(windExposedFrontSurfaces);
           
            string windResultsSerialized = JsonConvert.SerializeObject(windCalculationResults, Formatting.Indented);
            


            //write string to file
            System.IO.File.WriteAllText(@"C:\windLoadFiles\wind9Results.txt", windResultsSerialized);

            Console.WriteLine("Exposed ");
            foreach (var windExposedFrontSurface in windExposedFrontSurfaces)
            {
                Console.WriteLine($"X= {windExposedFrontSurface.Coordinate}, Area= {windExposedFrontSurface.Area:f06}");
            }

            /*Parallel.ForEach(aftSurfaces, aftSurface =>
             {
                 var protectingPolygonsAtCoordinate = inflator.InflateContainers(aftSurface.Polygons, 0.3);
                 aftInflatedProtectingSurfacesBag.Add(new Surface(aftSurface.Coordinate, protectingPolygonsAtCoordinate));
             });*/

            Console.Read();

        }

    }


}
         