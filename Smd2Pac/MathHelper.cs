using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public static class MathHelper
    {
        public static float PiOver2 { get { return (float)(Math.PI / 2f); } }

        public static float ToDegrees(float radians)
        {
            return (float)(radians * 180.0 / Math.PI);
        }

        public static float ToRadians(float degrees)
        {
            return (float)(degrees * Math.PI / 180.0);
        }

        public static float Normalize(float val, float min, float max)
        {
            max -= min;
            if (max == 0)
                return min;
            
            val = ((val - min) % max) + min;
            while (val < min)
                val += max;

            return val;
        }
    }
}
