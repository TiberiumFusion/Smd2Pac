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
    }
}
