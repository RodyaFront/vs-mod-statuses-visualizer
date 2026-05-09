using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

internal sealed class StatusTooltipPanelBackend : GuiElement
{
    public StatusTooltipPanelBackend(ICoreClientAPI capi)
        : base(capi, ElementBounds.Fixed(0, 0, 1, 1))
    {
    }

    public void FillPanelToTexture(int width, int height, ref LoadedTexture? target)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        target?.Dispose();
        target = new LoadedTexture(api);
        using ImageSurface surface = new(Format.Argb32, width, height);
        using Context ctx = genContext(surface);
        double[] fill = GuiStyle.DialogStrongBgColor;
        ctx.SetSourceRGBA(fill[0], fill[1], fill[2], fill[3]);
        ctx.Rectangle(0, 0, width, height);
        ctx.Fill();
        double[] border = GuiStyle.DialogBorderColor;
        ctx.SetSourceRGBA(border[0], border[1], border[2], border[3]);
        ctx.Rectangle(0, 0, width, height);
        ctx.LineWidth = GuiElementHoverText.DefaultBackground.BorderWidth;
        ctx.Stroke();
        generateTexture(surface, ref target);
    }
}
