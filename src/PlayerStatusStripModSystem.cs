using System;
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

    private ICoreClientAPI? _capi;
    private ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _stripMockServerChannel;
    private Action? _onLevelFinalize;
    private ClientChatLineDelegate? _stripMockOutgoingChat;
    private StatusStripHudApi? _api;
    private StatusStripHudElement? _hud;
    private MockDevProvider? _mockDev;

    public IStatusStripHudApi? StatusApi => _api;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        api.Logger.Notification("[Player Status Strip] Mod loaded.");
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;
        _stripMockServerChannel = api.Network.RegisterChannel(StripMockNetworkChannel);
        _stripMockServerChannel.RegisterMessageType<StripMockPacket>();
        RegisterStripMockServerRoot(api, "stripmock");
        RegisterStripMockServerRoot(api, ".stripmock");
        api.Logger.Notification(
            "[Player Status Strip] Chat: stripmock / .stripmock (list | run <id> | stop); needs DevMode in ModConfig/{0}.",
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
            .WithDescription("Player Status Strip dev: mock HUD scenarios")
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
        if (dev.DevMode)
        {
            _mockDev = new MockDevProvider(dev.UseMockStatuses);
            _api.RegisterProvider(_mockDev);
            if (dev.UseMockStatuses)
            {
                api.Logger.Notification("[Player Status Strip] Dev: static mock icons on (playerstatusstrip-dev.json).");
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
            api.Logger.Notification("[Player Status Strip] Dev: chat /stripmock or .stripmock (list | run <id> | stop), or plain .stripmock … in message box.");
        }

        _hud = new StatusStripHudElement(api, _api);

        api.Input.RegisterHotKeyFirst(
            "playerstatusstrip_reloadlayout",
            $"Player Status Strip: reload HUD layout ({StatusStripLayoutConfig.LayoutConfigFileName})",
            GlKeys.F8,
            HotkeyType.HelpAndOverlays,
            false,
            false,
            false);
        api.Input.SetHotKeyHandler("playerstatusstrip_reloadlayout", _ =>
        {
            _hud?.ReloadLayoutFromDisk();
            api.Logger.Notification("[Player Status Strip] HUD layout reload hotkey handled.");
            return true;
        });

        _onLevelFinalize = () =>
        {
            if (_hud == null)
            {
                return;
            }

            if (_hud.TryOpen() != true)
            {
                api.Logger.Warning("[Player Status Strip] HUD TryOpen failed after level finalize.");
                return;
            }

            api.Logger.Notification("[Player Status Strip] Status strip HUD TryOpen ok.");
            api.Event.RegisterCallback(_ =>
            {
                ElementBounds? b = _hud?.SingleComposer?.Bounds;
                if (b != null)
                {
                    api.Logger.Notification(
                        $"[Player Status Strip] HUD bounds render=({b.renderX:F0},{b.renderY:F0}) outer={b.OuterWidthInt}x{b.OuterHeightInt}");
                }
            }, 300);
        };
        api.Event.LevelFinalize += _onLevelFinalize;
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

    private void NotifyStripMock(string text)
    {
        if (_capi?.World?.Player != null)
        {
            _capi.World.Player.ShowChatNotification(text);
        }
        else
        {
            _capi?.Logger.Notification("[Player Status Strip] " + text);
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
        _capi = null;

        _hud?.TryClose();
        _hud?.Dispose();
        _hud = null;
        base.Dispose();
    }
}
