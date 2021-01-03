using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiberiumFusion.Smd2Pac.ValveMath;

namespace TiberiumFusion.Smd2Pac
{
    public static class Translator
    {
        public static PacCustomAnimation Smd2Pac(SmdData untouchedSmdData,
                                                  List<string> ignoreBones,
                                                  int optimizeLevel,
                                                  Dictionary<string, Tuple<Vector3, Vector3>> boneFixups,
                                                  SmdData subtractionBaseSmd,
                                                  int subtractionBaseFrame,
                                                  out SmdData subtractedSmdData)
        {
            PacCustomAnimation pacAnim = new PacCustomAnimation();

            // Work on a copy of the SmdData so we can do pose subtraction nondestructively
            SmdData smdData = untouchedSmdData.Clone();

            ///// Optimization
            // We can completely omit bones that do not ever animate (more than a given threshold)
            List<string> staticBones = new List<string>();
            if (optimizeLevel >= 1)
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
                                 || Math.Abs(bonePose.Rotation.X - lastBonePose.Rotation.X) > 0.0005
                                 || Math.Abs(bonePose.Rotation.Y - lastBonePose.Rotation.Y) > 0.0005
                                 || Math.Abs(bonePose.Rotation.Z - lastBonePose.Rotation.Z) > 0.0005 )
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
                        staticBones.Add(bone.Name);
                        Print("- Bone \"" + bone + "\" has virtually zero movement and has been optimized out", 1);
                    }
                }
            }


            ///// Find frame for base pose subtraction
            SmdTimelineFrame subtractionBasePoseFrame = null;
            if (subtractionBaseSmd != null)
            {
                // Validate the user's chosen frame number
                foreach (SmdTimelineFrame frame in subtractionBaseSmd.Timeline.ExplicitFrames)
                {
                    if (frame.FrameTime == subtractionBaseFrame)
                    {
                        subtractionBasePoseFrame = frame;
                        break;
                    }
                }
                if (subtractionBasePoseFrame == null)
                    throw new Exception("No frame with \"time " + subtractionBaseFrame + "\" exists within the subtraction base SMD.");
            }
            

            ///// Main animation data
            pacAnim.TimeScale = smdData.Timeline.ExpectedFrameRate; // Overall animation playback rate

            // The first smd frame with have an extremely high pac3 "FrameRate" due to the wacky animation rules of pac3
            // All subsequent frames with have a "FrameRate" derived from the time gap between them and the previous frame
            Print("- Processing animation frames...", 1);
            float lastSmdFrameTime = 0;
            for (int i = 0; i < smdData.Timeline.ExplicitFrames.Count; i++)
            {
                // Finalize the SMD frame data before translating it to the pac3 anim format
                SmdTimelineFrame smdFrame = smdData.Timeline.ExplicitFrames[i];
                smdFrame.BakeFrame();
                
                if (subtractionBasePoseFrame != null)
                {
                    /* Pose subtraction
                     * 
                     *   SMD animations are all relative in parent bone space, but they are relative to 0,0,0 in both translation and rotation.
                     *   This means that every SMD animation is implicitly responsibly for maintaining the shape of the model's skeleton.
                     *   In other words, SMD animation is NOT relative to the model's bind pose, but rather includes the bind pose + the actual animation composited together.
                     *   
                     *   This becomes a problem in-engine, since PAC3 custom animations are relative to either the model's bind pose or the currently playing sequence.
                     *   In other words, this means all PAC3 custom anims are additive animations. So if we feed PAC3 a normal SMD animation, things will look very ugly.
                     *   The solution is to "subtract" the desired animation from the model's bind pose, thus turning it into an additive animation that plays nice with PAC3.
                     *   
                     *   Studiomdl can also do this, via the `subtract` part of the $animation command. The compiled MDL can then be decompiled with Crowbar to get the subtracted SMD.
                     *   In my testing, I have found this always adds a -90 Y rotation to the root bone. I'm not sure why that happens, and whether it's a studiomdl or Crowbar bug.
                     *   Valve has kept the core level of the Source engine's math code very secret, including studiomdl, unfortunately.
                     *   I did find a copy of the 2004 Episode 1 and 2007 SDK, though, which includes all the relevant code for subtracting animations - most crucially, it revealed Valve's inconsistencies in coordinate order that I was missing beforehand.
                     */

                    // Copy the frame and subtract the base pose frame from it for all bones
                    foreach (SmdBonePose subtractedBonePose in smdFrame.ExplicitBonePoses)
                    {
                        SmdBonePose subtractionBaseBonePose = null;
                        if (subtractionBasePoseFrame.ExplicitBonePoseByBoneName.TryGetValue(subtractedBonePose.Bone.Name, out subtractionBaseBonePose))
                        {
                            // Translation subtraction
                            // This is as straightforward as it can possibly be
                            subtractedBonePose.Position -= subtractionBaseBonePose.Position;

                            // Rotation subtraction
                            // This is much more interesting. Basic idea: r = q * p^-1
                            
                            // The process here is identical to how studiomdl does it, but without the singularity/rounding/precision error that causes strange offsets
                            // Turn the SMD's yaw pitch roll into a quat (these are QAngles, NOT RadianAngles!)
                            Vector3 baseYPR = new Vector3(subtractionBaseBonePose.Rotation.Y, subtractionBaseBonePose.Rotation.Z, subtractionBaseBonePose.Rotation.X);
                            Vector3 destYPR = new Vector3(subtractedBonePose.Rotation.Y, subtractedBonePose.Rotation.Z, subtractedBonePose.Rotation.X);
                            VQuaternion baseRot = VQuaternion.FromQAngles(baseYPR);
                            VQuaternion destRot = VQuaternion.FromQAngles(destYPR);
                            Vector3 differenceRangles = VQuaternion.SMAngles(-1, baseRot, destRot); // RadianAngles?
                                // X is pitch, Y is yaw, Z is roll
                            subtractedBonePose.Rotation.X = differenceRangles.Z * 1;
                            subtractedBonePose.Rotation.Y = differenceRangles.X * 1;
                            subtractedBonePose.Rotation.Z = differenceRangles.Y * 1;

                            // In Source:
                            // - Pitch is rot X
                            // - Yaw is rot Y
                            // - Roll is rot Z
                            // But not always! Because it's Valve!
                            // Which is why we have to swizzle differenceRangles back into a QAngle, because SMAngles doesn't produce a proper QAngle!
                        }
                        else
                        {
                            Print("- [WARNING] Frame " + smdFrame.FrameTime + " subtraction: Bone " + subtractedBonePose.Bone.Name + " is not present in the subtraction base pose frame.", 1);
                            // Pose subtraction should really only be be done on sequences that have identical skeletons with every bone posed
                        }
                    }
                }

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
                foreach (SmdBonePose smdBonePose in smdFrame.ExplicitBonePoses)
                {
                    if (ignoreBones.Contains(smdBonePose.Bone.Name))
                        continue;

                    if (optimizeLevel >= 1 && i > 0 && staticBones.Contains(smdBonePose.Bone.Name))
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
                    pacBonePose.RF = MathHelper.ToDegrees(smdBonePose.Rotation.X) * 1;
                    pacBonePose.RR = MathHelper.ToDegrees(smdBonePose.Rotation.Y) * 1;
                    pacBonePose.RU = MathHelper.ToDegrees(smdBonePose.Rotation.Z) * 1;

                    // Fixup translation + rotation
                    // These are optional constant transforms that are added to the bone pose every frame.
                    // Useful for fixing skeleton alignment, especially for `subtracted` SMDs created by studiomdl + Crowbar, which typically add an erraneous -90 Y rotation to the root bone.
                    Tuple<Vector3, Vector3> boneFixup;
                    if (boneFixups.TryGetValue(smdBonePose.Bone.Name, out boneFixup))
                    {
                        // The fixup translations inputted by the user should be in SMD coordinate space
                        pacBonePose.MF += boneFixup.Item1.Y * -1;
                        pacBonePose.MR += boneFixup.Item1.X * 1;
                        pacBonePose.MU += boneFixup.Item1.Z * 1;

                        pacBonePose.RF += boneFixup.Item2.X * 1;
                        pacBonePose.RR += boneFixup.Item2.Y * 1;
                        pacBonePose.RU += boneFixup.Item2.Z * 1;
                    }

                    pacFrame.BoneInfo[smdBonePose.Bone.Name] = pacBonePose;
                }

                pacAnim.FrameData.Add(pacFrame);
                lastSmdFrameTime = smdFrame.FrameTime;
            }
            
            // Return subtracted SMD data for dumping
            if (subtractionBaseSmd != null)
                subtractedSmdData = smdData;
            else
                subtractedSmdData = null;

            return pacAnim;
        }

        private static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            Program.Print(message, indentLevel, bullet);
        }
    }
}
