using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float all)
        {
            X = all;
            Y = all;
            Z = all;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return "{X: " + X + ", Y: " + Y + ", Z: " + Z + "}";
        }
    }
}
