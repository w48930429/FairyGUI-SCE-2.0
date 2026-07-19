#if CLIENT
namespace FairyGUI.Utils;

/// <summary>
/// FGUI 文本字体名 → 星火引擎字体 family 映射。
/// FGUI 编辑器里存的是字体显示名（如 "繁媛明朝"）或包资源引用（"ui://pkg/xxx"），
/// 而星火原生 Label/Input 需要的是 ref/fontref.txt 里预加载过的 family（如 "font/dongfangdakai"）。
/// 这里把前者解析成后者；解析不出来时返回 null，调用方保持默认字体。
/// </summary>
public static class FGUIFontMap
{
    private static readonly Dictionary<string, string> NameToFamily = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alimama DongFangDaKai"] = "font/dongfangdakai",
        ["阿里妈妈东方大楷"] = "font/dongfangdakai",
        ["dongfangdakai"] = "font/dongfangdakai",
        ["AlimamaDongFangDaKai-Regular.ttf"] = "font/dongfangdakai",
        ["AlimamaDongFangDaKai-Regular"] = "font/dongfangdakai",
    };

    private static readonly HashSet<string> WarnedUnknown = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 运行时登记额外的"字体名 → family"映射，供业务代码扩展。
    /// </summary>
    public static void Register(string fontName, string family)
    {
        if (string.IsNullOrWhiteSpace(fontName) || string.IsNullOrWhiteSpace(family))
        {
            return;
        }

        NameToFamily[fontName.Trim()] = family.Trim();
    }

    /// <summary>
    /// 把 FGUI 的字体名解析成引擎 family；返回 null 表示保持默认字体（不调用 SetFontName）。
    /// </summary>
    public static string? Resolve(string? fguiFont)
    {
        if (string.IsNullOrWhiteSpace(fguiFont))
        {
            return null;
        }

        var name = fguiFont.Trim();

        if (name.StartsWith("font/", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("ui/font/", StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        if (name.StartsWith("ui://", StringComparison.OrdinalIgnoreCase))
        {
            var resName = ResolvePackageItemName(name);
            if (!string.IsNullOrEmpty(resName))
            {
                if (NameToFamily.TryGetValue(resName!, out var byName))
                {
                    return byName;
                }

                var leaf = resName!;
                var slash = leaf.LastIndexOf('/');
                if (slash >= 0 && slash < leaf.Length - 1)
                {
                    leaf = leaf[(slash + 1)..];
                }

                var dot = leaf.LastIndexOf('.');
                if (dot > 0)
                {
                    leaf = leaf[..dot];
                }

                if (!leaf.Equals(resName, StringComparison.OrdinalIgnoreCase) &&
                    NameToFamily.TryGetValue(leaf, out var byLeaf))
                {
                    return byLeaf;
                }
            }

            WarnOnce(resName != null ? $"{name}（资源名={resName}）" : name);
            return null;
        }

        if (NameToFamily.TryGetValue(name, out var family))
        {
            return family;
        }

        WarnOnce(name);
        return null;
    }

    private static string? ResolvePackageItemName(string url)
    {
        try
        {
            return FairyGUI.UIPackage.GetItemByURL(url)?.Name;
        }
        catch
        {
            return null;
        }
    }

    private static void WarnOnce(string fontName)
    {
        if (WarnedUnknown.Add(fontName))
        {
            Game.Logger.LogWarning(
                "[FGUI][FONT] 未登记的字体名 {FontName}，已回退默认字体。请在 FGUIFontMap 补充映射到 ref/fontref.txt 的 family。",
                fontName);
        }
    }
}
#endif
