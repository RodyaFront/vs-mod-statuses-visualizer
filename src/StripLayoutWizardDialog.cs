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

    private struct LayoutFlow
    {
        public LayoutFlow(double x, double y, double width)
        {
            X = x;
            Y = y;
            Width = width;
        }

        public readonly double X;
        public double Y;
        public readonly double Width;

        public ElementBounds Take(double height)
        {
            ElementBounds bounds = ElementBounds.Fixed(X, Y, Width, height);
            Y += height;
            return bounds;
        }

        public void Gap(double gap)
        {
            Y += gap;
        }
    }

    private readonly struct HeaderSectionLayout
    {
        public HeaderSectionLayout(ElementBounds intro)
        {
            Intro = intro;
        }

        public ElementBounds Intro { get; }
    }

    private readonly struct FormSectionLayout
    {
        public FormSectionLayout(
            ElementBounds secLayout,
            ElementBounds lblCorner,
            ElementBounds ddCorner,
            ElementBounds lblInset,
            ElementBounds ddInset,
            ElementBounds secAppearance,
            ElementBounds lblIcon,
            ElementBounds ddIcon,
            ElementBounds lblGap,
            ElementBounds ddGap)
        {
            SectionLayout = secLayout;
            LabelCorner = lblCorner;
            DropDownCorner = ddCorner;
            LabelInset = lblInset;
            DropDownInset = ddInset;
            SectionAppearance = secAppearance;
            LabelIcon = lblIcon;
            DropDownIcon = ddIcon;
            LabelGap = lblGap;
            DropDownGap = ddGap;
        }

        public ElementBounds SectionLayout { get; }
        public ElementBounds LabelCorner { get; }
        public ElementBounds DropDownCorner { get; }
        public ElementBounds LabelInset { get; }
        public ElementBounds DropDownInset { get; }
        public ElementBounds SectionAppearance { get; }
        public ElementBounds LabelIcon { get; }
        public ElementBounds DropDownIcon { get; }
        public ElementBounds LabelGap { get; }
        public ElementBounds DropDownGap { get; }
    }

    private readonly struct TipSectionLayout
    {
        public TipSectionLayout(ElementBounds footer)
        {
            Footer = footer;
        }

        public ElementBounds Footer { get; }
    }

    private readonly struct ActionsSectionLayout
    {
        public ActionsSectionLayout(ElementBounds apply, ElementBounds skip)
        {
            Apply = apply;
            Skip = skip;
        }

        public ElementBounds Apply { get; }
        public ElementBounds Skip { get; }
    }

    private static double EstimateAutoTextHeight(string text, double minHeight)
    {
        int lines = Math.Max(1, text.Replace("\r\n", "\n").Split('\n').Length);
        return Math.Max(minHeight, lines * 18 + 18);
    }

    private static HeaderSectionLayout LayoutHeaderSection(ref LayoutFlow flow, double introHeight, double bottomGap)
    {
        ElementBounds intro = flow.Take(introHeight);
        flow.Gap(bottomGap);
        return new HeaderSectionLayout(intro);
    }

    private static FormSectionLayout LayoutFormSection(
        ref LayoutFlow flow,
        double sectionTitleHeight,
        double rowHeight,
        double rowGap,
        double sectionGap,
        double labelWidth,
        double dropDownGap)
    {
        double ddWidth = flow.Width - labelWidth - dropDownGap;
        ElementBounds secLayout = flow.Take(sectionTitleHeight);
        flow.Gap(6);

        ElementBounds lblCorner = ElementBounds.Fixed(flow.X, flow.Y, labelWidth, rowHeight);
        ElementBounds ddCorner = ElementBounds.Fixed(flow.X + labelWidth + dropDownGap, flow.Y, ddWidth, rowHeight);
        flow.Gap(rowHeight + rowGap);

        ElementBounds lblInset = ElementBounds.Fixed(flow.X, flow.Y, labelWidth, rowHeight);
        ElementBounds ddInset = ElementBounds.Fixed(flow.X + labelWidth + dropDownGap, flow.Y, ddWidth, rowHeight);
        flow.Gap(rowHeight + sectionGap);

        ElementBounds secAppearance = flow.Take(sectionTitleHeight);
        flow.Gap(6);

        ElementBounds lblIcon = ElementBounds.Fixed(flow.X, flow.Y, labelWidth, rowHeight);
        ElementBounds ddIcon = ElementBounds.Fixed(flow.X + labelWidth + dropDownGap, flow.Y, ddWidth, rowHeight);
        flow.Gap(rowHeight + rowGap);

        ElementBounds lblGap = ElementBounds.Fixed(flow.X, flow.Y, labelWidth, rowHeight);
        ElementBounds ddGap = ElementBounds.Fixed(flow.X + labelWidth + dropDownGap, flow.Y, ddWidth, rowHeight);
        flow.Gap(rowHeight);

        return new FormSectionLayout(
            secLayout,
            lblCorner,
            ddCorner,
            lblInset,
            ddInset,
            secAppearance,
            lblIcon,
            ddIcon,
            lblGap,
            ddGap);
    }

    private static TipSectionLayout LayoutTipSection(ref LayoutFlow flow, double tipTopPad, double footerHeight)
    {
        flow.Gap(tipTopPad);
        ElementBounds footer = flow.Take(footerHeight);
        return new TipSectionLayout(footer);
    }

    private static ActionsSectionLayout LayoutActionsSection(
        ref LayoutFlow flow,
        double topGap,
        double rowHeight,
        double buttonGap)
    {
        flow.Gap(topGap);
        double buttonWidth = (flow.Width - buttonGap) / 2;
        ElementBounds apply = ElementBounds.Fixed(flow.X, flow.Y, buttonWidth, rowHeight);
        ElementBounds skip = ElementBounds.Fixed(flow.X + buttonWidth + buttonGap, flow.Y, buttonWidth, rowHeight);
        flow.Gap(rowHeight);
        return new ActionsSectionLayout(apply, skip);
    }

    private void SetupDialog()
    {
        const double dialogW = 390;
        const double secTitleH = 22;
        const double btnRowH = 34;
        const double bottomPad = 20;
        const double rowH = 28;
        const double rowGap = 6;
        const double secGap = 10;
        const double headerBottomGap = 16;
        const double tipTopPad = 36;
        const double actionsTopGap = 10;
        const double buttonGap = 8;
        const double dropDownGap = 8;
        const double labelW = 128;
        double t = GuiStyle.TitleBarHeight;
        double pad = GuiStyle.ElementToDialogPadding;
        double textWidth = dialogW - 2 * pad;
        string introText = Lang.Get("playerstatusstrip:wizard-intro");
        string footerText = Lang.Get("playerstatusstrip:wizard-footer-hint");
        double introH = EstimateAutoTextHeight(introText, 96);
        double footerH = EstimateAutoTextHeight(footerText, 72);
        LayoutFlow flow = new(pad, t + 8, textWidth);
        HeaderSectionLayout header = LayoutHeaderSection(ref flow, introH, headerBottomGap);
        FormSectionLayout form = LayoutFormSection(ref flow, secTitleH, rowH, rowGap, secGap, labelW, dropDownGap);
        TipSectionLayout tip = LayoutTipSection(ref flow, tipTopPad, footerH);
        ActionsSectionLayout actions = LayoutActionsSection(ref flow, actionsTopGap, btnRowH, buttonGap);
        double dialogH = flow.Y + bottomPad;

        ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, dialogW, dialogH);
        ElementBounds bgBounds = ElementBounds.Fill;
        CairoFont labelFont = CairoFont.WhiteSmallText();
        CairoFont sectionFont = CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold);
        CairoFont introFooterFont = CairoFont.WhiteSmallText();
        CairoFont footerHintFont = CairoFont.WhiteSmallText()
            .WithWeight(FontWeight.Bold)
            .WithColor(new double[] { 1.0, 0.82, 0.38, 1.0 });

        SingleComposer = capi.Gui
            .CreateCompo("playerstatusstrip-layout-wizard", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get("playerstatusstrip:wizard-title"), OnTitleClose)
            .AddStaticTextAutoBoxSize(introText, introFooterFont, EnumTextOrientation.Left, header.Intro)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-section-layout"), sectionFont, form.SectionLayout)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-corner"), labelFont, form.LabelCorner)
            .AddDropDown(_areaCodes, _areaDisplay, _areaIndex, OnAreaCodeSelected, form.DropDownCorner, CairoFont.WhiteSmallText(), "areaDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-inset"), labelFont, form.LabelInset)
            .AddDropDown(_insetCodes, _insetDisplay, _insetPresetIndex, OnInsetPresetSelected, form.DropDownInset, CairoFont.WhiteSmallText(), "insetDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-section-appearance"), sectionFont, form.SectionAppearance)
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-icon-preset"), labelFont, form.LabelIcon)
            .AddDropDown(_iconCodes, _iconDisplay, _iconPresetIndex, OnIconPresetSelected, form.DropDownIcon, CairoFont.WhiteSmallText(), "iconDd")
            .AddStaticText(Lang.Get("playerstatusstrip:wizard-label-gap-preset"), labelFont, form.LabelGap)
            .AddDropDown(_gapCodes, _gapDisplay, _gapPresetIndex, OnGapPresetSelected, form.DropDownGap, CairoFont.WhiteSmallText(), "gapDd")
            .AddStaticTextAutoBoxSize(footerText, footerHintFont, EnumTextOrientation.Left, tip.Footer)
            .AddButton(
                Lang.Get("playerstatusstrip:wizard-apply"),
                OnApplyClicked,
                actions.Apply,
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                EnumButtonStyle.Normal,
                "btnApply")
            .AddButton(
                Lang.Get("playerstatusstrip:wizard-not-now"),
                OnSkipClicked,
                actions.Skip,
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
