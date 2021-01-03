# Smd2Pac
CLI application that converts SMD skeleton animations into PAC3 custom animation data for use in Garry's Mod. Also supports animation subtraction, which is pretty much a necessity for making any kind of animation look correct with pac.
* Requires .NET 4.5.2 at minimum
* No Source SDK dependencies
* Does animation subtraction better than studiomdl/Crowbar

### So... what's the point?
Let's say you want to shamble around like the common infected from Left 4 Dead, but your favorite DarkRP server doesn't have any playermodels with the L4D animations. Lucky for you, Smd2Pac is gonna save your day. Grab the SMD animations for the common infected, convert them with Smd2Pac, copy & paste the converted data into your PAC3 outfit, and et voila! You now have zombie animations on any playermodel of your choosing (well, any valve biped), without needing any addons.

## Usage
### Go to the [wiki pages](https://github.com/TiberiumFusion/Smd2Pac/wiki) for complete instructions

Smd2Pac fits into any workflow that ends with an SMD file. You can use it with SMDs decompiled by Crowbar or with SMD exports from software like Maya or Blender.

Smd2Pac only supports the primordial SMD format used by Valve's Source engine games. Third-party derivations of the SMD format are not supported.

### Background
Of all the Source games, Garry's Mod has seen the most evolution as a living game, with an ever-increasing degree to which players can create and instantly show off their custom-made content. Sprays, videos, websites, images, sounds, music, models, effects, and almost everything under the sun can be loaded and displayed immediately, on demand, without requiring everyone on the server to leave and subscribe to a new addon. One key thing is missing from that list, however: animation. Despite the demand for it, Facepunch will not include the power of `$includemodel` in GLua, and so the ability to create dynamic animation content is greatly crippled.

Without proper access to the engine's animation system, the next best option for dynamic animation content is the ubiquitous PAC3 addon, which includes a functional animation system. Unfortunately, the interface for creating and editing animations is **very** basic and limiting, and is incapable of creating smooth & complex motion. If only there was a way to create high-quality pac animations with your favorite 3D modeling & animation software...

Smd2Pac bridges the gap by translating SMD animations into PAC3 animation data, allowing you to quite literally copy and paste your animations into gmod. You can animate your models in Maya, Blender, 3dsmax, etc. with the help of familiar animation tools, then export it to SMD, and finally bring your animation into gmod with Smd2Pac.
