using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class PacCustomAnimation
    {
        public string Type = "sequence"; // Type of animation. Affects engine playback and things like sequence layering.
                                         // One of: gesture, posture, sequence, stance
        public string Interpolation = "cosine"; // Type of frame interpolation
                                                // One of: cosine, cubic, linear, none
        public List<PacFrame> FrameData = new List<PacFrame>(); // List of animation frames
        public float TimeScale = 1.0f; // Rate of playback multiplier
        public float Power = 1.0f; // Bone pose intensity multiplier


        public PacCustomAnimation()
        {

        }

        // Translate an SMD sequence to a PAC3 custom animation
        public static PacCustomAnimation FromSmdData(SmdData smdData)
        {
            PacCustomAnimation pacAnim = new PacCustomAnimation();

            ///// Create a straight translation of each smd frame
            // The first smd frame with have an extremely high pac3 "FrameRate" due to the wacky animation rules of pac3
            // All subsequent frames with have a "FrameRate" derived from the time gap between them and the previous frame
            float lastSmdFrameTime = 0;
            for (int i = 0; i < smdData.Timeline.ExplicitFrames.Count; i++)
            {
                SmdTimelineFrame smdFrame = smdData.Timeline.ExplicitFrames[i];

                // Frame duration
                PacFrame pacFrame = new PacFrame();
                if (i == 0)
                    pacFrame.FrameRate = 1; // We want to complete this frame out as fast as possible since there's no bind poses in pac3 custom animations
                else
                {
                    // SMD frame "time" is in fps. So if one frame has time 0 and the next has time 10, that's a gap of 10 *frames*
                    // The actual time an SMD frame will take is defined by the `fps` option in the $sequence command
                    // So if fps=30, a 10 frame gap is 10*33.33ms
                    // Since pac has no concept of fps and instead times frames based on seconds, we need to do some converting here

                    float deltaSmdFrameCount = smdFrame.FrameTime - lastSmdFrameTime; // How many SMD "frames" it should take to interpolate between the two frames
                    float smdFrameUnitDuration = 1.0f / smdData.Timeline.ExpectedFrameRate; // How long an arbitrary single SMD frame should take in real time (in seconds)
                    float deltaFrameDuration = smdFrameUnitDuration * deltaSmdFrameCount; // How long it should take in real time (in seconds) to interpolate between the two frmes
                    float pacPlaybackRate = 1.0f / deltaFrameDuration; // Pac3-specific playback rate modifier to make the pac3 frame take deltaFrameDuration second long to finish

                    pacFrame.FrameRate = pacPlaybackRate;
                }

                // Frame bone poses
                foreach (SmdBonePose smdBonePose in smdFrame.GetBakedFrame().ExplicitBonePoses)
                {
                    PacBonePose pacBonePose = new PacBonePose();

                    /* SMD coordinate system:
                     *   +X: right/"north"
                     *   +Y: backward/"west"
                     *   +Z: up
                     * Source runtime coordinate system:
                     *   +X: forward/"north"
                     *   +Y: right/"east"
                     *   +Z: up
                     * The difference is 90 degrees rotation on +Z
                    */

                    pacBonePose.MF = smdBonePose.Position.X * 1;
                    pacBonePose.MR = smdBonePose.Position.Y * 1;
                    pacBonePose.MU = smdBonePose.Position.Z * 1;

                    pacBonePose.RF = smdBonePose.Rotation.X * 1;
                    pacBonePose.RR = smdBonePose.Rotation.Y * 1;
                    pacBonePose.RU = smdBonePose.Rotation.Z * 1;

                    pacFrame.BoneInfo[smdBonePose.Bone.Name] = pacBonePose;
                }

                pacAnim.FrameData.Add(pacFrame);
                lastSmdFrameTime = smdFrame.FrameTime;
            }

            return pacAnim;
        }
    }
}
