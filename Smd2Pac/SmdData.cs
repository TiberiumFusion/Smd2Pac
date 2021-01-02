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
        public SmdSkeleton Skeleton { get; private set; }
        public SmdTimeline Timeline { get; private set; }

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

        // Creates SmdData from an SMD file
        public static SmdData FromFile(string filepath)
        {
            return new SmdData(File.ReadAllLines(filepath));
        }

        public SmdData(string[] rawLines)
        {
            // Associate line numbers with source data
            List<NumberedLine> lines = new List<NumberedLine>();
            for (int i = 0; i < rawLines.Length; i++)
                lines.Add(new NumberedLine(rawLines[i], i + 1));

            // Strip comments and empty lines
            lines.RemoveAll(l => l.Text.TrimStart().StartsWith("//") || string.IsNullOrWhiteSpace(l.Text));

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
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone ID is not a valid number."));

                string boneName = linetextParts[1].Trim('"'); // Trim the " quotes that always present but useless
                if (string.IsNullOrWhiteSpace(boneName))
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone name is empty."));
                if (bones.Where(b => b.Name == boneName).ToList().Count > 0)
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; duplicate bone name."));

                int boneParentID = -2;
                if (!int.TryParse(linetextParts[2], out boneParentID))
                    throw new Exception(ErrInvalid(line.LineNumber, "Bone definition is invalid; bone parent ID is not a valid number."));

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
            Skeleton = new SmdSkeleton(bones, rootBone);


            ///// Process skeleton block to find animations frames
            List<List<NumberedLine>> timeBlocks = new List<List<NumberedLine>>();
            List<NumberedLine> buildTimeBlock = null;
            bool startedFirstBlockBuild = false;
            int lastTimeNumber = int.MinValue;
            for (int i = 1; i < dbSkeleton.Count - 1; i++)
            {
                NumberedLine line = dbSkeleton[i];
                string linetext = line.Text.Trim();
                string[] linetextParts = linetext.Split(' ');
                if (linetext.Length >= 4 && linetext.Substring(0, 4) == "time")
                {
                    if (linetextParts.Length != 2)
                        throw new Exception(ErrInvalid(line.LineNumber, "Invalid \"time\" block header."));

                    int timeNumber = -1;
                    if (!int.TryParse(linetextParts[1], out timeNumber))
                        throw new Exception(ErrInvalid(line.LineNumber, "\"time\" block number is not a valid number."));

                    if (timeNumber <= lastTimeNumber)
                        throw new Exception(ErrInvalid(line.LineNumber, "\"time\" block number is not sequential to the previous time block."));
                    lastTimeNumber = timeNumber;

                    if (!startedFirstBlockBuild)
                    {
                        buildTimeBlock = new List<NumberedLine>();
                        startedFirstBlockBuild = true;
                    }
                    else
                    {
                        //if (buildTimeBlock.Count < 2) // aka just the "time" header and no actual bone pose data
                        //    throw new Exception(ErrInvalid(line.LineNumber, "Empty \"time\" block."));
                            // This might actually be valid SMD. The Valve wiki isn't clear about this case.

                        timeBlocks.Add(buildTimeBlock);
                        buildTimeBlock = new List<NumberedLine>();
                    }
                }
                buildTimeBlock.Add(line);
            }
            timeBlocks.Add(buildTimeBlock);

            // Create timeline for animation
            Timeline = new SmdTimeline(Skeleton);
            // Add all explicit frames
            foreach (List<NumberedLine> timeBlock in timeBlocks)
            {
                SmdTimelineFrame frame = new SmdTimelineFrame();

                NumberedLine header = timeBlock[0];
                string[] headertextParts = header.Text.Trim().Split(' ');
                frame.FrameTime = (float)int.Parse(headertextParts[1]);

                for (int i = 1; i < timeBlock.Count; i++)
                {
                    NumberedLine boneline = timeBlock[i];
                    string bonelinetext = boneline.Text.Trim();
                    string[] bonelinetextParts = bonelinetext.Split(' ');

                    if (bonelinetextParts.Length != 7)
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Bone pose data is an invalid format."));

                    int boneID = -1;
                    if (!int.TryParse(bonelinetextParts[0], out boneID))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone ID is not a valid number."));

                    float bonePosX = 0f;
                    if (!float.TryParse(bonelinetextParts[1], out bonePosX))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone X translation is not a valid number."));
                    float bonePosY = 0f;
                    if (!float.TryParse(bonelinetextParts[2], out bonePosY))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone Y translation is not a valid number."));
                    float bonePosZ = 0f;
                    if (!float.TryParse(bonelinetextParts[3], out bonePosZ))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone Z translation is not a valid number."));

                    float boneRotX = 0f;
                    if (!float.TryParse(bonelinetextParts[4], out boneRotX))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone X rotation is not a valid number."));
                    float boneRotY = 0f;
                    if (!float.TryParse(bonelinetextParts[5], out boneRotY))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone Y rotation is not a valid number."));
                    float boneRotZ = 0f;
                    if (!float.TryParse(bonelinetextParts[6], out boneRotZ))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; bone Z rotation is not a valid number."));
                    
                    SmdBonePose bonePose = new SmdBonePose();
                    
                    SmdBone targetBone = null;
                    if (!Skeleton.BoneByID.TryGetValue(boneID, out targetBone))
                        throw new Exception(ErrInvalid(boneline.LineNumber, "Invalid bone pose; no bone exists with the ID " + boneID + "."));

                    foreach (SmdBonePose existingBonePose in frame.ExplicitBonePoses)
                        if (existingBonePose.Bone == targetBone)
                            throw new Exception(ErrInvalid(boneline.LineNumber, "Duplicate bone pose."));

                    bonePose.Bone = targetBone;
                    bonePose.Position = new Vector3(bonePosX, bonePosY, bonePosZ);
                    bonePose.Rotation = new Vector3(boneRotX, boneRotY, boneRotZ);

                    frame.AddBonePose(bonePose);
                }

                Timeline.AddFrame(frame); // Frames will be stored sequentially as they were defined in the SDM and any pose interpolation will occur on demand if needed
            }

            Print("- " + bones.Count + " bones, " + Timeline.ExplicitFrames.Count + " frames of animation", 1);
        }

        // Writes this SmdData back into ascii lines
        public string[] ToLines(bool bakeAnimationFrames = false)
        {
            List<string> lines = new List<string>();

            // Header
            lines.Add("// Output from Smd2Pac");
            lines.Add("version 1");

            // Nodes
            lines.Add("nodes");
            foreach (SmdBone bone in Skeleton.Bones)
                lines.Add("    " + bone.ID + " \"" + bone.Name + "\" " + bone.ParentID);
            lines.Add("end");

            // Animation
            lines.Add("skeleton");
            for (int i = 0; i < Timeline.ExplicitFrames.Count; i++)
            {
                SmdTimelineFrame frame = Timeline.ExplicitFrames[i];
                if (bakeAnimationFrames)
                    frame = frame.GetBakedFrame();

                lines.Add("    time " + frame.FrameTime);
                
                foreach (SmdBonePose bonePose in frame.ExplicitBonePoses)
                {
                    lines.Add("        " + bonePose.Bone.ID
                              + " " + bonePose.Position.X.ToString("F6")
                              + " " + bonePose.Position.Y.ToString("F6")
                              + " " + bonePose.Position.Z.ToString("F6")
                              + " " + bonePose.Rotation.X.ToString("F6")
                              + " " + bonePose.Rotation.Y.ToString("F6")
                              + " " + bonePose.Rotation.Z.ToString("F6"));
                }
            }
            lines.Add("end");

            return lines.ToArray();
        }

        private static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            Program.Print(message, indentLevel, bullet);
        }
    }
}