using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdBone
    {
        public int ID;
        public string Name;
        public int ParentID;
        public SmdBone Parent = null;
        public List<SmdBone> Children = new List<SmdBone>();

        public NumberedLine SmdSourceLine = null;
        
        public SmdBone()
        {

        }

        public SmdBone(int id, string name, int parentID)
        {
            ID = id;
            Name = name;
            Parent = null;
            ParentID = parentID;
        }

        public SmdBone Clone()
        {
            SmdBone clone = new SmdBone();
            clone.ID = this.ID;
            clone.Name = this.Name;
            clone.ParentID = this.ParentID;
            return clone;
        }

        public override string ToString()
        {
            return "[" + ID + "] " + Name;
        }
        public List<string> ListHierarchy()
        {
            List<string> build = new List<string>();
            build.Add(this.ToString());
            foreach (SmdBone child in Children)
            {
                List<string> childBuild = child.ListHierarchy();
                for (int i = 0; i < childBuild.Count; i++)
                {
                    if (childBuild[i][0] == ' ')
                        childBuild[i] = "    " + childBuild[i];
                    else
                        childBuild[i] = "  └─" + childBuild[i];
                }
                build.AddRange(childBuild);
            }
            return build;
        }
    }
}
