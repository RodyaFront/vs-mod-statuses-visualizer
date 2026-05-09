using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

internal sealed class StripMockChatCommandService
{
    private const string StripMockPrefixDot = ".stripmock";
    private const string StripMockPrefixSlash = "/stripmock";

    private readonly Func<MockDevProvider?> _mockProviderAccessor;
    private readonly Action<string> _notify;

    internal StripMockChatCommandService(Func<MockDevProvider?> mockProviderAccessor, Action<string> notify)
    {
        _mockProviderAccessor = mockProviderAccessor;
        _notify = notify;
    }

    internal bool TryHandle(ref string message, ref global::Vintagestory.API.Common.EnumHandling handled)
    {
        MockDevProvider? mockDev = _mockProviderAccessor();
        if (mockDev == null)
        {
            return false;
        }

        string raw = message?.Trim() ?? "";
        if (!TryExtractTail(raw, out string tail))
        {
            return false;
        }

        handled = global::Vintagestory.API.Common.EnumHandling.PreventSubsequent;
        string[] parts = tail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

        if (parts.Length == 0 || cmd is "help" or "?")
        {
            _notify(Lang.Get("playerstatusstrip:mock-cmd-help-footer"));
            return true;
        }

        if (cmd == "list")
        {
            _notify(StripMockListText.Build());
            return true;
        }

        if (cmd == "stop")
        {
            mockDev.StopScenario();
            _notify(Lang.Get("playerstatusstrip:mock-stop"));
            return true;
        }

        if (cmd == "run")
        {
            if (parts.Length < 2)
            {
                _notify(Lang.Get("playerstatusstrip:mock-run-need-id"));
                return true;
            }

            string id = parts[1];
            if (!mockDev.TryStartScenario(id, out _))
            {
                _notify(Lang.Get("playerstatusstrip:mock-run-unknown", id));
                return true;
            }

            MockScenarioDefinition def = MockScenarioCatalog.All[id.Trim().ToLowerInvariant()];
            _notify(Lang.Get("playerstatusstrip:mock-run-started", Lang.Get(def.TitleLangKey)));
            return true;
        }

        _notify(Lang.Get("playerstatusstrip:mock-cmd-unknown-sub", cmd));
        return true;
    }

    internal static bool TryExtractTail(string raw, out string tail)
    {
        tail = "";
        if (raw.Length == 0)
        {
            return false;
        }

        if (raw.StartsWith(StripMockPrefixDot, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripMockPrefixDot.Length).Trim();
            return true;
        }

        if (raw.StartsWith(StripMockPrefixSlash, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripMockPrefixSlash.Length).Trim();
            return true;
        }

        return false;
    }
}
