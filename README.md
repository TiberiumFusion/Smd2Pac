# Smd2Pac
CLI application that converts SMD skeleton animations into PAC3 custom animation data for use in Garry's Mod.
* Requires .NET 4.5.2 runtime
* No Source SDK dependencies
* Does animation subtraction better than studiomdl/Crowbar

### Example
Let's say you want to shamble around like the common infected from Left 4 Dead, but your favorite DarkRP server doesn't have any playermodels with the L4D animations. Get ahold of the SMD animations for the common infected, convert them with Smd2Pac, then copy & paste the converted data into your PAC3 outfit. You now have zombie animations on any compatible playermodel of your choosing, without needing any extra addons.

## Usage
### Go to the [wiki pages](https://github.com/TiberiumFusion/Smd2Pac/wiki) for complete instructions

Smd2Pac fits into any workflow that ends with an SMD file. You can use it with SMDs decompiled by Crowbar or with SMD exports from software like Maya or Blender.

Smd2Pac only supports the primordial SMD format used by Valve's Source engine games. Third-party derivations of the SMD format are not supported.

## Background
With the right addons, Garry's Mod can load and display many forms of dynamic content ingame, without requiring a round trip to the workshop for both the server and every client. Skeleton animation within the engine is one of the big ticket items missing from that list. Unfortunately, Facepunch is adamant in not extending the power of `$includemodel` to addons, which leaves us with very limited options for loading and playing animations on the fly.

Enter the ubiquitous PAC3 addon, which "solves" this issue in part by providing a bone-manipulation-based animation system and tools for creating and managing animations. It is the prime candidate for ensuring on-demand animations will be seen by everyone else on the server. Unfortunately, the interface is extremely basic, and creating quality animations in the pac editor is nigh impossible.

Smd2Pac bridges the gap by translating SMD animations into pac animation data that can be directly pasted into a pac outfit. You can animate your models in Maya/Blender/3dsmax/whatever with the help of familiar animation tools, export to SMD, then finally bring your animation into gmod with Smd2Pac.
