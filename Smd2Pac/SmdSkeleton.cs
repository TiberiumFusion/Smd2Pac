using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdSkeleton
    {
        public SmdBone RootBone;
        
        public SmdSkeleton()
        {

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
