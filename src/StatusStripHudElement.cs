using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace PlayerStatusStrip;

public sealed class StatusStripHudElement : HudElement
{
    private static readonly Vec4f WhiteTint = new(1f, 1f, 1f, 1f);
    private const float BaselineEnterDurationSec = 0.28f;
    private const float BaselineUpdateDurationSec = 0.38f;
    private const float BaselineExitDurationSec = 0.24f;
    private const float BaselineScaleAmplitude = 0.11f;

    private readonly StatusStripHudApi _api;
    private StatusStripLayoutConfig _layout;
    private double _statusIconsWaveTimeSec;
    private readonly List<StatusDescriptor> _active = new(32);
    private readonly Dictionary<string, int> _texByPath = new();

    private readonly Dictionary<string, AssetLocation> _lastKnownIcon = new();
    private GuiElementRichtext? _statusTooltipRich;
    private StatusTooltipPanelBackend? _statusTooltipPanel;
    private LoadedTexture? _statusTooltipBgTex;
    private string? _lastHudTooltipVtml;

    private readonly Dictionary<string, KindRuntimeAnim> _kindAnim = new();
    private readonly List<PopoutAnim> _popoutAnims = new();
    private readonly HashSet<string> _prevActiveIds = new();
    private readonly HashSet<string> _scratchCurrentIds = new();
    private readonly Dictionary<string, float> _prevPulseMetric = new();
    private readonly Dictionary<string, SavedIconRect> _lastIconRect = new();
    private readonly Dictionary<string, StatusAffectKind> _lastAffectKind = new();
    private bool _statusAnimIntroSynced;

    private struct KindRuntimeAnim
    {
        public StatusAffectKind AffectKind;
        public float EnterElapsed;
        public float UpdateElapsed;
        public bool Updating;
    }

    private struct SavedIconRect
    {
        public double Left;
        public double Top;
        public int Size;
    }

    private sealed class PopoutAnim
    {
        public string StableId = "";

        public StatusAffectKind AffectKind;

        public int TexId;

        public float Elapsed;

        public double CenterX;

        public double CenterY;

        public int BaselineSize;
    }

    private readonly struct ResolvedAnimProfile
    {
        internal readonly StatusAffectKind EffectiveKind;
        internal readonly bool Enabled;
        internal readonly float EnterDurationSec;
        internal readonly float UpdateDurationSec;
        internal readonly float ExitDurationSec;
        internal readonly float ScaleAmplitude;
        internal readonly float ShakePx;
        internal readonly float SlideDownPx;

        internal ResolvedAnimProfile(
            StatusAffectKind effectiveKind,
            bool enabled,
            float enterDurationSec,
            float updateDurationSec,
            float exitDurationSec,
            float scaleAmplitude,
            float shakePx,
            float slideDownPx)
        {
            EffectiveKind = effectiveKind;
            Enabled = enabled;
            EnterDurationSec = enterDurationSec;
            UpdateDurationSec = updateDurationSec;
            ExitDurationSec = exitDurationSec;
            ScaleAmplitude = scaleAmplitude;
            ShakePx = shakePx;
            SlideDownPx = slideDownPx;
        }
    }

    private readonly struct AnimatedIconPose
    {
        internal readonly float Scale;
        internal readonly float Dx;
        internal readonly float Dy;
        internal readonly float Alpha;

        internal AnimatedIconPose(float scale, float dx, float dy, float alpha)
        {
            Scale = scale;
            Dx = dx;
            Dy = dy;
            Alpha = alpha;
        }
    }

    private static void TopAlignedSpriteRect(
        double cellLeft,
        double cellTop,
        int cellSize,
        in AnimatedIconPose pose,
        out double spriteLeft,
        out double spriteTop,
        out double spriteSide)
    {
        spriteSide = cellSize * pose.Scale;
        spriteLeft = cellLeft + pose.Dx + (cellSize - spriteSide) * 0.5;
        spriteTop = cellTop + pose.Dy;
    }

    internal StatusStripHudElement(ICoreClientAPI capi, StatusStripHudApi api)
        : base(capi)
    {
        _api = api;
        _layout = StatusStripLayoutConfig.LoadOrCreate(capi);
        ComposeHud();
    }

    internal void ReloadLayoutFromDisk()
    {
        _layout = StatusStripLayoutConfig.Reload(capi);
        ComposeHud();
        ResetStripTransientState();

        if (TryOpen() != true)
        {
            capi.Logger.Warning("[Player Status Strip] HUD TryOpen failed after layout reload (F8).");
        }

        if (capi.World is IClientWorldAccessor w && w.Player != null)
        {
            _layout.EnsureDefaults();
            w.Player.ShowChatNotification(
                $"Player Status Strip: layout reloaded ({StatusStripLayoutConfig.LayoutConfigFileName}). " +
                $"HUD {_layout.DialogArea} off=({_layout.DialogOffsetX:F0},{_layout.DialogOffsetY:F0}) size=({_layout.DialogWidth:F0}x{_layout.DialogHeight:F0}); " +
                $"strip off=({_layout.StatusStripOffsetX:F0},{_layout.StatusStripOffsetY:F0}) side={_layout.StatusStripSide} anchor={_layout.StatusStripAnchorMode} valign={_layout.StatusStripVerticalAlign}; " +
                $"anchorPx={_layout.AnchorWidthPx} anchorH={(_layout.StatusStripAnchorHeightPx > 0 ? _layout.StatusStripAnchorHeightPx.ToString() : "same")} outerW={(_layout.StatusStripAnchorOuterWidthPx > 0 ? _layout.StatusStripAnchorOuterWidthPx.ToString() : "bounds")}; " +
                $"icon={_layout.StatusIconSize} gap={_layout.StatusIconGapPx} nudge=({_layout.StatusStripIconNudgeX:F1},{_layout.StatusStripIconNudgeY:F1}) drawOrder={_layout.HudDrawOrder:F2}; " +
                $"lockRowBaseline={_layout.LockRowBaseline} wave={_layout.StatusIconsWaveEnabled}; " +
                $"trailRight={_layout.UseTrailingEdgeStatusStripAlign()} legacyLeadingRow={_layout.StatusStripUseLegacyLeadingEdgeRow}");
        }
    }

    private void ComposeHud()
    {
        GuiComposer? previous = SingleComposer;
        ElementBounds dialogBounds = _layout.DialogBounds();
        GuiComposer composer = capi.Gui.CreateCompo("playerstatusstrip-hud", dialogBounds);
        SingleComposer = composer.Compose();
        previous?.Dispose();
    }

    private void ResetStripTransientState()
    {
        _kindAnim.Clear();
        _popoutAnims.Clear();
        _prevActiveIds.Clear();
        _scratchCurrentIds.Clear();
        _prevPulseMetric.Clear();
        _lastIconRect.Clear();
        _lastAffectKind.Clear();
        _statusAnimIntroSynced = false;
        _lastHudTooltipVtml = null;
    }

    private void RefreshActiveStatuses(float deltaTime)
    {
        _active.Clear();
        _api.CollectMerged(capi, deltaTime, _active);
    }

    private StripLayoutNumbers BuildStripLayout(ElementBounds root, int activeCount)
    {
        _layout.EnsureDefaults();
        int anchor = _layout.AnchorWidthPx;
        return StatusStripLayoutMath.Compute(
            root.renderX,
            root.renderY,
            root.OuterWidth,
            anchor,
            _layout.StatusIconSize,
            _layout.StatusIconGapPx,
            _layout.StatusStripSide,
            _layout.StatusStripOffsetX,
            _layout.StatusStripOffsetY,
            _layout.StatusStripAnchorMode,
            _layout.StatusStripVerticalAlign,
            activeCount,
            _layout.StatusStripAnchorHeightPx,
            _layout.StatusStripAnchorOuterWidthPx,
            _layout.UseTrailingEdgeStatusStripAlign());
    }

    private StatusStripLayoutConfig.StatusAnimProfileConfig ProfileConfigFor(StatusAffectKind kind)
    {
        return kind switch
        {
            StatusAffectKind.Positive => _layout.PositiveAnim,
            StatusAffectKind.Negative => _layout.NegativeAnim,
            _ => _layout.NeutralAnim
        };
    }

    private ResolvedAnimProfile ResolveProfile(StatusAffectKind requestedKind)
    {
        StatusStripLayoutConfig.StatusAnimProfileConfig requested = ProfileConfigFor(requestedKind);
        StatusStripLayoutConfig.StatusAnimProfileConfig neutral = _layout.NeutralAnim;
        StatusAffectKind effectiveKind = StatusAnimMath.ResolveEffectiveKind(
            requestedKind,
            requested.Enabled,
            neutral.Enabled);
        bool enabled = StatusAnimMath.ResolveEffectiveEnabled(
            requestedKind,
            requested.Enabled,
            neutral.Enabled);
        StatusStripLayoutConfig.StatusAnimProfileConfig source =
            effectiveKind == StatusAffectKind.Neutral ? neutral : requested;

        float enterDur = Math.Max(1e-4f, enabled ? (float)source.EnterDurationSec : BaselineEnterDurationSec);
        float updateDur = Math.Max(1e-4f, enabled ? (float)source.UpdateDurationSec : BaselineUpdateDurationSec);
        float exitDur = Math.Max(1e-4f, enabled ? (float)source.ExitDurationSec : BaselineExitDurationSec);
        float amp = enabled ? (float)source.ScaleAmplitude : BaselineScaleAmplitude;
        float shake = enabled ? (float)source.ShakePx : 0f;
        float slide = enabled ? (float)source.SlideDownPx : 0f;
        return new ResolvedAnimProfile(effectiveKind, enabled, enterDur, updateDur, exitDur, amp, shake, slide);
    }

    private AnimatedIconPose ResolveActivePose(StatusAffectKind affectKind, in KindRuntimeAnim anim)
    {
        ResolvedAnimProfile p = ResolveProfile(affectKind);
        float enterProgress = Math.Clamp(anim.EnterElapsed / p.EnterDurationSec, 0f, 1f);
        float updateProgress = Math.Clamp(anim.UpdateElapsed / p.UpdateDurationSec, 0f, 1f);

        float scale = StatusAnimMath.EnterScale(p.EffectiveKind, enterProgress);
        if (anim.Updating)
        {
            scale *= StatusAnimMath.ComputePulseScale(updateProgress, p.ScaleAmplitude);
        }

        float dx = StatusAnimMath.HorizontalShakeOffset(p.EffectiveKind, enterProgress, p.ShakePx);
        float dy = _layout.LockRowBaseline
            ? 0f
            : StatusAnimMath.UpdateVerticalOffset(p.EffectiveKind, updateProgress, p.SlideDownPx);
        return new AnimatedIconPose(scale, dx, dy, 1f);
    }

    private AnimatedIconPose ResolvePopoutPose(in PopoutAnim popout)
    {
        ResolvedAnimProfile p = ResolveProfile(popout.AffectKind);
        float t = Math.Clamp(popout.Elapsed / p.ExitDurationSec, 0f, 1f);
        float scale = StatusAnimMath.ExitScale(p.EffectiveKind, t);
        float alpha = StatusAnimMath.ExitAlpha(t);
        return new AnimatedIconPose(scale, 0f, 0f, alpha);
    }

    private double WaveYOffsetPx(int visibleIndex)
    {
        if (_layout.LockRowBaseline || !_layout.StatusIconsWaveEnabled)
        {
            return 0;
        }

        double amp = _layout.StatusIconsWaveAmplitudePx;
        double period = _layout.StatusIconsWavePeriodSec;
        if (amp <= 1e-9 || period <= 1e-9)
        {
            return 0;
        }

        period = Math.Max(period, 1e-6);
        double stagger = _layout.StatusIconsWaveStaggerSec;
        double t = _statusIconsWaveTimeSec - visibleIndex * stagger;
        double phase = 2.0 * Math.PI * t / period;
        return amp * Math.Sin(phase);
    }

    private int ResolveTextureId(AssetLocation icon)
    {
        string k = icon.ToShortString();
        if (_texByPath.TryGetValue(k, out int cached))
        {
            return cached;
        }

        int id = capi.Render.GetOrLoadTexture(icon);
        if (id > 0)
        {
            _texByPath[k] = id;
        }

        return id;
    }

    public override void OnRenderGUI(float deltaTime)
    {
        _layout.EnsureDefaults();
        RefreshActiveStatuses(deltaTime);

        if (_active.Count == 0 && _popoutAnims.Count == 0)
        {
            if (_statusAnimIntroSynced)
            {
                ResetStripTransientState();
            }

            return;
        }

        ElementBounds root = SingleComposer.Bounds;
        _statusIconsWaveTimeSec += deltaTime;
        UpdateStatusIconAnimations(deltaTime, root);
        RenderActiveEffectIcons(root);
        RenderStatusTooltips(deltaTime, root);
    }

    private void GetIconScreen_LTWH(
        ref StripLayoutNumbers strip,
        int visibleIndex,
        out double left,
        out double top,
        out int sz)
    {
        double w = WaveYOffsetPx(visibleIndex);
        StatusStripLayoutMath.IconRect(ref strip, visibleIndex, w, out left, out top, out sz);
        left += _layout.StatusStripIconNudgeX;
        top += _layout.StatusStripIconNudgeY;
    }

    private bool TryPickStatusIcon(
        int mouseX,
        int mouseY,
        ElementBounds root,
        out int pickedIndex)
    {
        pickedIndex = -1;
        if (_active.Count == 0)
        {
            return false;
        }

        StripLayoutNumbers strip = BuildStripLayout(root, _active.Count);
        bool anim = _layout.StatusIconAnimEnabled;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            int tid = ResolveTextureId(_active[i].Icon);
            if (tid <= 0)
            {
                continue;
            }

            GetIconScreen_LTWH(ref strip, i, out double left, out double top, out int sz);
            if (!anim)
            {
                if (mouseX >= left && mouseX < left + sz && mouseY >= top && mouseY < top + sz)
                {
                    pickedIndex = i;
                    return true;
                }

                continue;
            }

            string id = _active[i].StableId;
            _ = _kindAnim.TryGetValue(id, out KindRuntimeAnim a);
            AnimatedIconPose pose = ResolveActivePose(_active[i].AffectKind, a);
            TopAlignedSpriteRect(left, top, sz, pose, out double sl, out double st, out double side);
            if (mouseX >= sl && mouseX < sl + side && mouseY >= st && mouseY < st + side)
            {
                pickedIndex = i;
                return true;
            }
        }

        return false;
    }

    private void EnsureStatusTooltipElements(ElementBounds root)
    {
        _layout.EnsureDefaults();
        _statusTooltipPanel ??= new StatusTooltipPanelBackend(capi);
        if (_statusTooltipRich != null)
        {
            return;
        }

        ElementBounds richBounds = ElementBounds.Fixed(0, 0, _layout.StatusTooltipMaxWidth, 320);
        richBounds.WithFixedPadding(0);
        richBounds.IsDrawingSurface = true;
        richBounds.WithParent(root);
        _statusTooltipRich = new GuiElementRichtext(
            capi,
            VtmlUtil.Richtextify(capi, " ", CairoFont.WhiteDetailText()),
            richBounds);
    }

    private void RenderStatusTooltips(float deltaTime, ElementBounds root)
    {
        int mx = capi.Input.MouseX;
        int my = capi.Input.MouseY;
        _layout.EnsureDefaults();

        if (_active.Count == 0)
        {
            _lastHudTooltipVtml = null;
            return;
        }

        if (!TryPickStatusIcon(mx, my, root, out int pickedIndex))
        {
            _lastHudTooltipVtml = null;
            return;
        }

        string statusVtml = _active[pickedIndex].TooltipVtml;
        StripLayoutNumbers strip = BuildStripLayout(root, _active.Count);
        GetIconScreen_LTWH(ref strip, pickedIndex, out double left, out double top, out int sz);
        const double gapBelowPx = 4;
        double tipLeft = left;
        double tipTop = top + sz + gapBelowPx;
        RenderRichTooltipAt(deltaTime, root, statusVtml, tipLeft, tipTop);
    }

    private void RenderRichTooltipAt(
        float deltaTime,
        ElementBounds root,
        string vtml,
        double desiredLeftScreenX,
        double desiredTopScreenY)
    {
        EnsureStatusTooltipElements(root);
        if (_statusTooltipRich == null || _statusTooltipPanel == null)
        {
            return;
        }

        int maxW = _layout.StatusTooltipMaxWidth;
        bool textDirty = vtml != _lastHudTooltipVtml;
        if (textDirty)
        {
            ElementBounds composeBounds = ElementBounds.Fixed(0, 0, maxW, 320);
            composeBounds.WithFixedPadding(0);
            composeBounds.IsDrawingSurface = true;
            composeBounds.WithParent(root);
            _statusTooltipRich.Bounds = composeBounds;
            _statusTooltipRich.SetNewText(vtml, CairoFont.WhiteDetailText());
            _lastHudTooltipVtml = vtml;
        }

        LoadedTexture rtex = _statusTooltipRich.richtTextTexture;
        if (rtex.TextureId <= 0 || rtex.Width <= 0 || rtex.Height <= 0)
        {
            return;
        }

        int tw = rtex.Width;
        int th = rtex.Height;
        double pad = GuiElement.scaled(5);
        int bw = (int)Math.Ceiling(tw + 2 * pad);
        int bh = (int)Math.Ceiling(th + 2 * pad);

        double guiScale = Math.Max(1e-6, RuntimeEnv.GUIScale);
        double textUnscaledW = tw / guiScale;
        double textUnscaledH = th / guiScale;

        if (textDirty || _statusTooltipBgTex == null || _statusTooltipBgTex.TextureId <= 0)
        {
            _statusTooltipPanel.FillPanelToTexture(bw, bh, ref _statusTooltipBgTex);
        }

        if (_statusTooltipBgTex == null || _statusTooltipBgTex.TextureId <= 0)
        {
            return;
        }

        double screenX = desiredLeftScreenX;
        double screenY = desiredTopScreenY;
        double totalW = bw;
        double totalH = bh;
        if (screenX + totalW > capi.Render.FrameWidth)
        {
            screenX = capi.Render.FrameWidth - totalW;
        }

        if (screenY + totalH > capi.Render.FrameHeight)
        {
            screenY = capi.Render.FrameHeight - totalH;
        }

        if (screenX < 0)
        {
            screenX = 0;
        }

        if (screenY < 0)
        {
            screenY = 0;
        }

        screenX = Math.Round(screenX);
        screenY = Math.Round(screenY);

        float zBg = _layout.StatusTooltipZ - 1f;
        float zTxt = _layout.StatusTooltipZ;

        capi.Render.Render2DTexture(
            _statusTooltipBgTex.TextureId,
            (float)screenX,
            (float)screenY,
            bw,
            bh,
            zBg,
            WhiteTint);

        double relTextX = (screenX - root.renderX + pad) / guiScale;
        double relTextY = (screenY - root.renderY + pad) / guiScale;
        ElementBounds tb = ElementBounds.Fixed(relTextX, relTextY, textUnscaledW, textUnscaledH);
        tb.WithFixedPadding(0);
        tb.WithParent(root);
        tb.CalcWorldBounds();
        _statusTooltipRich.Bounds = tb;
        _statusTooltipRich.zPos = zTxt;
        _statusTooltipRich.RenderInteractiveElements(deltaTime);
    }

    private Dictionary<string, SavedIconRect> CollectIconRects(ref StripLayoutNumbers strip)
    {
        Dictionary<string, SavedIconRect> rects = new();
        for (int i = 0; i < _active.Count; i++)
        {
            StatusDescriptor d = _active[i];
            if (ResolveTextureId(d.Icon) <= 0)
            {
                continue;
            }

            GetIconScreen_LTWH(ref strip, i, out double x, out double yDraw, out int sz);
            rects[d.StableId] = new SavedIconRect { Left = x, Top = yDraw, Size = sz };
        }

        return rects;
    }

    private void FillScratchCurrentIds()
    {
        _scratchCurrentIds.Clear();
        foreach (StatusDescriptor d in _active)
        {
            _scratchCurrentIds.Add(d.StableId);
        }
    }

    private bool KindPopupSettled(string stableId)
    {
        if (!_kindAnim.TryGetValue(stableId, out KindRuntimeAnim a))
        {
            return true;
        }

        ResolvedAnimProfile p = ResolveProfile(a.AffectKind);
        return a.EnterElapsed >= p.EnterDurationSec;
    }

    private void CommitAnimSnapshot(HashSet<string> currentIds, Dictionary<string, SavedIconRect> rects)
    {
        _prevActiveIds.Clear();
        foreach (string k in currentIds)
        {
            _prevActiveIds.Add(k);
        }

        _prevPulseMetric.Clear();
        foreach (StatusDescriptor d in _active)
        {
            if (d.PulseMetric.HasValue)
            {
                _prevPulseMetric[d.StableId] = d.PulseMetric.Value;
            }
        }

        _lastIconRect.Clear();
        foreach (KeyValuePair<string, SavedIconRect> kv in rects)
        {
            _lastIconRect[kv.Key] = kv.Value;
        }

        _lastAffectKind.Clear();
        foreach (StatusDescriptor d in _active)
        {
            _lastAffectKind[d.StableId] = d.AffectKind;
        }
    }

    private void UpdateStatusIconAnimations(float deltaTime, ElementBounds root)
    {
        foreach (StatusDescriptor d in _active)
        {
            _lastKnownIcon[d.StableId] = d.Icon;
            _lastAffectKind[d.StableId] = d.AffectKind;
        }

        FillScratchCurrentIds();
        StripLayoutNumbers strip = BuildStripLayout(root, _active.Count);
        Dictionary<string, SavedIconRect> rects = CollectIconRects(ref strip);

        if (!_layout.StatusIconAnimEnabled)
        {
            _popoutAnims.Clear();
            _kindAnim.Clear();
            CommitAnimSnapshot(_scratchCurrentIds, rects);
            _statusAnimIntroSynced = true;
            return;
        }

        for (int i = _popoutAnims.Count - 1; i >= 0; i--)
        {
            PopoutAnim p = _popoutAnims[i];
            p.Elapsed += deltaTime;
            if (p.Elapsed >= ResolveProfile(p.AffectKind).ExitDurationSec)
            {
                _popoutAnims.RemoveAt(i);
            }
        }

        if (!_statusAnimIntroSynced)
        {
            _kindAnim.Clear();
            float initialEnterElapsed = Math.Clamp(deltaTime, 0f, 1f);
            if (initialEnterElapsed <= 0f)
            {
                initialEnterElapsed = 1f / 120f;
            }

            foreach (string id in _scratchCurrentIds)
            {
                StatusAffectKind kind = _lastAffectKind.TryGetValue(id, out StatusAffectKind resolved)
                    ? resolved
                    : StatusAffectKind.Neutral;
                _kindAnim[id] = new KindRuntimeAnim
                {
                    AffectKind = kind,
                    EnterElapsed = initialEnterElapsed
                };
            }

            CommitAnimSnapshot(_scratchCurrentIds, rects);
            _statusAnimIntroSynced = true;
            return;
        }

        foreach (string id in _prevActiveIds)
        {
            if (_scratchCurrentIds.Contains(id))
            {
                continue;
            }

            _kindAnim.Remove(id);
            if (_lastIconRect.TryGetValue(id, out SavedIconRect r))
            {
                int popTex = 0;
                if (_lastKnownIcon.TryGetValue(id, out AssetLocation? loc) && loc != null)
                {
                    popTex = ResolveTextureId(loc);
                }

                _popoutAnims.Add(new PopoutAnim
                {
                    StableId = id,
                    AffectKind = _lastAffectKind.TryGetValue(id, out StatusAffectKind kind)
                        ? kind
                        : StatusAffectKind.Neutral,
                    TexId = popTex,
                    Elapsed = 0f,
                    CenterX = r.Left + r.Size * 0.5,
                    CenterY = r.Top + r.Size * 0.5,
                    BaselineSize = r.Size
                });
            }
        }

        foreach (string id in _scratchCurrentIds)
        {
            if (!_prevActiveIds.Contains(id))
            {
                StatusAffectKind kind = _lastAffectKind.TryGetValue(id, out StatusAffectKind resolved)
                    ? resolved
                    : StatusAffectKind.Neutral;
                _kindAnim[id] = new KindRuntimeAnim
                {
                    AffectKind = kind
                };
            }
        }

        foreach (StatusDescriptor d in _active)
        {
            if (!_prevActiveIds.Contains(d.StableId))
            {
                continue;
            }

            if (!d.PulseMetric.HasValue)
            {
                continue;
            }

            if (!_prevPulseMetric.TryGetValue(d.StableId, out float prevM))
            {
                continue;
            }

            float nowM = d.PulseMetric.Value;
            if (PulseMetricTrigger.ShouldPulse(prevM, nowM) && KindPopupSettled(d.StableId))
            {
                KindRuntimeAnim a = _kindAnim.TryGetValue(d.StableId, out KindRuntimeAnim x) ? x : default;
                a.AffectKind = d.AffectKind;
                a.Updating = true;
                a.UpdateElapsed = 0f;
                _kindAnim[d.StableId] = a;
            }
        }

        foreach (string id in _scratchCurrentIds)
        {
            KindRuntimeAnim a = _kindAnim.TryGetValue(id, out KindRuntimeAnim x) ? x : default;
            ResolvedAnimProfile p = ResolveProfile(a.AffectKind);
            if (a.EnterElapsed < p.EnterDurationSec)
            {
                a.EnterElapsed += deltaTime;
            }

            if (a.Updating)
            {
                a.UpdateElapsed += deltaTime;
                if (a.UpdateElapsed >= p.UpdateDurationSec)
                {
                    a.Updating = false;
                    a.UpdateElapsed = 0f;
                }
            }

            _kindAnim[id] = a;
        }

        CommitAnimSnapshot(_scratchCurrentIds, rects);
    }

    private void DrawStatusIconTopAligned(
        int texId,
        double cellLeft,
        double cellTop,
        int cellSize,
        in AnimatedIconPose pose,
        float z)
    {
        TopAlignedSpriteRect(cellLeft, cellTop, cellSize, pose, out double sl, out double st, out double side);
        Vec4f tint = pose.Alpha >= 0.999f ? WhiteTint : new Vec4f(1f, 1f, 1f, pose.Alpha);
        capi.Render.Render2DTexture(texId, (float)sl, (float)st, (float)side, (float)side, z, tint);
    }

    private void RenderActiveEffectIcons(ElementBounds root)
    {
        if (_active.Count == 0 && _popoutAnims.Count == 0)
        {
            return;
        }

        _layout.EnsureDefaults();

        StripLayoutNumbers strip = BuildStripLayout(root, _active.Count);
        float z = _layout.ZStatusIcons;

        if (!_layout.StatusIconAnimEnabled)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                int texId = ResolveTextureId(_active[i].Icon);
                if (texId <= 0)
                {
                    continue;
                }

                GetIconScreen_LTWH(ref strip, i, out double x, out double yDraw, out int sz);
                capi.Render.Render2DTexture(
                    texId,
                    (float)x,
                    (float)yDraw,
                    sz,
                    sz,
                    z,
                    WhiteTint);
            }

            return;
        }

        for (int i = 0; i < _active.Count; i++)
        {
            StatusDescriptor d = _active[i];
            int texId = ResolveTextureId(d.Icon);
            if (texId <= 0)
            {
                continue;
            }

            GetIconScreen_LTWH(ref strip, i, out double x, out double yDraw, out int sz);
            _ = _kindAnim.TryGetValue(d.StableId, out KindRuntimeAnim anim);
            AnimatedIconPose pose = ResolveActivePose(d.AffectKind, anim);
            DrawStatusIconTopAligned(texId, x, yDraw, sz, pose, z);
        }

        float zPop = z + 2f;
        for (int i = 0; i < _popoutAnims.Count; i++)
        {
            PopoutAnim p = _popoutAnims[i];
            int texId = p.TexId;
            if (texId <= 0)
            {
                continue;
            }

            AnimatedIconPose pose = ResolvePopoutPose(p);
            if (pose.Scale <= 0.01f || pose.Alpha <= 0.01f)
            {
                continue;
            }

            double cellLeft = p.CenterX - p.BaselineSize * 0.5;
            double cellTop = p.CenterY - p.BaselineSize * 0.5;
            DrawStatusIconTopAligned(texId, cellLeft, cellTop, p.BaselineSize, pose, zPop);
        }
    }

    public override double DrawOrder => _layout.HudDrawOrder;

    public override EnumDialogType DialogType => EnumDialogType.HUD;

    public override bool PrefersUngrabbedMouse => true;

    public override void Dispose()
    {
        _statusTooltipRich?.Dispose();
        _statusTooltipBgTex?.Dispose();
        base.Dispose();
    }
}
