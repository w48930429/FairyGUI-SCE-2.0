#if CLIENT
using System.Drawing;
using FairyGUI;

namespace GameEntry;

internal abstract partial class FguiUILogicBase : IFguiUILogic
{
    private static readonly char[] NormalizeStripChars =
        ['_', '-', ' ', '&', '/', '\\', '.', ':', ';', ',', '(', ')', '[', ']', '{', '}', '+', '*', '\''];

    protected GComponent? Root;
    public abstract string PackageName { get; }

    public virtual bool TryBind(GComponent view, out string message)
    {
        Root = view;
        message = "bound";
        return true;
    }

    public virtual bool RunSmoke(out string message)
    {
        message = "smoke: no-op";
        return true;
    }

    public virtual void Cleanup()
    {
    }

    protected static string NormalizeToken(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var token = input.Trim().ToLowerInvariant();
        foreach (var c in NormalizeStripChars)
        {
            token = token.Replace(c.ToString(), string.Empty);
        }

        return token;
    }

    protected static GObject? FindChildRecursive(GObject root, string name)
    {
        if (root.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        if (root is not GComponent component)
        {
            return null;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            var child = component.GetChildAt(i);
            var found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    protected static IEnumerable<GObject> EnumerateChildrenRecursive(GObject root)
    {
        yield return root;
        if (root is not GComponent component)
        {
            yield break;
        }

        for (var i = 0; i < component.NumChildren; i++)
        {
            var child = component.GetChildAt(i);
            foreach (var nested in EnumerateChildrenRecursive(child))
            {
                yield return nested;
            }
        }
    }

    protected static GList? FindListByName(GObject root, string name)
    {
        return FindChildRecursive(root, name) as GList;
    }

    protected static GTextField? FindTextFieldByName(GObject root, string name)
    {
        return FindChildRecursive(root, name) as GTextField;
    }

    protected static GTextInput? FindTextInputByName(GObject root, string name)
    {
        return FindChildRecursive(root, name) as GTextInput;
    }

    protected static GButton? FindButtonByName(GObject root, string name)
    {
        return FindChildRecursive(root, name) as GButton;
    }
}

#endif



