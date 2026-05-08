using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PlayerStatusStrip;

public class PlayerStatusStripModSystem : ModSystem
{
    private const string StripMockNetworkChannel = "playerstatusstrip-stripmock";

    private const string StripMockPrefixDot = ".stripmock";

    private const string StripMockPrefixSlash = "/stripmock";
    private const string StripLayoutPrefixDot = ".striplayout";
    private const string StripLayoutPrefixSlash = "/striplayout";

    private ICoreClientAPI? _capi;
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _stripMockServerChannel;
    private Action? _onLevelFinalize;
    private Action? _onLeftWorldStripHud;
    private ClientChatLineDelegate? _stripMockOutgoingChat;
    private ClientChatLineDelegate? _stripLayoutOutgoingChat;
    private StatusStripHudApi? _api;
    private StatusStripHudElement? _hud;
    private StripLayoutWizardDialog? _layoutWizard;
    private MockDevProvider? _mockDev;

    public IStatusStripHudApi? StatusApi => _api;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        api.Logger.Notification("[Player Status HUD] Mod loaded.");
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;
        _ = StatusStripDevConfig.LoadOrCreate(api);
        _stripMockServerChannel = api.Network.RegisterChannel(StripMockNetworkChannel);
        _stripMockServerChannel.RegisterMessageType<StripMockPacket>();
        RegisterStripMockServerRoot(api, "stripmock");
        RegisterStripMockServerRoot(api, ".stripmock");
        api.Logger.Notification(
            "[Player Status HUD] Chat: stripmock / .stripmock (list | run <id> | stop); needs DevMode in ModConfig/{0}.",
            StatusStripDevConfig.DevConfigFileName);
    }

    private TextCommandResult StripMockServerRequireDevOrError()
    {
        if (_sapi == null)
        {
            return TextCommandResult.Error("Server not ready.");
        }

        if (!StatusStripDevConfig.LoadOrCreate(_sapi).DevMode)
        {
            return TextCommandResult.Error(Lang.Get("playerstatusstrip:mock-devmode-off-server"));
        }

        return TextCommandResult.Success("");
    }

    private void RegisterStripMockServerRoot(ICoreServerAPI api, string rootName)
    {
        api.ChatCommands.Create(rootName)
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .WithDescription("Player Status HUD dev: mock HUD scenarios")
            .HandleWith(StripMockServerRoot)
            .BeginSubCommand("list")
            .WithDescription("List mock scenarios")
            .HandleWith(StripMockServerList)
            .EndSubCommand()
            .BeginSubCommand("run")
            .WithDescription("Run scenario by id")
            .WithArgs(api.ChatCommands.Parsers.Word("id"))
            .HandleWith(StripMockServerRun)
            .EndSubCommand()
            .BeginSubCommand("stop")
            .WithDescription("Stop mock scenario")
            .HandleWith(StripMockServerStop)
            .EndSubCommand();
    }

    private TextCommandResult StripMockServerRoot(TextCommandCallingArgs args)
    {
        TextCommandResult gate = StripMockServerRequireDevOrError();
        if (gate.Status != EnumCommandStatus.Success)
        {
            return gate;
        }

        return TextCommandResult.Success(Lang.Get("playerstatusstrip:mock-cmd-help-footer"));
    }

    private TextCommandResult StripMockServerList(TextCommandCallingArgs args)
    {
        TextCommandResult gate = StripMockServerRequireDevOrError();
        if (gate.Status != EnumCommandStatus.Success)
        {
            return gate;
        }

        IServerPlayer? pl = args.Caller.Player as IServerPlayer;
        if (pl == null)
        {
            return TextCommandResult.Error("No player.");
        }

        _stripMockServerChannel!.SendPacket(
            new StripMockPacket { Op = 0, Text = StripMockListText.Build() },
            new[] { pl });
        return TextCommandResult.Success("");
    }

    private TextCommandResult StripMockServerRun(TextCommandCallingArgs args)
    {
        TextCommandResult gate = StripMockServerRequireDevOrError();
        if (gate.Status != EnumCommandStatus.Success)
        {
            return gate;
        }

        IServerPlayer? pl = args.Caller.Player as IServerPlayer;
        if (pl == null)
        {
            return TextCommandResult.Error("No player.");
        }

        string id = ((string)args.Parsers[0].GetValue()).Trim();
        if (!MockScenarioCatalog.All.TryGetValue(id.ToLowerInvariant(), out _))
        {
            return TextCommandResult.Error(Lang.Get("playerstatusstrip:mock-run-unknown", id));
        }

        _stripMockServerChannel!.SendPacket(
            new StripMockPacket { Op = 1, ScenarioId = id.ToLowerInvariant() },
            new[] { pl });
        return TextCommandResult.Success("");
    }

    private TextCommandResult StripMockServerStop(TextCommandCallingArgs args)
    {
        TextCommandResult gate = StripMockServerRequireDevOrError();
        if (gate.Status != EnumCommandStatus.Success)
        {
            return gate;
        }

        IServerPlayer? pl = args.Caller.Player as IServerPlayer;
        if (pl == null)
        {
            return TextCommandResult.Error("No player.");
        }

        _stripMockServerChannel!.SendPacket(new StripMockPacket { Op = 2 }, new[] { pl });
        return TextCommandResult.Success("");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;

        _api = new StatusStripHudApi();
        StatusStripDevConfig dev = StatusStripDevConfig.LoadOrCreate(api);
        _stripLayoutOutgoingChat = (int _, ref string message, ref EnumHandling handled) =>
        {
            if (!TryHandleStripLayoutOutgoingChat(ref message, ref handled))
            {
                return;
            }
        };
        api.Event.OnSendChatMessage += _stripLayoutOutgoingChat;
        api.Logger.Notification(
            "[Player Status HUD] Layout chat: .striplayout (/striplayout) wizard | help | list | show | get [key] | set [key] [value] | reload. Layout wizard hotkey: Ctrl+F8.");
        if (dev.DevMode)
        {
            _mockDev = new MockDevProvider(dev.UseMockStatuses);
            _api.RegisterProvider(_mockDev);
            if (dev.UseMockStatuses)
            {
                api.Logger.Notification("[Player Status HUD] Dev: static mock icons on (playerstatusstrip-dev.json).");
            }
            else
            {
                api.Logger.Notification("[Player Status HUD] Dev: static mocks off by default; run /stripmock run <id> to visualize scenarios.");
            }

            IClientNetworkChannel netCh = api.Network.RegisterChannel(StripMockNetworkChannel);
            netCh.RegisterMessageType<StripMockPacket>();
            netCh.SetMessageHandler<StripMockPacket>(OnStripMockPacketFromServer);

            _stripMockOutgoingChat = (int _, ref string message, ref EnumHandling handled) =>
            {
                if (!TryHandleStripMockOutgoingChat(ref message, ref handled))
                {
                    return;
                }
            };
            api.Event.OnSendChatMessage += _stripMockOutgoingChat;
            api.Logger.Notification("[Player Status HUD] Dev: chat /stripmock or .stripmock (list | run <id> | stop), or plain .stripmock … in message box.");
        }

        _hud = new StatusStripHudElement(api, _api);

        _onLeftWorldStripHud = OnLeftWorldStripHud;
        api.Event.LeftWorld += _onLeftWorldStripHud;

        const string reloadLayoutHotkey = "playerstatusstrip_reloadlayout";
        if (!api.Input.HotKeys.ContainsKey(reloadLayoutHotkey))
        {
            api.Input.RegisterHotKeyFirst(
                reloadLayoutHotkey,
                $"Player Status HUD: reload HUD layout ({StatusStripLayoutConfig.LayoutConfigFileName})",
                GlKeys.F8,
                HotkeyType.HelpAndOverlays,
                false,
                false,
                false);
        }

        api.Input.SetHotKeyHandler(reloadLayoutHotkey, _ =>
        {
            _hud?.ReloadLayoutFromDisk();
            api.Logger.Notification("[Player Status HUD] HUD layout reload hotkey handled.");
            return true;
        });

        const string layoutWizardHotkey = "playerstatusstrip_layoutwizard";
        if (!api.Input.HotKeys.ContainsKey(layoutWizardHotkey))
        {
            api.Input.RegisterHotKeyFirst(
                layoutWizardHotkey,
                Lang.Get("playerstatusstrip:wizard-hotkey-desc"),
                GlKeys.F8,
                HotkeyType.HelpAndOverlays,
                false,
                true,
                false);
        }

        api.Input.SetHotKeyHandler(layoutWizardHotkey, _ =>
        {
            ToggleLayoutWizardHotkey();
            return true;
        });

        _onLevelFinalize = () => OnLevelFinalizeStripHud(api);
        api.Event.LevelFinalize += _onLevelFinalize;
    }

    private void OnLeftWorldStripHud()
    {
        CloseLayoutWizardWithoutSuppress();
        DisposeStripHud();
    }

    private void CloseLayoutWizardWithoutSuppress()
    {
        if (_layoutWizard == null)
        {
            return;
        }

        _layoutWizard.SuppressOnboardingWhenClosed = false;
        _layoutWizard.TryClose();
        _layoutWizard = null;
    }

    private void DisposeStripHud()
    {
        if (_hud == null)
        {
            return;
        }

        _hud.TryClose();
        _hud.Dispose();
        _hud = null;
    }

    private void OpenLayoutWizardFromMenu()
    {
        if (_capi == null || _hud == null || _api == null)
        {
            return;
        }

        if (_layoutWizard != null && _layoutWizard.IsOpened())
        {
            return;
        }

        _layoutWizard = new StripLayoutWizardDialog(_capi, _hud, _api);
        _layoutWizard.LayoutWizardClosed += () => { _layoutWizard = null; };
        _layoutWizard.TryOpen();
    }

    private void ToggleLayoutWizardHotkey()
    {
        if (_capi == null || _hud == null)
        {
            return;
        }

        if (_layoutWizard != null && _layoutWizard.IsOpened())
        {
            _layoutWizard.TryClose();
            return;
        }

        OpenLayoutWizardFromMenu();
    }

    private void TryAutoShowLayoutWizard()
    {
        if (_capi == null || _hud == null)
        {
            return;
        }

        StatusStripDevConfig dev = StatusStripDevConfig.LoadOrCreate(_capi);
        bool bypassOnboardingSuppress = dev.DevMode && dev.AlwaysAutoLayoutWizard;
        if (!bypassOnboardingSuppress && StatusStripOnboardingConfig.LoadOrCreate(_capi).SuppressAutoLayoutWizard)
        {
            return;
        }

        if (_layoutWizard != null && _layoutWizard.IsOpened())
        {
            return;
        }

        OpenLayoutWizardFromMenu();
    }

    private void OnLevelFinalizeStripHud(ICoreClientAPI api)
    {
        if (_api == null)
        {
            return;
        }

        if (_hud == null)
        {
            _hud = new StatusStripHudElement(api, _api);
        }

        if (_hud.TryOpen() == true)
        {
            LogStripHudOpenedOk(api);
            return;
        }

        api.Logger.Warning("[Player Status HUD] HUD TryOpen failed after level finalize; recreating HUD element.");
        DisposeStripHud();
        _hud = new StatusStripHudElement(api, _api);
        if (_hud.TryOpen() != true)
        {
            api.Logger.Warning("[Player Status HUD] HUD TryOpen failed after recreate.");
            return;
        }

        LogStripHudOpenedOk(api);
    }

    private void LogStripHudOpenedOk(ICoreClientAPI api)
    {
        api.Logger.Notification("[Player Status HUD] Status strip HUD TryOpen ok.");
        api.Event.RegisterCallback(_ =>
        {
            ElementBounds? b = _hud?.SingleComposer?.Bounds;
            if (b != null)
            {
                api.Logger.Notification(
                    $"[Player Status HUD] HUD bounds render=({b.renderX:F0},{b.renderY:F0}) outer={b.OuterWidthInt}x{b.OuterHeightInt}");
            }
        }, 300);
        api.Event.RegisterCallback(_ => TryAutoShowLayoutWizard(), 650);
    }

    private void OnStripMockPacketFromServer(StripMockPacket p)
    {
        if (_mockDev == null)
        {
            return;
        }

        switch (p.Op)
        {
            case 0:
                NotifyStripMock(p.Text);
                break;
            case 1:
                if (!_mockDev.TryStartScenario(p.ScenarioId, out _))
                {
                    NotifyStripMock(Lang.Get("playerstatusstrip:mock-run-unknown", p.ScenarioId));
                }
                else if (MockScenarioCatalog.All.TryGetValue(p.ScenarioId, out MockScenarioDefinition? def))
                {
                    NotifyStripMock(Lang.Get("playerstatusstrip:mock-run-started", Lang.Get(def.TitleLangKey)));
                }

                break;
            case 2:
                _mockDev.StopScenario();
                NotifyStripMock(Lang.Get("playerstatusstrip:mock-stop"));
                break;
        }
    }

    private bool TryHandleStripMockOutgoingChat(ref string message, ref EnumHandling handled)
    {
        if (_capi == null || _mockDev == null)
        {
            return false;
        }

        string raw = message?.Trim() ?? "";
        if (raw.Length == 0)
        {
            return false;
        }

        string tail;
        if (raw.StartsWith(StripMockPrefixDot, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripMockPrefixDot.Length).Trim();
        }
        else if (raw.StartsWith(StripMockPrefixSlash, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripMockPrefixSlash.Length).Trim();
        }
        else
        {
            return false;
        }

        handled = EnumHandling.PreventSubsequent;
        string[] parts = tail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

        if (parts.Length == 0 || cmd is "help" or "?")
        {
            NotifyStripMock(Lang.Get("playerstatusstrip:mock-cmd-help-footer"));
            return true;
        }

        if (cmd == "list")
        {
            NotifyStripMock(StripMockListText.Build());
            return true;
        }

        if (cmd == "stop")
        {
            _mockDev.StopScenario();
            NotifyStripMock(Lang.Get("playerstatusstrip:mock-stop"));
            return true;
        }

        if (cmd == "run")
        {
            if (parts.Length < 2)
            {
                NotifyStripMock(Lang.Get("playerstatusstrip:mock-run-need-id"));
                return true;
            }

            string id = parts[1];
            if (!_mockDev.TryStartScenario(id, out _))
            {
                NotifyStripMock(Lang.Get("playerstatusstrip:mock-run-unknown", id));
                return true;
            }

            MockScenarioDefinition def = MockScenarioCatalog.All[id.Trim().ToLowerInvariant()];
            NotifyStripMock(Lang.Get("playerstatusstrip:mock-run-started", Lang.Get(def.TitleLangKey)));
            return true;
        }

        NotifyStripMock(Lang.Get("playerstatusstrip:mock-cmd-unknown-sub", cmd));
        return true;
    }

    private bool TryHandleStripLayoutOutgoingChat(ref string message, ref EnumHandling handled)
    {
        if (_capi == null)
        {
            return false;
        }

        string raw = message?.Trim() ?? "";
        if (raw.Length == 0)
        {
            return false;
        }

        string tail;
        if (raw.StartsWith(StripLayoutPrefixDot, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripLayoutPrefixDot.Length).Trim();
        }
        else if (raw.StartsWith(StripLayoutPrefixSlash, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripLayoutPrefixSlash.Length).Trim();
        }
        else
        {
            return false;
        }

        handled = EnumHandling.PreventSubsequent;
        string[] parts = tail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        switch (cmd)
        {
            case "":
            case "help":
            case "?":
                NotifyStripMock(
                    $"Player Status HUD layout: .striplayout wizard | list | show | get [key] | set [key] [value] | reload. File: {StatusStripLayoutConfig.LayoutConfigFileName}. Reopen wizard: Ctrl+F8.");
                return true;
            case "wizard":
            case "setup":
                OpenLayoutWizardFromMenu();
                NotifyStripMock(Lang.Get("playerstatusstrip:wizard-opened-chat"));
                return true;
            case "list":
                ShowStripLayoutKeyList();
                return true;
            case "show":
                ShowLayoutSummary();
                return true;
            case "reload":
                _hud?.ReloadLayoutFromDisk();
                NotifyStripMock($"Reloaded {StatusStripLayoutConfig.LayoutConfigFileName}.");
                return true;
            case "get":
                if (parts.Length < 2)
                {
                    NotifyStripMock("Usage: .striplayout get [key]");
                    return true;
                }

                ShowLayoutValue(parts[1]);
                return true;
            case "set":
                if (parts.Length < 3)
                {
                    NotifyStripMock("Usage: .striplayout set [key] [value]");
                    return true;
                }

                string key = parts[1];
                string valueText = tail.Substring(tail.IndexOf(key, StringComparison.OrdinalIgnoreCase) + key.Length).Trim();
                if (valueText.StartsWith(" ", StringComparison.Ordinal))
                {
                    valueText = valueText.TrimStart();
                }

                SetLayoutValue(key, valueText);
                return true;
            default:
                NotifyStripMock($"Unknown layout subcommand: {cmd}. Use .striplayout help");
                return true;
        }
    }

    private void ShowStripLayoutKeyList()
    {
        NotifyStripMock("Player Status HUD — layout keys (get/set):");
        int n = 0;
        StringBuilder chunk = new();
        foreach (StripLayoutKeyCatalog.Entry e in StripLayoutKeyCatalog.ChatEditableKeys)
        {
            chunk.Append("- ").Append(e.Key).Append(": ").AppendLine(e.Description);
            n++;
            if (n % 10 == 0)
            {
                NotifyStripMock(chunk.ToString().TrimEnd());
                chunk.Clear();
            }
        }

        if (chunk.Length > 0)
        {
            NotifyStripMock(chunk.ToString().TrimEnd());
        }

        NotifyStripMock(StripLayoutKeyCatalog.AnimBlocksNote);
    }

    private void ShowLayoutSummary()
    {
        if (_capi == null)
        {
            return;
        }

        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        NotifyStripMock(
            $"layout={StatusStripLayoutConfig.LayoutConfigFileName} area={cfg.DialogArea} off=({cfg.DialogOffsetX:F0},{cfg.DialogOffsetY:F0}) size=({cfg.DialogWidth:F0}x{cfg.DialogHeight:F0}) stripOff=({cfg.StatusStripOffsetX:F0},{cfg.StatusStripOffsetY:F0}) side={cfg.StatusStripSide} anchor={cfg.StatusStripAnchorMode} valign={cfg.StatusStripVerticalAlign} icon={cfg.StatusIconSize} gap={cfg.StatusIconGapPx}");
    }

    private void ShowLayoutValue(string key)
    {
        if (_capi == null)
        {
            return;
        }

        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        if (!TryResolveLayoutProperty(key, out PropertyInfo? property) || property is null)
        {
            NotifyStripMock($"Unknown key '{key}'.");
            return;
        }

        object? value = property.GetValue(cfg);
        NotifyStripMock($"{property.Name} = {FormatLayoutValue(value)}");
    }

    private void SetLayoutValue(string key, string valueText)
    {
        if (_capi == null)
        {
            return;
        }

        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        if (!TryResolveLayoutProperty(key, out PropertyInfo? property) || property is null)
        {
            NotifyStripMock($"Unknown key '{key}'.");
            return;
        }

        if (!TryConvertLayoutValue(property.PropertyType, valueText, out object? converted, out string error))
        {
            NotifyStripMock(error);
            return;
        }

        property.SetValue(cfg, converted);
        cfg.EnsureDefaults();
        _capi.StoreModConfig(cfg, StatusStripLayoutConfig.LayoutConfigFileName);
        _hud?.ReloadLayoutFromDisk();
        NotifyStripMock($"{property.Name} set to {FormatLayoutValue(property.GetValue(cfg))}.");
    }

    private static bool TryResolveLayoutProperty(string key, out PropertyInfo? property)
    {
        property = typeof(StatusStripLayoutConfig).GetProperty(
            key,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (property == null || !property.CanRead || !property.CanWrite)
        {
            property = null;
            return false;
        }

        Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (t != typeof(string)
            && t != typeof(bool)
            && t != typeof(int)
            && t != typeof(float)
            && t != typeof(double))
        {
            property = null;
            return false;
        }

        return true;
    }

    private static bool TryConvertLayoutValue(Type propertyType, string valueText, out object? value, out string error)
    {
        Type target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        string raw = valueText.Trim();
        value = null;
        error = "";
        if (target == typeof(string))
        {
            value = raw;
            return true;
        }

        if (target == typeof(bool))
        {
            if (bool.TryParse(raw, out bool parsed))
            {
                value = parsed;
                return true;
            }

            error = $"Invalid bool '{valueText}'. Use true/false.";
            return false;
        }

        if (target == typeof(int))
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                value = parsed;
                return true;
            }

            error = $"Invalid int '{valueText}'.";
            return false;
        }

        if (target == typeof(float))
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                value = parsed;
                return true;
            }

            error = $"Invalid float '{valueText}'.";
            return false;
        }

        if (target == typeof(double))
        {
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                value = parsed;
                return true;
            }

            error = $"Invalid double '{valueText}'.";
            return false;
        }

        error = "Unsupported value type.";
        return false;
    }

    private static string FormatLayoutValue(object? value)
    {
        return value switch
        {
            null => "null",
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }

    private void NotifyStripMock(string text)
    {
        if (_capi?.World?.Player != null)
        {
            _capi.World.Player.ShowChatNotification(text);
        }
        else
        {
            _capi?.Logger.Notification("[Player Status HUD] " + text);
        }
    }

    public override void Dispose()
    {
        _sapi = null;
        _stripMockServerChannel = null;

        if (_capi != null && _stripMockOutgoingChat != null)
        {
            _capi.Event.OnSendChatMessage -= _stripMockOutgoingChat;
        }

        _stripMockOutgoingChat = null;
        if (_capi != null && _stripLayoutOutgoingChat != null)
        {
            _capi.Event.OnSendChatMessage -= _stripLayoutOutgoingChat;
        }

        _stripLayoutOutgoingChat = null;

        if (_capi != null && _onLeftWorldStripHud != null)
        {
            _capi.Event.LeftWorld -= _onLeftWorldStripHud;
        }

        _onLeftWorldStripHud = null;

        if (_capi != null && _onLevelFinalize != null)
        {
            _capi.Event.LevelFinalize -= _onLevelFinalize;
        }

        _onLevelFinalize = null;

        if (_api != null && _mockDev != null)
        {
            _api.UnregisterProvider(_mockDev);
        }

        _mockDev = null;
        _api = null;

        CloseLayoutWizardWithoutSuppress();
        _capi = null;

        _hud?.TryClose();
        _hud?.Dispose();
        _hud = null;
        base.Dispose();
    }
}
