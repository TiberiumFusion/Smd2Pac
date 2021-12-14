using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    // Like most gmod addons, pac uses absolutely ass backwards naming and unit conventions
    public class PacBonePose
    {
        public float MF = 0f; // +X translation (forward/east)
        public float MR = 0f; // +Y translation (right/north)
        public float MU = 0f; // +Z translation (up)

        // Source's "forward"/"right" terminology is misleading.
        // - At design time, models face +Y while their right side faces +X.
        // - Ingame, models face +X while their right side faces -Y.

        public float RF = 0f; // pitch
        public float RR = 0f; // yaw
        public float RU = 0f; // roll
            // Yes these are out of order because either Source is weird
    }
}