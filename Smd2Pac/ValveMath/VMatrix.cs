using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac.ValveMath
{
    // 3x4 matrix with functions from the 2004 Episode 1 SDK
    public struct VMatrix
    {
        /* Source init reference:
         * The visual layout valve used (as shown below) suggests this is a row-major matrix
         
             m_flMatVal[0][0] = m00; m_flMatVal[0][1] = m01; m_flMatVal[0][2] = m02; m_flMatVal[0][3] = m03;
             m_flMatVal[1][0] = m10; m_flMatVal[1][1] = m11; m_flMatVal[1][2] = m12; m_flMatVal[1][3] = m13;
             m_flMatVal[2][0] = m20; m_flMatVal[2][1] = m21; m_flMatVal[2][2] = m22; m_flMatVal[2][3] = m23;

         */

        public float M00, M01, M02, M03;
        public float M10, M11, M12, M13;
        public float M20, M21, M22, M23;

        public static VMatrix Zero { get { return new VMatrix(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); } }

        public VMatrix(float m00, float m01, float m02, float m03,
                        float m10, float m11, float m12, float m13,
                        float m20, float m21, float m22, float m23)
        {
            M00 = m00; M01 = m01; M02 = m02; M03 = m03;
            M10 = m10; M11 = m11; M12 = m12; M13 = m13;
            M20 = m20; M21 = m21; M22 = m22; M23 = m23;
        }

        // Originally: void MatrixAngles( const matrix3x4_t& matrix, float* angles )
        public Vector3 ToEngineAngles()
        {
            Vector3 qangles;

            Vector3 forward;
            Vector3 left;
            Vector3 up;

            //
            // Extract the basis vectors from the matrix. Since we only need the Z
            // component of the up vector, we don't get X and Y.
            //
            forward.X = M00;
            forward.Y = M10;
            forward.Z = M20;
            left.X = M01;
            left.Y = M11;
            left.Z = M21;
            up.Z = M22;
  
            float xyDist = (float)Math.Sqrt((forward.X * forward.X) + (forward.Y * forward.Y));
     
            // enough here to get angles?
            if (xyDist > 0.001f)
            {
                // (yaw)    y = ATAN( forward.y, forward.x );       -- in our space, forward is the X axis
                qangles.Y = (float)Math.Atan2(forward.Y, forward.X);

                // The engine does pitch inverted from this, but we always end up negating it in the DLL
                // UNDONE: Fix the engine to make it consistent
                // (pitch)  x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
                qangles.X = (float)Math.Atan2(-forward.Z, xyDist);
  
                // (roll)   z = ATAN( left.z, up.z );
                qangles.Z = (float)Math.Atan2(left.Z, up.Z);
            }
            else    // forward is mostly Z, gimbal lock-
            {
                // (yaw)    y = ATAN( -left.x, left.y );            -- forward is mostly z, so use right for yaw
                qangles.Y = (float)Math.Atan2(-left.X, left.Y);
  
                // The engine does pitch inverted from this, but we always end up negating it in the DLL
                // UNDONE: Fix the engine to make it consistent
                // (pitch)  x = ATAN( -forward.z, sqrt(forward.x*forward.x+forward.y*forward.y) );
                qangles.X = (float)Math.Atan2(-forward.Z, xyDist);
  
                // Assume no roll in this case as one degree of freedom has been lost (i.e. yaw == roll)
                qangles.Z = 0;
            }

            return qangles;
         }
    }
}
