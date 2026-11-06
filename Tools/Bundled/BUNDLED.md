# Mods bundled with PIP

This download ("with dependencies") includes the five mods PIP needs, so it works
without CKAN. They are included **unmodified**, exactly as their authors
distribute them. Each belongs to its authors and keeps its own license; PIP's MIT
license covers only PIP. Because two of them are licensed for non-commercial use,
**this bundle may not be sold.**

| Mod | Version | Authors | License | Source |
|---|---|---|---|---|
| Harmony 2 (HarmonyKSP) | 2.2.1.0 | Andreas Pardeike (Harmony); KSP package by HarmonyKSP (gotmachine) | MIT: `Harmony-LICENSE.txt` | https://github.com/KSPModdingLibs/HarmonyKSP, https://github.com/pardeike/Harmony |
| Module Manager | 4.2.3 | ialdabaoth (original), sarbian, Blowfish | CC BY-SA: `ModuleManager/README.md` | https://github.com/sarbian/ModuleManager |
| B9PartSwitch | 2.21.0.4 | blowfish, linuxgurugamer | LGPL-3.0: `GameData/B9PartSwitch/LICENSE` | https://github.com/KSPModStewards/B9PartSwitch |
| Community Tech Tree | 3.4.5 | Nertea | Creative Commons non-commercial: `CommunityTechTree/readme.txt` | https://github.com/post-kerbin-mining-corporation/CommunityTechTree |
| Community Resource Pack | 112.0.1 | RoverDude | CC BY-NC-SA 4.0: `CommunityResourcePack/License_CC.txt` | https://github.com/UmbraSpaceIndustries/CommunityResourcePack |

The license files named above are in this folder (`GameData/PIP/Bundled/`),
except B9PartSwitch's, which stays in its own folder.

- **Module Manager** here is sarbian's maintained version, as distributed; its
  readme credits ialdabaoth as the original creator.
- **B9PartSwitch**'s source code is at the link above.
- **Community Tech Tree**'s readme names the CC Attribution-NonCommercial license
  and links the ShareAlike version; this bundle meets both (credited, unmodified,
  free).
- **PIP changes the prices** of nine Community Resource Pack resources at load
  time, through a Module Manager patch in PIP's own `Fuels.cfg`. Community
  Resource Pack's files in this bundle are unmodified.

**If you already have any of these mods,** use the PIP-only download instead, or
don't let the unzip replace newer versions. Never keep two copies of Module
Manager or Harmony: delete the older one.
