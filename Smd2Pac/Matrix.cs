using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public struct Matrix
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;
        
        public Matrix Zero { get { return new Matrix(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); } }

        public Matrix(float m11, float m12, float m13, float m14,
                      float m21, float m22, float m23, float m24,
                      float m31, float m32, float m33, float m34,
                      float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m31; M42 = m42; M43 = m43; M44 = m44;
        }

        public static Matrix FromQuaternion(Quaternion q)
        {
            Matrix m;
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float xy = q.X * q.Y;
            float zw = q.Z * q.W;
            float zx = q.Z * q.X;
            float yw = q.Y * q.W;
            float yz = q.Y * q.Z;
            float xw = q.X * q.W;
            m.M11 = 1f - (2f * (yy + zz));
            m.M12 = 2f * (xy + zw);
            m.M13 = 2f * (zx - yw);
            m.M14 = 0f;
            m.M21 = 2f * (xy - zw);
            m.M22 = 1f - (2f * (zz + xx));
            m.M23 = 2f * (yz + xw);
            m.M24 = 0f;
            m.M31 = 2f * (zx + yw);
            m.M32 = 2f * (yz - xw);
            m.M33 = 1f - (2f * (yy + xx));
            m.M34 = 0f;
            m.M41 = 0f;
            m.M42 = 0f;
            m.M43 = 0f;
            m.M44 = 1f;
            return m;
        }

        // From the 2004 Episode 1 SDK
        // Notably different from the way I'm accustomed to doing this
        public static Matrix FromQuaternion_SourceEngine(Quaternion q)
        {
            Matrix m;

            float x2 = q.X + q.X;
            float y2 = q.Y + q.Y;
            float z2 = q.Z + q.Z;
            float xx = q.X * x2;
            float xy = q.X * y2;
            float xz = q.X * z2;
            float yy = q.Y * y2;
            float yz = q.Y * z2;
            float zz = q.Z * z2;
            float wx = q.W * x2;
            float wy = q.W * y2;
            float wz = q.W * z2;

            /*
            m.M11 = 1.0f - (yy + zz);
            m.M12 = xy - wz;
            m.M13 = xz + wy;
            m.M14 = 0f;

            m.M21 = xy + wz;
            m.M22 = 1.0f - (xx + zz);
            m.M23 = yz - wx;
            m.M24 = 0f;

            m.M31 = xz - wy;
            m.M32 = yz + wx;
            m.M33 = 1.0f - (xx + yy);
            m.M34 = 0f;
            */
            
            // Source matrices are column major, not row major
            m.M11 = 1.0f - (yy + zz);
            m.M21 = xy - wz;
            m.M31 = xz + wy;
            m.M14 = 0f;

            m.M12 = xy + wz;
            m.M22 = 1.0f - (xx + zz);
            m.M32 = yz - wx;
            m.M24 = 0f;

            m.M13 = xz - wy;
            m.M23 = yz + wx;
            m.M33 = 1.0f - (xx + yy);
            m.M34 = 0f;

            m.M41 = 0f;
            m.M42 = 0f;
            m.M43 = 0f;
            m.M44 = 1f;

            return m;
        }
    }
}
