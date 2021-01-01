using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public static class Translator
    {
        public static PacCustomAnimation Smd2Pac(SmdData smdData, List<string> ignoreBones, int optimizeLevel, Dictionary<string, Vector3> boneFixups)
        {
            PacCustomAnimation pacAnim = new PacCustomAnimation();

            // Optimization
            // We can completely omit bones that do not ever animate (more than a given threshold)
            List<string> identityBones = new List<string>();
            if (optimizeLevel >= 1 || true)
            {
                Dictionary<string, SmdBonePose> initialBonePoses = new Dictionary<string, SmdBonePose>();
                HashSet<string> hasSignificantMovement = new HashSet<string>();
                foreach (SmdTimelineFrame frame in smdData.Timeline.ExplicitFrames)
                {
                    foreach (SmdBonePose bonePose in frame.ExplicitBonePoses)
                    {
                        SmdBonePose lastBonePose = null;
                        if (initialBonePoses.TryGetValue(bonePose.Bone.Name, out lastBonePose))
                        {
                            if ( (bonePose.Position - lastBonePose.Position).Length() > 0.0001
                                 || Math.Abs(bonePose.Rotation.X - lastBonePose.Rotation.X) > 0.01
                                 || Math.Abs(bonePose.Rotation.Y - lastBonePose.Rotation.Y) > 0.01
                                 || Math.Abs(bonePose.Rotation.Z - lastBonePose.Rotation.Z) > 0.01 )
                                hasSignificantMovement.Add(bonePose.Bone.Name);
                        }
                        else
                            initialBonePoses[bonePose.Bone.Name] = bonePose;
                    }
                }
                foreach (SmdBone bone in smdData.Timeline.TargetSkeleton.Bones)
                {
                    if (!hasSignificantMovement.Contains(bone.Name))
                    {
                        identityBones.Add(bone.Name);
                        Print("- Bone \"" + bone + "\" has virtually zero movement and has been optimized out", 1);
                    }
                }
            }

            // Overall animation playback rate
            pacAnim.TimeScale = smdData.Timeline.ExpectedFrameRate;

            // The first smd frame with have an extremely high pac3 "FrameRate" due to the wacky animation rules of pac3
            // All subsequent frames with have a "FrameRate" derived from the time gap between them and the previous frame
            Print("- Processing animation frames...", 1);
            float lastSmdFrameTime = 0;
            for (int i = 0; i < smdData.Timeline.ExplicitFrames.Count; i++)
            {
                SmdTimelineFrame smdFrame = smdData.Timeline.ExplicitFrames[i];

                // Frame duration
                PacFrame pacFrame = new PacFrame();
                if (i == 0)
                    pacFrame.FrameRate = 999; // We want to complete this frame as fast as possible since there's no way to change the bind pose in pac3 custom animations
                else
                {
                    /* SMD frame "time" is in fps. So if one frame has time 0 and the next has time 10, that's a gap of 10 *frames*.
                     * The actual time an SMD frame will take is defined by the `fps` option in the $sequence command.
                     * In comparison, pac3 has no concept of fps and instead sets frame interpolation time purely in seconds
                     * Every custom animation frame in pac3 is innately 1 second long and pac3 individually scales the playback rate of each frame to achieve different length frames
                     * To avoid making the ugly pac3 behavior even uglier, we will use pac3's "TimeScale" property to simply transform this 1 second long unit frame time into a 1/30s or 1/60s or 1/whatever unit.
                     */

                    float deltaSmdFrameCount = smdFrame.FrameTime - lastSmdFrameTime; // How many SMD "frames" it should take to interpolate between the two frames
                    pacFrame.FrameRate = 1.0f * deltaSmdFrameCount; // How many units this frame should take (natively seconds)
                }

                // Frame bone poses
                foreach (SmdBonePose smdBonePose in smdFrame.GetBakedFrame().ExplicitBonePoses)
                {
                    if (ignoreBones.Contains(smdBonePose.Bone.Name))
                        continue;

                    if (optimizeLevel >= 1 && identityBones.Contains(smdBonePose.Bone.Name))
                        continue;

                    PacBonePose pacBonePose = new PacBonePose();

                    /* SMD coordinate system:
                     *   +X: right/"north"
                     *   +Y: backward/"west"
                     *   +Z: up
                     * Source engine coordinate system:
                     *   +X: forward/"north"
                     *   +Y: right/"east"
                     *   +Z: up
                     * Conversion from SMD to engine: rotate -90 degree on +Z
                    */

                    // Translation (including coordinate system conversion via swizzled x and y)
                    pacBonePose.MF = smdBonePose.Position.Y * -1;
                    pacBonePose.MR = smdBonePose.Position.X * 1;
                    pacBonePose.MU = smdBonePose.Position.Z * 1;

                    // Rotation
                    float radToDeg(float rad) { return rad * 180f / 3.14159265359f; }
                    pacBonePose.RF = radToDeg(smdBonePose.Rotation.X) * 1;
                    pacBonePose.RR = radToDeg(smdBonePose.Rotation.Y) * 1;
                    pacBonePose.RU = radToDeg(smdBonePose.Rotation.Z) * 1;

                    // Fixup rotations
                    // These are optional constant rotations that are added to the bone every frame.
                    // Useful for fixing skeleton alignment, especially if the root bone is incorrectly angled 90 degrees in a random direction. (studiomdl sometimes does this when `subtract`ing $animations and $sequences)
                    Vector3 boneFixupRotation;
                    if (boneFixups.TryGetValue(smdBonePose.Bone.Name, out boneFixupRotation))
                    {
                        pacBonePose.RF += boneFixupRotation.X;
                        pacBonePose.RR += boneFixupRotation.Y;
                        pacBonePose.RU += boneFixupRotation.Z;
                    }

                    pacFrame.BoneInfo[smdBonePose.Bone.Name] = pacBonePose;
                }

                pacAnim.FrameData.Add(pacFrame);
                lastSmdFrameTime = smdFrame.FrameTime;
            }

            return pacAnim;
        }

        private static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            Program.Print(message, indentLevel, bullet);
        }
    }
}
