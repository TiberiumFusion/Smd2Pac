using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac.ValveMath
{
    // Quaternion with functions from the 2007 SDK
    public struct VQuaternion
    {
        /* Source type layout reference
         * Most notable is the component order: the conventional XYZW instead of the annoying WXYZ

             class Quaternion                // same data-layout as engine's vec4_t,
             {                               //      which is a vec_t[4]
             public:
                 inline Quaternion(void) { 
     
                 // Initialize to NAN to catch errors
             #ifdef _DEBUG
             #ifdef VECTOR_PARANOIA
                     x = y = z = w = VEC_T_NAN;
             #endif
             #endif
                 }
                 inline Quaternion(vec_t ix, vec_t iy, vec_t iz, vec_t iw) : x(ix), y(iy), z(iz), w(iw) { }
                 inline Quaternion(RadianEuler const &angle);    // evil auto type promotion!!!
  
                 inline void Init(vec_t ix=0.0f, vec_t iy=0.0f, vec_t iz=0.0f, vec_t iw=0.0f)    { x = ix; y = iy; z = iz; w = iw; }
  
                 bool IsValid() const;
                 void Invalidate();
  
                 bool operator==( const Quaternion &src ) const;
                 bool operator!=( const Quaternion &src ) const;
  
                 vec_t* Base() { return (vec_t*)this; }
                 const vec_t* Base() const { return (vec_t*)this; }
  
                 // array access...
                 vec_t operator[](int i) const;
                 vec_t& operator[](int i);
  
              vec_t x, y, z, w;

        */


        public float X;
        public float Y;
        public float Z;
        public float W;

        public static VQuaternion Zero { get { return new VQuaternion(0, 0, 0, 0); } }

        public VQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public VQuaternion(VQuaternion source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
            W = source.W;
        }
        
        // Originally: void AngleQuaternion( const RadianEuler &angles, Quaternion &outQuat )
        // "Purpose: Converts radian-euler axis aligned angles to a quaternion
        //  Input  : *pfAngles - Right-handed Euler angles in radians
        //           *outQuat - quaternion of form (i,j,k,real)"
        public static VQuaternion FromRadianEulerAngles(Vector3 rangles) // In RADIANS
        {
            // NOTE: The ordering here is *different* from the AngleQuaternion below
            // because p, y, r are not in the same locations in QAngle + RadianEuler. Yay!
            float sy = (float)Math.Sin(rangles.Z * 0.5f);
            float cy = (float)Math.Cos(rangles.Z * 0.5f);
            float sp = (float)Math.Sin(rangles.Y * 0.5f);
            float cp = (float)Math.Cos(rangles.Y * 0.5f);
            float sr = (float)Math.Sin(rangles.X * 0.5f);
            float cr = (float)Math.Cos(rangles.X * 0.5f);

            VQuaternion outQuat = VQuaternion.Zero;

            // NJS: for some reason VC6 wasn't recognizing the common subexpressions:
            float srXcp = sr * cp, crXsp = cr * sp;
            outQuat.X = srXcp * cy - crXsp * sy; // X
            outQuat.Y = crXsp * cy + srXcp * sy; // Y

            float crXcp = cr * cp, srXsp = sr * sp;
            outQuat.Z = crXcp * sy - srXsp * cy; // Z
            outQuat.W = crXcp * cy + srXsp * sy; // W (real component)

            return outQuat;
        }
        
        // Originally: void AngleQuaternion( const QAngle &angles, Quaternion &outQuat )
        // "Purpose: Converts engine-format euler angles to a quaternion
        //  Input  : angles - Right-handed Euler angles in degrees as follows:
        //               [0]: PITCH: Clockwise rotation around the Y axis.
        //               [1]: YAW:   Counterclockwise rotation around the Z axis.
        //               [2]: ROLL:  Counterclockwise rotation around the X axis.
        //           *outQuat - quaternion of form (i,j,k,real)"
        public static VQuaternion FromQAngles(Vector3 qangles) // In DEGREES!!
        {
            // NOTE: The ordering here is *different* from the AngleQuaternion above
            // because p, y, r are not in the same locations in QAngle + RadianEuler. Yay!
            float sy = (float)Math.Sin(qangles.Y * 0.5f);
            float cy = (float)Math.Cos(qangles.Y * 0.5f);
            float sp = (float)Math.Sin(qangles.X * 0.5f);
            float cp = (float)Math.Cos(qangles.X * 0.5f);
            float sr = (float)Math.Sin(qangles.Z * 0.5f);
            float cr = (float)Math.Cos(qangles.Z * 0.5f);

            VQuaternion outQuat = VQuaternion.Zero;

            // NJS: for some reason VC6 wasn't recognizing the common subexpressions:
            float srXcp = sr * cp, crXsp = cr * sp;
            outQuat.X = srXcp* cy-crXsp* sy; // X
            outQuat.Y = crXsp* cy+srXcp* sy; // Y

            float crXcp = cr * cp, srXsp = sr * sp;
            outQuat.Z = crXcp* sy-srXsp* cy; // Z
            outQuat.W = crXcp* cy+srXsp* sy; // W (real component)

            return outQuat;
        }
        
        // Originally: void QuaternionAlign( const Quaternion &p, const Quaternion &q, Quaternion &qt )
        // "make sure quaternions are within 180 degrees of one another, if not, reverse q"
        public static VQuaternion Align(VQuaternion p, VQuaternion q)
        {
            VQuaternion qt = new VQuaternion(q);

            // "decide if one of the quaternions is backwards"
            float a = (p.X - q.X) * (p.X - q.X)
                      + (p.Y - q.Y) * (p.Y - q.Y)
                      + (p.Z - q.Z) * (p.Z - q.Z)
                      + (p.W - q.W) * (p.W - q.W);
            float b = (p.X + q.X) * (p.X + q.X)
                      + (p.Y + q.Y) * (p.Y + q.Y)
                      + (p.Z + q.Z) * (p.Z + q.Z)
                      + (p.W + q.W) * (p.W + q.W);
            if (a > b)
            {
                qt.X = -q.X;
                qt.Y = -q.Y;
                qt.Z = -q.Z;
                qt.W = -q.W;
            }

            return qt;
        }
        
        // Originally: void QuaternionScale( const Quaternion &p, float t, Quaternion &q )
        public VQuaternion ScaledBy(float t)
        {
            float r;

            // FIXME: nick, this isn't overly sensitive to accuracy, and it may be faster to 
            // use the cos part (w) of the quaternion (sin(omega)*N,cos(omega)) to figure the new scale.
                // I guess in the 3 years since the 2004 SDK, Nick still wouldn't let anyone replace his code
            float sinom = (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
            sinom = (float)Math.Min(sinom, 1f);

            float sinsom = (float)Math.Sin(Math.Asin(sinom) * t);

            t = sinsom / (sinom + float.Epsilon);
            VQuaternion q = new VQuaternion(X * t, Y * t, Z * t, W);

            // rescale rotation
            r = 1.0f - sinsom * sinsom;
  
            // Assert( r >= 0 );
            if (r< 0.0f) 
                r = 0.0f;
            r = (float)Math.Sqrt(r);
  
            // keep sign of rotation
            if (W < 0)
                q.W = -r;
            else
                q.W = r;

            return q;
        }
        
        // Originally: void QuaternionMult( const Quaternion &p, const Quaternion &q, Quaternion &qt )
        // "qt = p * q"
        public VQuaternion MultipliedWith(VQuaternion q)
        {
            // decide if one of the quaternions is backwards
            VQuaternion q2 = Align(this, q);

            VQuaternion qt = VQuaternion.Zero;
            qt.X =  X* q2.W + Y* q2.Z - Z* q2.Y + W* q2.X;
            qt.Y = -X* q2.Z + Y* q2.W + Z* q2.X + W* q2.Y;
            qt.Z =  X* q2.Y - Y* q2.X + Z* q2.W + W* q2.Z;
            qt.W = -X* q2.X - Y* q2.Y - Z* q2.Z + W* q2.W;
            return qt;
        }
        
        // Originally: float QuaternionNormalize(Quaternion &q )
        // "Make sure the quaternion is of unit length"
        public VQuaternion Normalized()
        {
            float radius, iradius;

            radius = (X * X) + (Y * Y) + (Z * Z) + (W * W);

            VQuaternion q2 = new VQuaternion(this);
            if (radius != 0) // > FLT_EPSILON && ((radius < 1.0f - 4*FLT_EPSILON) || (radius > 1.0f + 4*FLT_EPSILON))
            {
                radius = (float)Math.Sqrt(radius);
                iradius = 1.0f / radius;
                q2.W *= iradius;
                q2.Z *= iradius;
                q2.Y *= iradius;
                q2.X *= iradius;
            }

            return q2;
        }
        
        // Originally: void QuaternionSM(float s, const Quaternion &p, const Quaternion &q, Quaternion &qt )
        // "Purpose: qt = ( s * p ) * q"
        public VQuaternion ScaledMultipliedWith(float s, VQuaternion q)
        {
            VQuaternion p1 = this.ScaledBy(s);
            VQuaternion q1 = p1.MultipliedWith(q);
            VQuaternion qt = q1.Normalized();
            return qt;
        }
        
        // Originally: void QuaternionMatrix( const Quaternion &q, matrix3x4_t& matrix )
        public VMatrix ToMatrix()
        {
            VMatrix m = VMatrix.Zero;

            m.M00 = 1.0f - 2.0f * Y * Y - 2.0f * Z * Z;
            m.M10 = 2.0f * X * Y + 2.0f * W * Z;
            m.M20 = 2.0f * X * Z - 2.0f * W * Y;

            m.M01 = 2.0f * X * Y - 2.0f * W * Z;
            m.M11 = 1.0f - 2.0f * X * X - 2.0f * Z * Z;
            m.M21 = 2.0f * Y * Z + 2.0f * W * X;

            m.M02 = 2.0f * X * Z + 2.0f * W * Y;
            m.M12 = 2.0f * Y * Z - 2.0f * W * X;
            m.M22 = 1.0f - 2.0f * X * X - 2.0f * Y * Y;

            m.M03 = 0.0f;
            m.M13 = 0.0f;
            m.M23 = 0.0f;

            return m;
        }
        
        // Originally: void QuaternionAngles( const Quaternion &q, RadianEuler &angles )
        // "Purpose: Converts a quaternion into engine angles
        //  Input  : *quaternion - q3 + q0.i + q1.j + q2.k
        //           *outAngles - PITCH, YAW, ROLL"
        public Vector3 ToRadianAngles()
        {
            // FIXME: doing it this way calculates too much data, needs to do an optimized version...
                // I wonder if they ever came up with an optimized version
            VMatrix m = this.ToMatrix();
            return m.ToRadianAngles();
        }
        
        // Originally: void QuaternionSMAngles(float s, Quaternion const &p, Quaternion const &q, RadianEuler &angles )
        // "overlay
        //  studiomdl : delta = (-1 * base_anim ) * new_anim
        //  engine : result = base_anim * (w * delta)"
        public static Vector3 SMAngles(float s, VQuaternion p, VQuaternion q)
        {
	        VQuaternion qt = p.ScaledMultipliedWith(s, q);
            return qt.ToRadianAngles();
        }

        
        public static bool operator ==(VQuaternion a, VQuaternion b)
        {
            return a.X == b.X
                   && a.Y == b.Y
                   && a.Z == b.Z
                   && a.W == b.W;
        }

        public override bool Equals(object b)
        {
            if (!(b is VQuaternion))
                return false;

            VQuaternion bq = (VQuaternion)b;
            return X == bq.X && Y == bq.Y && Z == bq.Z && W == bq.W;
        }

        public static bool operator !=(VQuaternion a, VQuaternion b)
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
