using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    // Like most gmod addons, pac uses absolutely ass backwards naming and unit conventions
    public class PacFrame
    {
        public float FrameRate = 1.0f; // Rate of playback for this frame relative to a 1 frame per second target. In other words, 1 / frame duration = FrameRate
        public Dictionary<string, PacBonePose> BoneInfo = new Dictionary<string, PacBonePose>(); // Object map of bone transformations in parent bone space
    }
}
