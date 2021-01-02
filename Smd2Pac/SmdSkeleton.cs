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
        
        public SmdSkeleton()
        {

        }

        public SmdSkeleton(List<SmdBone> bones, SmdBone rootBone)
        {
            Bones = bones;
            RootBone = rootBone;

            foreach (SmdBone bone in bones)
                BoneByID[bone.ID] = bone;
        }
        
        public SmdSkeleton Clone()
        {
            SmdSkeleton clone = new SmdSkeleton();

            clone.Bones = new List<SmdBone>();

            // Clone all bones
            Dictionary<SmdBone, SmdBone> cloneToOrig = new Dictionary<SmdBone, SmdBone>();
            foreach (SmdBone bone in Bones)
            {
                SmdBone boneClone = bone.Clone();
                cloneToOrig[boneClone] = bone;
                clone.Bones.Add(boneClone);
            }
            
            // Re-establish references
            foreach (SmdBone cloneBone in clone.Bones)
            {
                SmdBone origBone = cloneToOrig[cloneBone];

                // Parent
                if (origBone.Parent != null)
                {
                    foreach (SmdBone cloneBone2 in clone.Bones)
                        if (cloneBone2.ID == origBone.Parent.ID)
                            cloneBone.Parent = cloneBone2;
                }

                // Children
                foreach (SmdBone origBoneChild in origBone.Children)
                {
                    foreach (SmdBone cloneBone2 in clone.Bones)
                        if (cloneBone2.ID == origBoneChild.ID)
                            cloneBone.Children.Add(cloneBone2);
                }
            }

            // Find root bone
            clone.RootBone = clone.Bones.Where(b => b.ParentID == -1).FirstOrDefault();

            // BoneByID
            foreach (SmdBone bone in clone.Bones)
                clone.BoneByID[bone.ID] = bone;

            return clone;
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
