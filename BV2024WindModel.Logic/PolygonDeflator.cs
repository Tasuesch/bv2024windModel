using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using BV2024WindModel.Abstractions;
using Clipper2Lib;
using Macs3.Core.Mathematics.GeneralPolygonClipperLibrary;

namespace BV2024WindModel
{
    public class PolygonDeflator
    {
        public static PathsD DeflatePolygon(Surface protectingSurface, double protectedSurfaceCoordinate, double angle)
        {
            
            var containersDist = Math.Abs(protectingSurface.Coordinate - protectedSurfaceCoordinate);
            var deckHeight = protectingSurface.Polygon.getInnerPoly(0).Bounds.Y;
            var tg = Math.Tan(angle * (Math.PI / 180));
            var offset = -tg * containersDist ;

            //var deflatedPolygons = new List<PolyDefault>();

            //var allDeflatedPaths = new PathsD();
            var paths = new PathsD();
            for (var polygonIndex = 0; polygonIndex < protectingSurface.Polygon.NumInnerPoly; polygonIndex++)
            {
                var innerPoly = protectingSurface.Polygon.getInnerPoly(polygonIndex);

                var path = new PathD();
                foreach (var point in innerPoly.Points)
                {
                    if (point.Y < deckHeight + 0.01)
                    {
                        path.Add(new PointD(point.X, -1000)); 
                    }
                    else
                    {
                        path.Add(new PointD(point.X , point.Y));
                    }
                }
                paths.Add(path);
                /*
                var deflatedPaths = Clipper.InflatePaths(new PathsD { path }, offset, JoinType.Miter, EndType.Polygon, 2.0, 3);

                foreach (var deflatedPath in deflatedPaths)
                { 
                    allDeflatedPaths.Add(deflatedPath);
                }*/

            }
            var allDeflatedPaths = Clipper.InflatePaths(paths, offset, JoinType.Miter, EndType.Polygon, 2.0, 3);

            /*
            foreach (var deflatedPath in deflatedPaths)
            {
                var deflatedPolygon = new PolyDefault();
                foreach (var point in deflatedPath)
                {
                    if (point.Y < -100)
                    {
                        deflatedPolygon.add(point.X / 1000.0, deckHeight - 0.01);
                    }
                    else
                    {
                        deflatedPolygon.add(point.X / 1000.0, point.Y / 1000.0);
                    }
                }
                deflatedPolygons.Add(deflatedPolygon);
            }

            }

            if (deflatedPolygons.Count > 0)
            {
                var deflatedSurface = new Surface(protectedSurfaceCoordinate, deflatedPolygons); 
                return deflatedSurface; 
            }
            */
            if (allDeflatedPaths.Count > 0)
            {
                return allDeflatedPaths;
            }
            return null;
        }
    }

    public class PolygonInflator
    {
        public List<PolyDefault> InflateContainers(List<PolyDefault> polygons, double offset)
        {
            offset = offset / 2 * 1000;
            var inflatedPolygons = new List<PolyDefault>();

            foreach (var polygon in polygons)
            {
                for (var polygonIndex = 0; polygonIndex < polygon.NumInnerPoly; polygonIndex++)
                {
                    var innerPoly = polygon.getInnerPoly(polygonIndex);
                    var path = new Path64();
                    foreach (var point in innerPoly.Points)
                    {
                        path.Add(new Point64(point.X * 1000, point.Y * 1000));
                    }
                    var inflatedPaths = Clipper.InflatePaths(new Paths64 { path }, offset, JoinType.Miter, EndType.Polygon);

                    foreach (var inflatedPath in inflatedPaths)
                    {
                        var inflatedPolygon = new PolyDefault();
                        foreach (var point in inflatedPath)
                        {
                            inflatedPolygon.add(point.X / 1000.0, point.Y / 1000.0);
                        }
                        inflatedPolygons.Add(inflatedPolygon);
                    }
                }
            }
            
            return inflatedPolygons;
        }

    }

}
