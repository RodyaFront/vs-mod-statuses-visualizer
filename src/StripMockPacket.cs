using System.Text;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

public sealed class StripMockPacket
{
    public int Op;

    public string Text = "";

    public string ScenarioId = "";
}

internal static class StripMockListText
{
    internal static string Build()
    {
        StringBuilder sb = new();
        foreach (System.Collections.Generic.KeyValuePair<string, MockScenarioDefinition> kv in MockScenarioCatalog.All)
        {
            sb.Append(kv.Key).Append(" тАФ ").Append(Lang.Get(kv.Value.TitleLangKey)).Append('\n');
        }

        sb.Append(Lang.Get("playerstatusstrip:mock-cmd-help-footer"));
        return sb.ToString().TrimEnd();
    }
}
