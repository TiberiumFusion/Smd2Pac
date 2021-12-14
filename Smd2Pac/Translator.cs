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
                                                  bool hideWarnings,
                                                  out SmdData subtractedSmdData)
        {
            PacCustomAnimation pacAnim = new PacCustomAnimation();

            // Work on a copy of the SmdData so we can do pose subtraction nondestructively
            SmdData smdData = untouchedSmdData.Clone();


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
                            // Undo the base animation, then apply the destination animation
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
                            if (hideWarnings)
                            {
                                Print("- [WARNING] Frame " + smdFrame.FrameTime + " subtraction: Bone " + subtractedBonePose.Bone.Name + " is not present in the subtraction base pose frame.", 1);
                                // Pose subtraction should really only be be done on sequences that have identical skeletons with every bone posed
                            }
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
                    
                    PacBonePose pacBonePose = new PacBonePose();

                    /* Just like QAngles and RAngles, coordinates are not consistent either because Valve is Valve
                     * 
                     * SMD coordinate system:
                     *   +X: North on the compass.
                     *   +Y: East on the compass.
                     *   +Z: Up.
                     *   
                     *   - With your RIGHT hand, stick out your thumb, index finger, and middle finger in a kind of L shape.
                     *   - All 3 digits should form right angles with each other.
                     *   - Your index finger is +X, your thumb is +Y, and your middle finger is +Z.
                     *   - Twist your hand so your *index finger* is pointing directly forward in front of you and your middle finger is pointing at the sky.
                     *   
                     * Source engine coordinate system:
                     *   +X: East on the compass. Moving forward (W) ingame moves you +X.
                     *   +Y: North on the compass. Moving left (A) ingame moves you +Y.
                     *   +Z: Up.
                     *   
                     *   - With your LEFT hand, stick out your thumb, index finger, and middle finger in a kind of L shape.
                     *   - All 3 digits should form right angles with each other.
                     *   - Your index finger is +X, your thumb is +Y, and your middle finger is +Z.
                     *   - Twist your hand so your *thumb* is pointing at your monitor and your middle finger is pointing at the sky.
                     *   
                     * Conversion from SMD to engine: Swap X and Y, then negate the new X.
                     *   - This is "correct" (for direct bone manipulation as intended), but does not work with pac3.
                     * 
                     * Conversion from SMD to pac3: Swap X and Y, then negate both.
                     *   - This is because for some bizarre reason, pac uses an unorthodox coordinate space that assumes +Y is "backward" instead of "forward".
                     *   - The various pac3 elements that manipulate bone position blindly negate the "MR" value (Y translation), so we have to adhere to that.
                    */

                    // Translation (including coordinate system conversion via swizzled x and y)
                    pacBonePose.MF = smdBonePose.Position.Y * -1;
                    pacBonePose.MR = smdBonePose.Position.X * -1;
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
                        pacBonePose.MF += boneFixup.Item1.Y * -1; // So we can convert them into engine coordinate space
                        pacBonePose.MR += boneFixup.Item1.X * -1;
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

            
            ///// Optimization
            // We can completely omit bones that have an extremely negligible transform (and thus no perceptible visual movement)
            if (optimizeLevel >= 1)
            {
                // Get all bones that made it into the pac data
                HashSet<string> allPacBones = new HashSet<string>();
                foreach (PacFrame frame in pacAnim.FrameData)
                    foreach (string boneName in frame.BoneInfo.Keys)
                        allPacBones.Add(boneName);

                // Find bones which have or are very close to a 0,0,0 0,0,0 transform for the entire animation, and thus will have no visual effect (pac3 animations are additive)
                List<string> identityBones = new List<string>();
                foreach (string pacBoneName in allPacBones)
                {
                    bool nearIdentity = true;
                    foreach (PacFrame frame in pacAnim.FrameData)
                    {
                        PacBonePose pose = null;
                        if (frame.BoneInfo.TryGetValue(pacBoneName, out pose))
                        {
                            if (new Vector3(pose.MF, pose.MR, pose.MU).Length() > 0.0001 || pose.RF > 0.0005 || pose.RR > 0.0005 || pose.RU > 0.0005)
                            {
                                nearIdentity = false;
                                break;
                            }
                        }
                    }

                    if (nearIdentity)
                        identityBones.Add(pacBoneName);
                }

                // Remove those bones from all frames of the animation
                foreach (string pacBoneName in identityBones)
                {
                    foreach (PacFrame frame in pacAnim.FrameData)
                        frame.BoneInfo.Remove(pacBoneName);

                    Print("- Bone \"" + pacBoneName + "\" has virtually zero movement and has been optimized out", 1);
                }
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
