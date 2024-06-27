﻿using Clipper2Lib;


namespace BV2024WindModel.Abstractions
{
    public static class AreaCalculator 
    {
        public static double CalcArea(PathsD paths)
        {
            double totalArea = 0;
            for (int i = 0; i < paths.Count; i++)
                totalArea += Clipper.Area(paths[i]);
            return totalArea;
        }
    }
}

