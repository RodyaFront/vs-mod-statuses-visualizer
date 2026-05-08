using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

public sealed class StripLayoutWizardDialog : GuiDialog
{
    public bool SuppressOnboardingWhenClosed { get; set; } = true;

    public event Action? LayoutWizardClosed;

    private static readonly int[] IconPresetPx = { 32, 42, 64, 78 };

    private static readonly int[] GapPresetPx = { 2, 4, 8, 12, 16 };

    private readonly StatusStripHudElement _hud;
    private readonly IStatusStripHudApi _hudApi;
    private readonly StripLayoutWizardPreviewProvider _previewProvider = new();
    private readonly string[] _areaCodes;
    private readonly string[] _areaDisplay;
    private readonly string[] _insetCodes;
    private readonly string[] _insetDisplay;
    private readonly string[] _iconCodes;
    private readonly string[] _iconDisplay;
    private readonly string[] _gapCodes;
    private readonly string[] _gapDisplay;
    private int _areaIndex;
    private int _insetPresetIndex;
    private int _iconPresetIndex;
    private int _gapPresetIndex;
    private bool _layoutAppliedFromWizard;

    public StripLayoutWizardDialog(ICoreClientAPI capi, StatusStripHudElement hud, IStatusStripHudApi hudApi)
        : base(capi)
    {
        _hud = hud;
        _hudApi = hudApi;
        _hudApi.RegisterProvider(_previewProvider);
        _hudApi.SetPreviewExclusiveProvider(_previewProvider);
        StatusStripLayoutConfig baseline = StatusStripLayoutConfig.Reload(capi);
        baseline.EnsureDefaults();
        _areaCodes = new[]
        {
            "RightTop", "RightBottom", "LeftTop", "LeftBottom", "CenterTop",
            "RightMiddle", "LeftMiddle"
        };
        _areaDisplay = new string[_areaCodes.Length];
        for (int i = 0; i < _areaCodes.Length; i++)
        {
            string langKey = "playerstatusstrip:wizard-area-" + _areaCodes[i];
            string label = Lang.Get(langKey);
            _areaDisplay[i] = string.Equals(label, langKey, StringComparison.Ordinal) ? _areaCodes[i] : label;
        }

        _insetCodes = new[] { "tight", "standard", "relaxed", "generous" };
        _insetDisplay = new string[_insetCodes.Length];
        for (int i = 0; i < _insetCodes.Length; i++)
        {
            string langKey = "playerstatusstrip:wizard-preset-inset-" + _insetCodes[i];
            string label = Lang.Get(langKey);
            _insetDisplay[i] = string.Equals(label, langKey, StringComparison.Ordinal) ? _insetCodes[i] : label;
        }

        _iconCodes = new[] { "32", "42", "64", "78" };
        _iconDisplay = new string[_iconCodes.Length];
        for (int i = 0; i < _iconCodes.Length; i++)
        {
            string langKey = "playerstatusstrip:wizard-preset-size-" + _iconCodes[i];
            string label = Lang.Get(langKey);
            _iconDisplay[i] = string.Equals(label, langKey, StringComparison.Ordinal)
                ? _iconCodes[i] + " px"
                : label;
        }

        _gapCodes = new[] { "2", "4", "8", "12", "16" };
        _gapDisplay = new string[_gapCodes.Length];
        for (int i = 0; i < _gapCodes.Length; i++)
        {
            string langKey = "playerstatusstrip:wizard-preset-gap-" + _gapCodes[i];
            string label = Lang.Get(langKey);
            _gapDisplay[i] = string.Equals(label, langKey, StringComparison.Ordinal) ? _gapCodes[i] + " px" : label;
        }

        _areaIndex = IndexOfArea(baseline.DialogArea);
        _insetPresetIndex = StripLayoutInsetPresets.NearestStepIndex(
            baseline.DialogArea,
            baseline.DialogOffsetX,
            baseline.DialogOffsetY);
        _iconPresetIndex = NearestIntPresetIndex(
            baseline.StatusIconSize <= 0 ? 42 : baseline.StatusIconSize,
            IconPresetPx);
        _gapPresetIndex = NearestIntPresetIndex(Math.Max(0, baseline.StatusIconGapPx), GapPresetPx);

        SetupDialog();
        PushLayoutPreview();
    }

    private int IndexOfArea(string? area)
    {
        if (string.IsNullOrWhiteSpace(area))
        {
            return 0;
        }

        string code = area.Trim();
        if (string.Equals(code, "CenterBottom", StringComparison.OrdinalIgnoreCase))
        {
            code = "CenterTop";
        }

        int idx = Array.IndexOf(_areaCodes, code);
        return idx >= 0 ? idx : 0;
    }

    private static int NearestIntPresetIndex(int value, int[] presets)
    {
        int best = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < presets.Length; i++)
        {
            int d = Math.Abs(value - presets[i]);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }

    public override string? ToggleKeyCombinationCode => null;

    private void SetupDialog()
    {
        const double dialogW = 390;
        const double secTitleH = 22;
        const double introH = 118;
        const double beforeLayoutPad = 16;
        const double footerH = 96;
        const double btnRowH = 34;
        const double bottomPad = 20;
        double rowH = 28;
        double g = 6;
        double secGap = 10;
        double t = GuiStyle.TitleBarHeight;
        double pad = GuiStyle.ElementToDialogPadding;
        double textWidth = dialogW - 2 * pad;
        double labelW = 128;
        double ddW = textWidth - labelW - 8;
        double dialogH = t + 8 + introH + secGap + beforeLayoutPad + secTitleH + 6 + rowH + g + rowH + secGap
            + secTitleH + 6 + rowH + g + rowH + 12 + footerH + 10 + btnRowH + bottomPad;

        ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, dialogW, dialogH);
        ElementBounds bgBounds = ElementBounds.Fill;
        CairoFont labelFont = CairoFont.WhiteSmallText();
        CairoFont sectionFont = CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold);
        CairoFont introFooterFont = CairoFont.WhiteSmallText();
        CairoFont footerHintFont = CairoFont.WhiteSmallText()
            .WithWeight(FontWeight.Bold)
            .WithColor(new double[] { 1.0, 0.82, 0.38, 1.0 });
        double nx = pad;
        double ny = t + 8;

        ElementBounds intro = ElementBounds.Fixed(nx, ny, textWidth, introH);
        ny += introH + secGap + beforeLayoutPad;

        ElementBounds secLayout = ElementBounds.Fixed(nx, ny, textWidth, secTitleH);
        ny += secTitleH + 6;
        ElementBounds lblCorner = ElementBounds.Fixed(nx, ny, labelW, rowH);
        ElementBounds ddCorner = ElementBounds.Fixed(nx + labelW + 8, ny, ddW, rowH);
        ny += rowH + g;
        ElementBounds lblInset = ElementBounds.Fixed(nx, ny, labelW, rowH);
        ElementBounds ddInset = ElementBounds.Fixed(nx + labelW + 8, ny, ddW, rowH);
        ny += rowH + secGap;

        ElementBounds secAppearance = ElementBounds.Fixed(nx, ny, textWidth, secTitleH);
        ny += secTitleH + 6;
        ElementBounds lblIcon = ElementBounds.Fixed(nx, ny, labelW, rowH);
        ElementBounds ddIcon = ElementBounds.Fixed(nx + labelW + 8, ny, ddW, rowH);
        ny += rowH + g;
        ElementBounds lblGap = ElementBounds.Fixed(nx, ny, labelW, rowH);
        ElementBounds ddGap = ElementBounds.Fixed(nx + labelW + 8, ny, ddW, rowH);
        ny += rowH + 12;

        ElementBounds footer = ElementBounds.Fixed(nx, ny, textWidth, footerH);
        ny += footerH + 10;
        double btnGap = 8;
        double btnW = (textWidth - btnGap) / 2;
        ElementBounds btnApply = ElementBounds.Fixed(nx, ny, btnW, btnRowH);
        ElementBounds btnSkip = ElementBounds.Fixed(nx + btnW + btnGap, ny, btnW, btnRowH);

        SingleComposer = capi.Gui
            .CreateCompo("playerstatusstrip-layout-wizard", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get("playerstatusstrip:wizard-title"), OnTitleClose)
            .AddStaticTextAutoBoxSize(Lang.Get("playerstatusstrip:wizard-intro"), introFooterFont, EnumTextOrientation.Left, intro)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-section-layout"), sectionFont, secLayout)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-corner"), labelFont, lblCorner)
            .AddDropDown(_areaCodes, _areaDisplay, _areaIndex, OnAreaCodeSelected, ddCorner, CairoFont.WhiteSmallText(), "areaDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-inset"), labelFont, lblInset)
            .AddDropDown(_insetCodes, _insetDisplay, _insetPresetIndex, OnInsetPresetSelected, ddInset, CairoFont.WhiteSmallText(), "insetDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-section-appearance"), sectionFont, secAppearance)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-icon-preset"), labelFont, lblIcon)
            .AddDropDown(_iconCodes, _iconDisplay, _iconPresetIndex, OnIconPresetSelected, ddIcon, CairoFont.WhiteSmallText(), "iconDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-gap-preset"), labelFont, lblGap)
            .AddDropDown(_gapCodes, _gapDisplay, _gapPresetIndex, OnGapPresetSelected, ddGap, CairoFont.WhiteSmallText(), "gapDd")
            .AddStaticTextAutoBoxSize(Lang.Get("playerstatusstrip:wizard-footer-hint"), footerHintFont, EnumTextOrientation.Left, footer)
            .AddButton(
                Lang.Get("playerstatusstrip:wizard-apply"),
                OnApplyClicked,
                btnApply,
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                EnumButtonStyle.Normal,
                "btnApply")
            .AddButton(
                Lang.Get("playerstatusstrip:wizard-not-now"),
                OnSkipClicked,
                btnSkip,
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                EnumButtonStyle.Small,
                "btnSkip")
            .Compose();
    }

    private void OnAreaCodeSelected(string code, bool selected)
    {
        if (!selected)
        {
            return;
        }

        int idx = Array.IndexOf(_areaCodes, code);
        if (idx >= 0)
        {
            _areaIndex = idx;
        }

        PushLayoutPreview();
    }

    private void OnInsetPresetSelected(string code, bool selected)
    {
        if (!selected)
        {
            return;
        }

        int idx = Array.IndexOf(_insetCodes, code);
        if (idx >= 0)
        {
            _insetPresetIndex = idx;
        }

        PushLayoutPreview();
    }

    private void OnIconPresetSelected(string code, bool selected)
    {
        if (!selected)
        {
            return;
        }

        int idx = Array.IndexOf(_iconCodes, code);
        if (idx >= 0)
        {
            _iconPresetIndex = idx;
        }

        PushLayoutPreview();
    }

    private void OnGapPresetSelected(string code, bool selected)
    {
        if (!selected)
        {
            return;
        }

        int idx = Array.IndexOf(_gapCodes, code);
        if (idx >= 0)
        {
            _gapPresetIndex = idx;
        }

        PushLayoutPreview();
    }

    private void PushLayoutPreview()
    {
        if (SingleComposer == null)
        {
            return;
        }

        if (!TryBuildConfig(out StatusStripLayoutConfig cfg, out _))
        {
            return;
        }

        _hud.ApplyLayoutPreview(cfg);
    }

    private void OnTitleClose()
    {
        TryClose();
    }

    private bool OnSkipClicked()
    {
        TryClose();
        return true;
    }

    private bool OnApplyClicked()
    {
        if (!TryBuildConfig(out StatusStripLayoutConfig cfg, out string error))
        {
            capi.Logger.Notification("[Player Status HUD] Layout wizard: " + error);
            return true;
        }

        cfg.EnsureDefaults();
        capi.StoreModConfig(cfg, StatusStripLayoutConfig.LayoutConfigFileName);
        _layoutAppliedFromWizard = true;
        _hud.ReloadLayoutFromDisk(showLayoutSummaryChat: false);
        capi.Logger.Notification(Lang.Get("playerstatusstrip:wizard-applied"));
        TryClose();
        return true;
    }

    private bool TryBuildConfig(out StatusStripLayoutConfig cfg, out string error)
    {
        cfg = StatusStripLayoutConfig.Reload(capi);
        cfg.EnsureDefaults();
        error = "";
        try
        {
            cfg.DialogArea = _areaCodes[Math.Clamp(_areaIndex, 0, _areaCodes.Length - 1)];
            (double ix, double iy) = StripLayoutInsetPresets.OffsetsForArea(cfg.DialogArea, _insetPresetIndex);
            cfg.DialogOffsetX = ix;
            cfg.DialogOffsetY = iy;
            cfg.StatusIconSize = IconPresetPx[Math.Clamp(_iconPresetIndex, 0, IconPresetPx.Length - 1)];
            cfg.StatusIconGapPx = GapPresetPx[Math.Clamp(_gapPresetIndex, 0, GapPresetPx.Length - 1)];
            cfg.StatusStripSide = StripLayoutWizardStripSide.ForDialogArea(cfg.DialogArea);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public override bool TryClose()
    {
        if (IsOpened() && SuppressOnboardingWhenClosed)
        {
            StatusStripOnboardingConfig ob = StatusStripOnboardingConfig.LoadOrCreate(capi);
            ob.SuppressAutoLayoutWizard = true;
            ob.WizardDismissedForModVersion = PlayerStatusStripModVersion.Current(capi);
            StatusStripOnboardingConfig.Save(capi, ob);
        }

        bool ok = base.TryClose();
        if (ok)
        {
            _hudApi.SetPreviewExclusiveProvider(null);
            _hudApi.UnregisterProvider(_previewProvider);
            if (!_layoutAppliedFromWizard)
            {
                _hud.ReloadLayoutFromDisk(showLayoutSummaryChat: false);
            }

            LayoutWizardClosed?.Invoke();
        }

        return ok;
    }
}
