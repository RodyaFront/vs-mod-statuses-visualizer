using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PlayerStatusStrip;

internal sealed class StripLayoutChatCommandService
{
    private const string StripLayoutPrefixDot = ".striplayout";
    private const string StripLayoutPrefixSlash = "/striplayout";

    private readonly ICoreClientAPI _capi;
    private readonly Func<StatusStripHudElement?> _hudAccessor;
    private readonly Action _openLayoutWizard;
    private readonly Action<string> _notify;

    internal StripLayoutChatCommandService(
        ICoreClientAPI capi,
        Func<StatusStripHudElement?> hudAccessor,
        Action openLayoutWizard,
        Action<string> notify)
    {
        _capi = capi;
        _hudAccessor = hudAccessor;
        _openLayoutWizard = openLayoutWizard;
        _notify = notify;
    }

    internal bool TryHandle(ref string message, ref global::Vintagestory.API.Common.EnumHandling handled)
    {
        string raw = message?.Trim() ?? "";
        if (!TryExtractTail(raw, out string tail))
        {
            return false;
        }

        handled = global::Vintagestory.API.Common.EnumHandling.PreventSubsequent;
        string[] parts = tail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        switch (cmd)
        {
            case "":
            case "help":
            case "?":
                _notify(
                    $"Player Status HUD layout: .striplayout wizard | list | show | get [key] | set [key] [value] | reload. File: {StatusStripLayoutConfig.LayoutConfigFileName}. Reopen wizard: Ctrl+F8.");
                return true;
            case "wizard":
            case "setup":
                _openLayoutWizard();
                _notify(Lang.Get("playerstatusstrip:wizard-opened-chat"));
                return true;
            case "list":
                ShowStripLayoutKeyList();
                return true;
            case "show":
                ShowLayoutSummary();
                return true;
            case "reload":
                _hudAccessor()?.ReloadLayoutFromDisk();
                _notify($"Reloaded {StatusStripLayoutConfig.LayoutConfigFileName}.");
                return true;
            case "get":
                if (parts.Length < 2)
                {
                    _notify("Usage: .striplayout get [key]");
                    return true;
                }

                ShowLayoutValue(parts[1]);
                return true;
            case "set":
                if (parts.Length < 3)
                {
                    _notify("Usage: .striplayout set [key] [value]");
                    return true;
                }

                string key = parts[1];
                int keyStart = tail.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                string valueText = keyStart >= 0
                    ? tail.Substring(keyStart + key.Length).TrimStart()
                    : "";
                SetLayoutValue(key, valueText);
                return true;
            default:
                _notify($"Unknown layout subcommand: {cmd}. Use .striplayout help");
                return true;
        }
    }

    internal static bool TryExtractTail(string raw, out string tail)
    {
        tail = "";
        if (raw.Length == 0)
        {
            return false;
        }

        if (raw.StartsWith(StripLayoutPrefixDot, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripLayoutPrefixDot.Length).Trim();
            return true;
        }

        if (raw.StartsWith(StripLayoutPrefixSlash, StringComparison.OrdinalIgnoreCase))
        {
            tail = raw.Substring(StripLayoutPrefixSlash.Length).Trim();
            return true;
        }

        return false;
    }

    private void ShowStripLayoutKeyList()
    {
        _notify("Player Status HUD — layout keys (get/set):");
        int n = 0;
        StringBuilder chunk = new();
        foreach (StripLayoutKeyCatalog.Entry e in StripLayoutKeyCatalog.ChatEditableKeys)
        {
            chunk.Append("- ").Append(e.Key).Append(": ").AppendLine(e.Description);
            n++;
            if (n % 10 == 0)
            {
                _notify(chunk.ToString().TrimEnd());
                chunk.Clear();
            }
        }

        if (chunk.Length > 0)
        {
            _notify(chunk.ToString().TrimEnd());
        }

        _notify(StripLayoutKeyCatalog.AnimBlocksNote);
    }

    private void ShowLayoutSummary()
    {
        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        _notify(
            $"layout={StatusStripLayoutConfig.LayoutConfigFileName} area={cfg.DialogArea} off=({cfg.DialogOffsetX:F0},{cfg.DialogOffsetY:F0}) size=({cfg.DialogWidth:F0}x{cfg.DialogHeight:F0}) stripOff=({cfg.StatusStripOffsetX:F0},{cfg.StatusStripOffsetY:F0}) side={cfg.StatusStripSide} anchor={cfg.StatusStripAnchorMode} valign={cfg.StatusStripVerticalAlign} icon={cfg.StatusIconSize} gap={cfg.StatusIconGapPx}");
    }

    private void ShowLayoutValue(string key)
    {
        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        if (!TryResolveLayoutProperty(key, out PropertyInfo? property) || property is null)
        {
            _notify($"Unknown key '{key}'.");
            return;
        }

        object? value = property.GetValue(cfg);
        _notify($"{property.Name} = {FormatLayoutValue(value)}");
    }

    private void SetLayoutValue(string key, string valueText)
    {
        StatusStripLayoutConfig cfg = StatusStripLayoutConfig.Reload(_capi);
        if (!TryResolveLayoutProperty(key, out PropertyInfo? property) || property is null)
        {
            _notify($"Unknown key '{key}'.");
            return;
        }

        if (!TryConvertLayoutValue(property.PropertyType, valueText, out object? converted, out string error))
        {
            _notify(error);
            return;
        }

        property.SetValue(cfg, converted);
        cfg.EnsureDefaults();
        _capi.StoreModConfig(cfg, StatusStripLayoutConfig.LayoutConfigFileName);
        _hudAccessor()?.ReloadLayoutFromDisk();
        _notify($"{property.Name} set to {FormatLayoutValue(property.GetValue(cfg))}.");
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
}
