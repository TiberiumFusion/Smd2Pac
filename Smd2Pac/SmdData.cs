using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdData
    {
        public int Version { get; private set; } = 0;
        
        // Creates an SmdData from an SMD file
        public static SmdData FromFile(string filepath)
        {
            return new SmdData(File.ReadAllLines(filepath));
        }

        private string ErrInvalid(string message)
        {
            return "Invalid SMD data. " + message;
        }
        private string ErrInvalid(int lineNum, string message)
        {
            return "Invalid SMD data at line " + lineNum + ". " + message;
        }
        private string ErrUnknown(string message)
        {
            return "Unknown SMD format . " + message;
        }
        private string ErrUnknown(int lineNum, string message)
        {
            return "Unknown SMD format at line " + lineNum + ". " + message;
        }

        public SmdData(string[] rawLines)
        {
            // Associate line numbers with source data
            List<NumberedLine> lines = new List<NumberedLine>();
            for (int i = 0; i < rawLines.Length; i++)
                lines.Add(new NumberedLine(rawLines[i], i + 1));

            // Strip comments and empty lines
            lines.RemoveAll(l => l.Text.StartsWith("//") || string.IsNullOrWhiteSpace(l.Text));

            if (lines.Count == 0)
                throw new Exception("SMD file has no data.");

            // Header check
            // This is extremely minimal for Valve's SMD format. It's just the "version" tag.
            string[] rawVersionLine = lines[0].Text.Trim().Split(' ');
            if (string.IsNullOrWhiteSpace(rawVersionLine[0]) || rawVersionLine[0] != "version")
                throw new Exception(ErrInvalid(lines[0].LineNumber, "\"version\" tag is missing."));
            if (rawVersionLine.Length != 2)
                throw new Exception(ErrInvalid(lines[0].LineNumber, "\"version\" tag is invalid."));
            if (rawVersionLine[1] != "1")
                throw new Exception(ErrUnknown(lines[0].LineNumber, "\"version\" has a value of " + rawVersionLine[1] + ", expected 1."));

            // Top-level data block structure
            List<NumberedLine> dbNodes = null;
            List<NumberedLine> dbSkeleton = null;
            // We don't need the "triangles" or "vertexanimation" data blocks

            // Extract relevant data blocks
            bool inBlock = false;
            string inBlockName = null;
            int inBlockStart = -1;
            for (int i = 1; i < lines.Count; i++)
            {
                NumberedLine line = lines[i];
                string linetext = line.Text.Trim();
                if (string.IsNullOrWhiteSpace(linetext))
                    continue;

                if (linetext == "nodes" || linetext == "skeleton" || linetext == "vertexanimation")
                {
                    if (inBlock)
                        throw new Exception(ErrInvalid(line.LineNumber, "New data block \"" + linetext + "\" starts in the middle of the previous \"" + inBlockName + "\" data block."));

                    inBlock = true;
                    inBlockName = linetext;
                    inBlockStart = i;
                }

                if (linetext == "end")
                {
                    if (inBlock)
                    {
                        inBlock = false;
                        if (inBlockName == "nodes")
                            dbNodes = lines.GetRange(inBlockStart, i - inBlockStart + 1);
                        else if (inBlockName == "skeleton")
                            dbSkeleton = lines.GetRange(inBlockStart, i - inBlockStart + 1);
                    }
                    else
                        throw new Exception(ErrInvalid(line.LineNumber, "Unexpected \"end\" keyword outside of any data blocks."));
                }
            }

            // Ensure data blocks exist
            if (dbNodes == null)
                throw new Exception(ErrInvalid("\"nodes\" data block is missing."));
            if (dbSkeleton == null)
                throw new Exception(ErrInvalid("\"skeleton\" data block is missing."));
            // And that they arent empty
            if (dbNodes.Count == 2)
                throw new Exception(ErrInvalid(dbNodes[0].LineNumber, "\"nodes\" data block is empty."));
            if (dbSkeleton.Count == 2)
                throw new Exception(ErrInvalid(dbSkeleton[0].LineNumber, "\"skeleton\" data block is empty."));

            ///// Process node block to build skeleton hierarchy
            // Bone definition order does NOT have to be sequential, so we'll set up the parenting AFTER discovering all bones
            // Crowbar always writes bones sequentially, but other software might be lazy and write them out of order
            List<SmdBone> bones = new List<SmdBone>();
            for (int i = 1; i < dbNodes.Count - 1; i++)
            {
                NumberedLine line = dbNodes[i];
                string linetext = line.Text.Trim();

                string[] linetextParts = linetext.Split(' '); // This is ok because source engine bones cannot have spaces in their names
                if (linetextParts.Length != 3)
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is an invalid format."));

                int boneID = -1;
                if (!int.TryParse(linetextParts[0], out boneID))
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone ID is not an integer."));

                string boneName = linetextParts[1].Trim('"'); // Trim the " quotes that always present but useless
                if (string.IsNullOrWhiteSpace(boneName))
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone name is empty."));
                if (bones.Where(b => b.Name == boneName).ToList().Count > 0)
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; duplicate bone name."));

                int boneParentID = -2;
                if (!int.TryParse(linetextParts[2], out boneParentID))
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone parent ID is not an integer."));

                SmdBone bone = new SmdBone(boneID, boneName, boneParentID);
                bone.SmdSourceLine = line;
                bones.Add(bone);
            }
            
            // Find and verify the root bone
            List<SmdBone> rootBones = bones.Where(b => b.ParentID == -1).ToList();
            if (rootBones.Count > 1)
                throw new Exception(ErrInvalid(rootBones[1].SmdSourceLine.LineNumber, "Invalid skeleton hierarchy. Only be one root bone can exist."));
            SmdBone rootBone = rootBones[0];

            // Create hierarchy
            List<SmdBone> orphanBonesLeft = new List<SmdBone>(bones);
            orphanBonesLeft.Remove(rootBone);
            void buildHierarchy(SmdBone self, List<SmdBone> orphanBones)
            {
                foreach (SmdBone orphanBone in orphanBones.ToList()) // Iterate a copy
                {
                    if (orphanBone == self)
                        continue;

                    if (orphanBone.ParentID == self.ID)
                    {
                        self.Children.Add(orphanBone);
                        orphanBone.Parent = self;
                        orphanBones.Remove(orphanBone);

                        buildHierarchy(orphanBone, orphanBones);
                    }
                }
            }
            buildHierarchy(rootBone, orphanBonesLeft);
            if (orphanBonesLeft.Count > 0)
            {
                string message = "\n";
                foreach (SmdBone bone in orphanBonesLeft)
                    message += "Orphaned bone \"" + bone.Name + "\" (ID: " + bone.ID + "). No bone with parent ID " + bone.ParentID + " exists.\n";
                throw new Exception(ErrInvalid(message.TrimEnd('\n')));
            }

            // Create skeleton object
            SmdSkeleton skeleton = new SmdSkeleton();
            skeleton.RootBone = rootBone;

            Console.WriteLine(skeleton);
        }
    }
}