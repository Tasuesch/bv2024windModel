using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using BV2024WindModel.Abstractions;
using Clipper2Lib;
using Macs3.Core.Mathematics.GeneralPolygonClipperLibrary;


namespace BV2024WindModel.Logic
{
    public class BV2024WindCalculator : ICalculator<IEnumerable<Container>, IEnumerable<Surface>>
    {
        public IEnumerable<Surface> Calculate(in IEnumerable<Container> input)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var containers = input.ToList();
            var frontSurfaces = containers.GroupBy(container => container.FrontSurface.Coordinate, container => container.FrontSurface.Polygon,
                (key, g) => new PolygonsAtCoordinate { Coordinate = key, Polygons = g.ToList() }).ToList();

            var aftProtectingSurfaces = GetProtectingSurfaces(containers, frontSurfaces);

            var building = new Building(212.65, 0, 29, 14.3, 50, 33);

            //var buildingFrontPolygons = new List<PolyDefault>();
            //buildingFrontPolygons.Add(building.FrontSurface.Polygon);
           // frontSurfaces.Add(new PolygonsAtCoordinate { Coordinate = building.FrontSurface.Coordinate, Polygons = buildingFrontPolygons });
            aftProtectingSurfaces.Add(building.AftSurface);
            stopWatch.Stop();
            Console.WriteLine($"Data preparation time {stopWatch.ElapsedMilliseconds}ms");
            stopWatch.Restart();
            double alpha = 25;
            var windExposedFrontSurfaces = GetWindExposedSurfaces(alpha, frontSurfaces, aftProtectingSurfaces);
            stopWatch.Stop();
            Console.WriteLine($"Calculation time {stopWatch.ElapsedMilliseconds}ms");
            foreach (var windExposedFrontSurface in aftProtectingSurfaces)
            {
                Console.WriteLine($"X= {windExposedFrontSurface.Coordinate}");
            }
            return windExposedFrontSurfaces;
            
        }

        private static List<Surface> GetWindExposedSurfaces(double alpha, List<PolygonsAtCoordinate> frontSurfaces, List<Surface> aftProtectingSurfaces)
        {
            var windExposedFrontSurfaces = new List<Surface>();

            foreach (var frontSurface in frontSurfaces)
            //Parallel.ForEach(frontSurfaces, frontSurface =>
            {
                Surface windExposedFrontSurface = GetWindExposedSurface(alpha, aftProtectingSurfaces, frontSurface);
                windExposedFrontSurfaces.Add(windExposedFrontSurface);
                
            }//);

            return windExposedFrontSurfaces;
        }

        private static Surface GetWindExposedSurface(double alpha, List<Surface> aftProtectingSurfaces, PolygonsAtCoordinate frontSurface)
        {
            
            var frontSurfacePaths = new PathsD();
            foreach (var polygon in  frontSurface.Polygons)
            //for (var polygonIndex = 0; polygonIndex < frontSurface.Polygon.NumInnerPoly; polygonIndex++)
            {
                var path = new PathD();
                foreach (var point in polygon.Points)
                {
                   path.Add(new PointD(point.X, point.Y ));
                }
                frontSurfacePaths.Add(path);
            }

            foreach (var protectingSurface in aftProtectingSurfaces)
            {
                if (protectingSurface.Coordinate > frontSurface.Coordinate && Math.Abs(protectingSurface.Coordinate - frontSurface.Coordinate) < 75)
                {
                    bool needCalculate = NeedToCalculate(alpha, frontSurface, protectingSurface);

                    if (needCalculate)
                    {
                        var deflatedPaths = PolygonDeflator.DeflatePolygon(protectingSurface, frontSurface.Coordinate, alpha);
                        if (deflatedPaths != null)
                        {
                            frontSurfacePaths = Clipper.Difference(frontSurfacePaths, deflatedPaths, FillRule.NonZero, 8);
                            if (frontSurfacePaths.Count == 0)
                                break;
                           
                        }
                    }
                }
                
            }
            var area = CalcArea(frontSurfacePaths);
            Console.WriteLine($"X= {frontSurface.Coordinate}, Area= {area:f06}");

            var frontSurfacePolygons = new List<PolyDefault>();
            foreach (var frontSurfacePath in frontSurfacePaths)
            {
                var frontSurfacePolygon = new PolyDefault();
                foreach (var point in frontSurfacePath)
                {
                    frontSurfacePolygon.add(point.x , point.y );
                }
                frontSurfacePolygons.Add(frontSurfacePolygon);
            }
            var windExposedFrontSurface = new Surface(frontSurface.Coordinate, frontSurfacePolygons);
            return windExposedFrontSurface;
        }
        static double CalcArea(PathsD paths)
        {
            double totalArea = 0;
            for (int i = 0; i < paths.Count; i++)
                totalArea += Clipper.Area(paths[i]);
            return totalArea;
        }
        private static bool NeedToCalculate(double alpha, PolygonsAtCoordinate frontSurface, Surface protectingSurface)
        {
            var needCalculate = false;

            var containersDist = Math.Abs(protectingSurface.Coordinate - frontSurface.Coordinate);
            var deckHeight = protectingSurface.Polygon.getInnerPoly(0).Bounds.Y;
            var tg = Math.Tan(alpha * (Math.PI / 180));
            var offset = -tg * containersDist;

            var deflatedPolygons = new List<PolyDefault>();

            for (var protectingPolygonIndex = 0; protectingPolygonIndex < protectingSurface.Polygon.NumInnerPoly; protectingPolygonIndex++)
            {
                var innerPoly = protectingSurface.Polygon.getInnerPoly(protectingPolygonIndex);
                if (innerPoly.Bounds.Width > 2 * Math.Abs(offset) && innerPoly.Bounds.Height > Math.Abs(offset))
                {
                    needCalculate = true;
                    break;
                }
            }

            return needCalculate;
        }

        private static List<Surface> GetProtectingSurfaces(IEnumerable<Container> containersFromFile, List<PolygonsAtCoordinate> frontSurfaces)
        {
            var aftSurfaces = containersFromFile.GroupBy(container => container.AftSurface.Coordinate, container => container.AftSurface.Polygon,
                (key, g) => new PolygonsAtCoordinate { Coordinate = key, Polygons = g.ToList() }).ToList();
            var crossingContainers = GetCrossingContainers(containersFromFile, frontSurfaces, aftSurfaces);

            var allAftContainers = new List<PolygonsAtCoordinate>();
            allAftContainers.AddRange(aftSurfaces);
            allAftContainers.AddRange(crossingContainers);

            var allAftSurfaces = allAftContainers.GroupBy(container => container.Coordinate, container => container.Polygons,
                (key, g) => new PolygonsAtCoordinate { Coordinate = key, Polygons = g.SelectMany(x => x).ToList() }).ToList();

            var aftInflatedProtectingSurfacesBag = new ConcurrentBag<Surface>();

            /*Parallel.ForEach(aftSurfaces, aftSurface =>
             {
                 var protectingPolygonsAtCoordinate = inflator.InflateContainers(aftSurface.Polygons, 0.3);
                 aftInflatedProtectingSurfacesBag.Add(new Surface(aftSurface.Coordinate, protectingPolygonsAtCoordinate));
             });*/
            var aftProtectingSurfaces = GetUnitedProtectingSurfaces(allAftSurfaces, aftInflatedProtectingSurfacesBag);

            return aftProtectingSurfaces;
        }

        private static List<Surface> GetUnitedProtectingSurfaces(List<PolygonsAtCoordinate> allAftSurfaces, ConcurrentBag<Surface> aftInflatedProtectingSurfacesBag)
        {
            var inflator = new PolygonInflator();
            var inflatedContainers = new List<PolyDefault>();
            foreach (var aftSurface in allAftSurfaces)
            //Parallel.ForEach(allAftSurfaces, aftSurface =>
            {
                var protectingPolygonsAtCoordinate = inflator.InflateContainers(aftSurface.Polygons, 0.3);
                aftInflatedProtectingSurfacesBag.Add(new Surface(aftSurface.Coordinate, protectingPolygonsAtCoordinate));
            }//);
            var aftProtectingSurfaces = new List<Surface>();
            var aftInflatedProtectingSurfaces = aftInflatedProtectingSurfacesBag.ToList();
            foreach (var aftSurface in aftInflatedProtectingSurfaces)
            //Parallel.ForEach(aftInflatedProtectingSurfaces, aftSurface =>
            {
                var protectingPolygonsAtCoordinate = inflator.InflateContainers(new List<PolyDefault> { aftSurface.Polygon }, -0.3);
                aftProtectingSurfaces.Add(new Surface(aftSurface.Coordinate, protectingPolygonsAtCoordinate));
            }//);

            return aftProtectingSurfaces;
        }

        private static List<PolygonsAtCoordinate> GetCrossingContainers(IEnumerable<Container> containersFromFile, List<PolygonsAtCoordinate> frontSurfaces, List<PolygonsAtCoordinate> aftSurfaces)
        {
            var crossingContainers = new List<PolygonsAtCoordinate>();
            foreach (var frontSurface in frontSurfaces)
            {
                var crossingCoordinate = frontSurface.Coordinate + 0.01;
                var crossingContainersAtCoordinate = containersFromFile.Where(container => IsInside(container, crossingCoordinate));

                if (crossingContainersAtCoordinate.Count() > 0)
                {
                    foreach (var aftSurface in aftSurfaces)
                    {
                        if (aftSurface.Coordinate - crossingCoordinate < 0.1 && crossingCoordinate <= aftSurface.Coordinate)
                        {
                            crossingContainers.Add(new PolygonsAtCoordinate()
                            {
                                Coordinate = aftSurface.Coordinate,
                                Polygons = crossingContainersAtCoordinate.Select(container => PolygonFactory.FromContainer(container)).ToList()
                            });
                        }
                    }
                }
            }

            return crossingContainers;
        }

        private static bool IsInside(Container container, double crossingCoordinate)
        {
            return crossingCoordinate >= container.AftSurface.Coordinate &&
                            crossingCoordinate <= container.FrontSurface.Coordinate;
        }
    }
}

