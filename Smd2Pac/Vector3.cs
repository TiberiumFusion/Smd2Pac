using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    // Basic Vector3, XNA style
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;
        
        public Vector3 Zero { get { return new Vector3(0, 0, 0); } }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(float all)
        {
            X = all;
            Y = all;
            Z = all;
        }

        public Vector3(Vector3 source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }
        
        public float Length()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }
        
        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.X == b.X
                   && a.Y == b.Y
                   && a.Z == b.Z;
        }

        public override bool Equals(object b)
        {
            if (!(b is Vector3))
                return false;

            Vector3 bv = (Vector3)b;
            return X == bv.X && Y == bv.Y && Z == bv.Z;
        }

        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !(a == b);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            a.X += b.X;
            a.Y += b.Y;
            a.Z += b.Z;
            return a;
        }

        public static Vector3 operator -(Vector3 a)
        {
            return new Vector3(-a.X, -a.Y, -a.Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            a.X -= b.X;
            a.Y -= b.Y;
            a.Z -= b.Z;
            return a;
        }

        public static Vector3 operator *(Vector3 a, Vector3 b)
        {
            a.X *= b.X;
            a.Y *= b.Y;
            a.Z *= b.Z;
            return a;
        }

        public static Vector3 operator *(Vector3 a, float b)
        {
            a.X *= b;
            a.Y *= b;
            a.Z *= b;
            return a;
        }

        public static Vector3 operator *(float a, Vector3 b)
        {
            b.X *= a;
            b.Y *= a;
            b.Z *= a;
            return b;
        }

        public static Vector3 operator /(Vector3 a, Vector3 b)
        {
            a.X /= b.X;
            a.Y /= b.Y;
            a.Z /= b.Z;
            return a;
        }

        public static Vector3 operator /(Vector3 a, float b)
        {
            a.X /= b;
            a.Y /= b;
            a.Z /= b;
            return a;
        }

        public static Vector3 operator /(float a, Vector3 b)
        {
            return new Vector3(a / b.X, a / b.Y, a / b.Z);
        }

        public override string ToString()
        {
            return "{X: " + X + ", Y: " + Y + ", Z: " + Z + "}";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }
}
