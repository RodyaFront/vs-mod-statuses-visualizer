using Vintagestory.API.Common;

namespace PlayerStatusStrip;

internal static class PlayerStatusStripModVersion
{
    internal static string Current(ICoreAPI api)
    {
        Mod? mod = api.ModLoader.GetMod("playerstatusstrip");
        return mod?.Info?.Version?.Trim() ?? "";
    }
}
