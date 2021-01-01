using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdTimelineFrame
    {
        public int FrameIndex = -1;
        public float FrameTime = -1;
        public List<SmdBonePose> ExplicitBonePoses = new List<SmdBonePose>(); // Only bone poses which are explicitly defined in the SMD file for this frame
        public SmdTimeline Timeline = null;

        public SmdTimelineFrame()
        {

        }
        
        // SMD allows frames to omit explicit bone poses for bones that havent actually moved since the last frame.
        // This method returns a "baked" timeline frame of itself that includes effective pose data for ALL bones
        public SmdTimelineFrame GetBakedFrame()
        {
            // Frame 0 should always define a pose for every bone
            if (FrameIndex == 0)
                return this;

            SmdTimelineFrame baked = Clone();

            List<SmdBone> remainingBones = new List<SmdBone>(Timeline.TargetSkeleton.Bones); // Bones which we still have to find pose data for
            foreach (SmdBonePose explicitBonePose in baked.ExplicitBonePoses)
                if (remainingBones.Contains(explicitBonePose.Bone))
                    remainingBones.Remove(explicitBonePose.Bone);
            
            if (remainingBones.Count == 0)
                return this; // We already have pose data for every bone in the skeleton

            // Rewind through the previous frames and grab the bone pose for any remaining bones until we either get them all or run out of timeline to rewind
            int prevFrameIndex = FrameIndex - 1;
            while (remainingBones.Count > 0 && prevFrameIndex > 0)
            {
                SmdTimelineFrame prevFrame = Timeline.ExplicitFrames[prevFrameIndex];
                foreach (SmdBonePose prevBonePose in prevFrame.ExplicitBonePoses)
                {
                    if (remainingBones.Contains(prevBonePose.Bone))
                    {
                        baked.ExplicitBonePoses.Add(prevBonePose.Clone());
                        remainingBones.Remove(prevBonePose.Bone);
                    }
                }
                prevFrameIndex--;
            }
            baked.ExplicitBonePoses = baked.ExplicitBonePoses.OrderBy(p => p.Bone.ID).ToList(); // Restore order by bone ID

            return baked;
        }

        public SmdTimelineFrame Clone()
        {
            SmdTimelineFrame clone = new SmdTimelineFrame();
            clone.FrameIndex = FrameIndex;
            clone.FrameTime = FrameTime;
            clone.Timeline = Timeline;
            foreach (SmdBonePose bonePose in ExplicitBonePoses)
                clone.ExplicitBonePoses.Add(bonePose.Clone());
            return clone;
        }

        public override string ToString()
        {
            return "FrameIndex: " + FrameIndex + ", FrameTime: " + FrameTime + ", " + ExplicitBonePoses.Count + " posed bones";
        }
    }
}
