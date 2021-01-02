using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class PacCustomAnimation
    {
        [JsonProperty(Order = 1)]
        public string Type = "sequence"; // Type of animation. Affects engine playback and things like sequence layering.
                                         // One of: gesture, posture, sequence, stance

        [JsonProperty(Order = 2)]
        public string Interpolation = "cosine"; // Type of frame interpolation
                                                // One of: cosine, cubic, linear, none
        
        [JsonProperty(Order = 5)]
        public List<PacFrame> FrameData = new List<PacFrame>(); // List of animation frames

        [JsonProperty(Order = 3)]
        public float TimeScale = 1.0f; // Rate of playback multiplier

        [JsonProperty(Order = 4)]
        public float Power = 1.0f; // Bone pose intensity multiplier


        public PacCustomAnimation()
        {

        }
    }
}
