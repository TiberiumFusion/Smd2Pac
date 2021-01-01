using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdSkeleton
    {
        public List<SmdBone> Bones;
        public SmdBone RootBone;
        public Dictionary<int, SmdBone> BoneByID = new Dictionary<int, SmdBone>();

        public SmdSkeleton(List<SmdBone> bones, SmdBone rootBone)
        {
            Bones = bones;
            RootBone = rootBone;

            foreach (SmdBone bone in bones)
                BoneByID[bone.ID] = bone;
        }

        public override string ToString()
        {
            if (RootBone == null)
                return "Null root bone";
            else
            {
                string build = "";
                foreach (string line in RootBone.ListHierarchy())
                    build += line + "\n";
                return build.TrimEnd('n');
            }
        }
    }
}
