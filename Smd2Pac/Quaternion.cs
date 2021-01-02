using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public struct Quaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Quaternion(Quaternion source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = source.W;
        }
        
        public static Quaternion Multiply(Quaternion q, Quaternion p)
        {
            Quaternion r;
            float qX = q.X;
            float qY = q.Y;
            float qZ = q.Z;
            float qW = q.W;
            float pX = p.X;
            float pY = p.Y;
            float pZ = p.Z;
            float pW = p.W;
            float a = (qY * pZ) - (qZ * pY);
            float b = (qZ * pX) - (qX * pZ);
            float c = (qX * pY) - (qY * pX);
            float d = ((qX * pX) + (qY * pY)) + (qZ * pZ);
            r.X = ((qX * pW) + (pX * qW)) + a;
            r.Y = ((qY * pW) + (pY * qW)) + b;
            r.Z = ((qZ * pW) + (pZ * qW)) + c;
            r.W = (qW * pW) - d;
            return r;
        }

        public static Quaternion Multiply(Quaternion q, float s)
        {
            Quaternion r;
            r.X = q.X * s;
            r.Y = q.Y * s;
            r.Z = q.Z * s;
            r.W = q.W * s;
            return r;
        }

        public Quaternion Inverse()
        {
            Quaternion r;
            float n = (X * X) + (Y * Y) + (Z * Z) + (W * W);
            float ni = 1f / n;
            r.X = -X * ni;
            r.Y = -Y * ni;
            r.Z = -Z * ni;
            r.W = W * ni;
            return r;
        }

        public static Quaternion FromYawPitchRoll(float yaw, float pitch, float roll) // In radians
        {
            float sinY = (float)Math.Sin(yaw * 0.5f);
            float cosY = (float)Math.Cos(yaw * 0.5f);
            float sinP = (float)Math.Sin(pitch * 0.5f);
            float cosP = (float)Math.Cos(pitch * 0.5f);
            float sinR = (float)Math.Sin(roll * 0.5f);
            float cosR = (float)Math.Cos(roll * 0.5f);
            return new Quaternion((cosY * sinP * cosR) + (sinY * cosP * sinR),
                                  (sinY * cosP * cosR) - (cosY * sinP * sinR),
                                  (cosY * cosP * sinR) - (sinY * sinP * cosR),
                                  (cosY * cosP * cosR) + (sinY * sinP * sinR));
        }

        public void ToYawPitchRoll(out float yaw, out float pitch, out float roll) // In radians
        {
            Matrix m = Matrix.FromQuaternion(this);

            float forwardY = -m.M32;
            if (forwardY <= -1.0f)
                pitch = -MathHelper.PiOver2;
            else if (forwardY >= 1.0f)
                pitch = MathHelper.PiOver2;
            else
                pitch = (float)Math.Asin(forwardY);

            if (forwardY > 0.9999f)
            {
                yaw = 0f;
                roll = (float)Math.Atan2(m.M13, m.M11);
            }
            else
            {
                yaw = (float)Math.Atan2(m.M31, m.M33);
                roll = (float)Math.Atan2(m.M12, m.M22);
            }
        }

        public static Quaternion operator *(Quaternion q, Quaternion p) // p on the left, q on the right
        {
            return Multiply(q, p);
        }
        
        public static Quaternion operator *(Quaternion q, float s)
        {
            return Multiply(q, s);
        }
        
        public static bool operator ==(Quaternion a, Quaternion b)
        {
            return a.X == b.X
                   && a.Y == b.Y
                   && a.Z == b.Z
                   && a.W == b.W;
        }

        public override bool Equals(object b)
        {
            if (!(b is Quaternion))
                return false;

            Quaternion bq = (Quaternion)b;
            return X == bq.X && Y == bq.Y && Z == bq.Z && W == bq.W;
        }

        public static bool operator !=(Quaternion a, Quaternion b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return "{X: " + X + ", Y: " + Y + ", Z: " + Z + ", W: " + W + "}";
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
        }
    }
}
