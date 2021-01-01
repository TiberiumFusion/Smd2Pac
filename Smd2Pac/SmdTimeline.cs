using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class SmdTimeline
    {
        public SmdSkeleton TargetSkeleton { get; private set; }
        public List<SmdTimelineFrame> ExplicitFrames { get; private set; } = new List<SmdTimelineFrame>(); // Only time blocks which are explcitiy defined in the SMD file
        public float ExpectedFrameRate = 30.0f; // In frame per second. This would be the value you'd use with the `fps` option in your $sequence commands
                                                // Gmod (or source in general) default to 30 fps sequences unless otherwise specified, but the user will have to choose the correct framerate using their qc file

        public SmdTimeline(SmdSkeleton targetSkeleton)
        {
            TargetSkeleton = targetSkeleton;
        }

        public void AddFrame(SmdTimelineFrame frame)
        {
            ExplicitFrames.Add(frame);
            frame.Timeline = this;
            frame.FrameIndex = ExplicitFrames.Count - 1;
        }

        public override string ToString()
        {
            if (ExplicitFrames.Count == 0)
                return "No frames";
            else
            {
                return ExplicitFrames.Count + " explicit frames covering " + (ExplicitFrames[ExplicitFrames.Count - 1].FrameTime - ExplicitFrames[0].FrameTime) + " units of time";
            }
        }
    }
}
