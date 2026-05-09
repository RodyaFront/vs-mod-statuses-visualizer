using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PlayerStatusStrip;

public class PlayerStatusStripModSystem : ModSystem
{
    private const string StripMockNetworkChannel = "playerstatusstrip-stripmock";

    private ICoreClientAPI? _capi;
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _stripMockServerChannel;
    private Action? _onLevelFinalize;
    private Action? _onLeftWorldStripHud;
    private ClientChatLineDelegate? _stripMockOutgoingChat;
    private ClientChatLineDelegate? _stripLayoutOutgoingChat;
    private StatusStripHudApi? _api;
    private StripMockChatCommandService? _stripMockChatService;
    private StripLayoutChatCommandService? _stripLayoutChatService;
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
        _stripLayoutChatService = new StripLayoutChatCommandService(api, () => _hud, OpenLayoutWizardFromMenu, NotifyStripMock);
        StatusStripDevConfig dev = StatusStripDevConfig.LoadOrCreate(api);
        _api.SetDiagnosticsWarningsEnabled(dev.DevMode && dev.EnableDiagnosticsWarnings);
        _stripLayoutOutgoingChat = (int _, ref string message, ref global::Vintagestory.API.Common.EnumHandling handled) =>
        {
            if (_stripLayoutChatService == null || !_stripLayoutChatService.TryHandle(ref message, ref handled))
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
            _stripMockChatService = new StripMockChatCommandService(() => _mockDev, NotifyStripMock);
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

            _stripMockOutgoingChat = (int _, ref string message, ref global::Vintagestory.API.Common.EnumHandling handled) =>
            {
                if (_stripMockChatService == null || !_stripMockChatService.TryHandle(ref message, ref handled))
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
        _stripMockChatService = null;
        _stripLayoutChatService = null;
        _api = null;

        CloseLayoutWizardWithoutSuppress();
        _capi = null;

        _hud?.TryClose();
        _hud?.Dispose();
        _hud = null;
        base.Dispose();
    }
}
