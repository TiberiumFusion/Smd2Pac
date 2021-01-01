using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdBonePose
    {
        public SmdBone Bone;
        public Vector3 Position; // Translation in parent bone space
        public Vector3 Rotation; // Euler rotations in parent bone space

        public SmdBonePose()
        {

        }

        public SmdBonePose(SmdBone bone, Vector3 pos, Vector3 rot)
        {
            Bone = bone;
            Position = pos;
            Rotation = rot;
        }

        public SmdBonePose Clone()
        {
            return new SmdBonePose(Bone, Position, Rotation);
        }

        public override string ToString()
        {
            return Bone.ToString() + "; Pos: " + Position + "; Rot: " + Rotation;
        }
    }
}
