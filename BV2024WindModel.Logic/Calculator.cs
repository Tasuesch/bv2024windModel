﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BV2024WindModel.Abstractions;
using Macs3.Core.Mathematics.GeneralPolygonClipperLibrary;


namespace BV2024WindModel.Logic
{
    public class BV2024WindCalculator : ICalculator<IEnumerable<Container>, IEnumerable<Surface>>
    {
        public IEnumerable<Surface> Calculate(in IEnumerable<Container> input)
        {
            var containers = input.ToList();
            var frontSurfaces = containers.GroupBy(container => container.FrontSurface.Coordinate, container => container.FrontSurface.Polygon,
                (key, g) => new PolygonsAtCoordinate { Coordinate = key, Polygons = g.ToList() }).ToList();

            var aftProtectingSurfaces = GetProtectingSurfaces(containers, frontSurfaces);

            var building = new Building(212.65, 0, 29, 14.3, 50, 33);

            var buildingFrontPolygons = new List<PolyDefault>();
            buildingFrontPolygons.Add(building.FrontSurface.Polygon);
            frontSurfaces.Add(new PolygonsAtCoordinate { Coordinate = building.FrontSurface.Coordinate, Polygons = buildingFrontPolygons });
            aftProtectingSurfaces.Add(building.AftSurface);

            double alpha = 25;
            var windExposedFrontSurfaces = GetWindExposedSurfaces(alpha, frontSurfaces, aftProtectingSurfaces);
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
            var windExposedFrontSurface = new Surface(frontSurface.Coordinate, frontSurface.Polygons);

            foreach (var protectingSurface in aftProtectingSurfaces)
            {
                if (protectingSurface.Coordinate > frontSurface.Coordinate && Math.Abs(protectingSurface.Coordinate - frontSurface.Coordinate) < 75)
                {
                    bool needCalculate = NeedToCalculate(alpha, frontSurface, protectingSurface);

                    if (needCalculate)
                    {
                        var deflatedSurface = PolygonDeflator.DeflatePolygon(protectingSurface, frontSurface.Coordinate, alpha);
                        if (deflatedSurface != null)
                        {
                            for (var polygonIndex = 0; polygonIndex < deflatedSurface.Polygon.NumInnerPoly; polygonIndex++)
                            {
                                var innerProjectedPoly = deflatedSurface.Polygon.getInnerPoly(polygonIndex);
                                windExposedFrontSurface.Polygon = windExposedFrontSurface.Polygon.diff(innerProjectedPoly) as PolyDefault;
                                if (windExposedFrontSurface.Polygon.Empty)
                                    break;
                            }
                            if (windExposedFrontSurface.Polygon.Empty)
                                break;
                        }
                    }
                }
                
            }
            return windExposedFrontSurface;
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

