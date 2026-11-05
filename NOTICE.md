# Notice: how PIP was made, and what it uses

## This mod was written by an AI

**All of Phil's Iterative Parts was written by an AI: Claude, made by Anthropic.**
That covers the C# plugin, the config files, the scripts that generate them,
the tests, the in-game text and these documents.

Phil Nutt designed the mod: the mechanics, the numbers, the rules and the look.
He directed the work and play-tests it in game. **Phil is not a programmer and
has not reviewed the code line by line.** No human programmer has reviewed it.
It passes its own automated checks and has been played, but treat it as
experimental: back up your saves before trying it.

Every commit in the repository is marked `Co-Authored-By: Claude`.

The MIT license in [LICENSE](LICENSE) applies to the extent that anything here
can be copyrighted. In some places, including the United States, work produced
mainly by an AI may have limited or no copyright protection. Phil's design and
direction are his own.

## What it uses, and what it doesn't include

PIP contains **no code, models, textures or other files from anyone else.**

- **No other mod's files are included.** PIP needs Harmony, Module Manager,
  B9PartSwitch, Community Tech Tree and CryoTanks, but they are installed
  separately (CKAN does this). PIP calls them while the game runs. It is built
  against KSP's, Unity's and Harmony's libraries, but none of them is shipped.
- **No KSP assets are included.** PIP's parts copy parts already installed in
  your game when it loads: stock parts and, if you have them, parts from other
  mods (its "looks"). The configs only name those parts. They don't contain
  their models or textures.
- **Some numbers mirror other mods' on purpose, so the parts work with them:**
  PIP's xenon, argon and lithium tank types use the same tank masses and
  capacities as Near Future's, and its tanks follow CryoTanks' tank types and
  boil-off rates. These are compatibility values, not copied code.
- **The toolbar icon** was drawn in code for PIP: a simple engine bell and arrow.
- **Fonts:** the window uses fonts already on your computer; none are included.

"Kerbal Space Program" is a trademark of its owners. PIP is an unofficial,
fan-made mod and isn't affiliated with or endorsed by them, or by the authors of
the mods it works with.
