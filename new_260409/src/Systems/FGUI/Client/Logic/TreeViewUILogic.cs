#if CLIENT
using FairyGUI;

namespace GameEntry;

internal sealed partial class TreeViewUILogic : FguiUILogicBase
{
    private GTree? tree1;
    private GTree? tree2;
    private string fileIconUrl = string.Empty;
    private string heartIconUrl = string.Empty;
    private bool fallbackListMode;
    private readonly List<TreeNodeVm> fallbackRootNodes = [];
    private readonly List<TreeNodeVm> fallbackVisibleNodes = [];

    public override string PackageName => "TreeView";

    public override bool TryBind(GComponent view, out string message)
    {
        Root = view;
        tree1 = FindChildRecursive(view, "tree") as GTree;
        tree2 = FindChildRecursive(view, "tree2") as GTree;
        if (tree1 == null && tree2 == null)
        {
            message = "bind failed: tree/tree2 missing";
            Game.Logger.LogWarning("[FGUI][TreeView] bind failed: tree/tree2 missing.");
            return false;
        }

        fileIconUrl = UIPackage.GetItemURL(PackageName, "file") ?? string.Empty;
        heartIconUrl = UIPackage.GetItemURL(PackageName, "heart") ?? string.Empty;

        if (tree1 != null)
        {
            tree1.OnClickItem.Add(OnTreeClick);
        }

        if (tree2 != null)
        {
            tree2.OnClickItem.Add(OnTreeClick);
            tree2.TreeNodeRender = RenderTreeNode;
            BuildTree2Data(tree2);
            ActivateFallbackIfNeeded(tree2);
        }

        message = "tree view bound";
        Game.Logger.LogWarning(
            "[FGUI][TreeView] bind ok tree1={Tree1} tree2={Tree2} fallback={Fallback}",
            tree1 != null,
            tree2 != null,
            fallbackListMode);
        return true;
    }

    public override void Cleanup()
    {
        if (tree1 != null)
        {
            tree1.OnClickItem.Remove(OnTreeClick);
        }

        if (tree2 != null)
        {
            tree2.OnClickItem.Remove(OnTreeClick);
            tree2.TreeNodeRender = null;
        }

        tree1 = null;
        tree2 = null;
        fileIconUrl = string.Empty;
        heartIconUrl = string.Empty;
        fallbackListMode = false;
        fallbackRootNodes.Clear();
        fallbackVisibleNodes.Clear();
        Root = null;
    }

    private void BuildTree2Data(GTree tree)
    {
        var rootNode = tree.RootNode;
        rootNode.RemoveChildren();

        var topNode = new GTreeNode(true) { Data = "I'm a top node" };
        rootNode.AddChild(topNode);
        for (var i = 0; i < 5; i++)
        {
            topNode.AddChild(new GTreeNode(false) { Data = $"Hello {i}" });
        }

        var folderNode = new GTreeNode(true) { Data = "A folder node" };
        topNode.AddChild(folderNode);
        for (var i = 0; i < 5; i++)
        {
            folderNode.AddChild(new GTreeNode(false) { Data = $"Good {i}" });
        }

        for (var i = 0; i < 3; i++)
        {
            topNode.AddChild(new GTreeNode(false) { Data = $"World {i}" });
        }

        var anotherTopNode = new GTreeNode(false) { Data = new[] { "I'm a top node too", heartIconUrl } };
        rootNode.AddChild(anotherTopNode);
        topNode.Expanded = true;
    }

    private void ActivateFallbackIfNeeded(GTree tree)
    {
        // Current SCE GTree backend does not realize dynamic node insertion yet.
        // If data exists on RootNode but no visible cells, switch to logic-side list rendering.
        fallbackListMode = tree.RootNode.NumChildren > 0 && tree.NumChildren == 0;
        if (!fallbackListMode)
        {
            return;
        }

        BuildFallbackData();
        RenderFallbackTree2();
        Game.Logger.LogWarning("[FGUI][TreeView] fallback-list mode enabled.");
    }

    private void RenderTreeNode(GTreeNode node, GComponent obj)
    {
        if (node.IsFolder)
        {
            obj.Text = node.Data as string ?? string.Empty;
            obj.Icon = string.Empty;
            return;
        }

        if (node.Data is string[] values && values.Length >= 2)
        {
            obj.Text = values[0];
            obj.Icon = values[1];
            return;
        }

        obj.Text = node.Data as string ?? string.Empty;
        obj.Icon = fileIconUrl;
    }

    private void OnTreeClick(EventContext context)
    {
        if (context.Data is not GObject clicked)
        {
            return;
        }

        if (fallbackListMode && ReferenceEquals(context.Sender, tree2) && clicked.Data is TreeNodeVm fallbackNode)
        {
            if (fallbackNode.IsFolder)
            {
                fallbackNode.Expanded = !fallbackNode.Expanded;
                RenderFallbackTree2();
            }

            if (!string.IsNullOrWhiteSpace(fallbackNode.Text))
            {
                FguiNotificationBridge.EnqueueSystemTip($"TreeView: {fallbackNode.Text}");
            }

            return;
        }

        var node = clicked._treeNode;
        var text = node?.Data switch
        {
            string s => s,
            string[] values when values.Length > 0 => values[0],
            _ => node?.Text ?? clicked.Text ?? string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(text))
        {
            FguiNotificationBridge.EnqueueSystemTip($"TreeView: {text}");
        }
    }

    private void BuildFallbackData()
    {
        fallbackRootNodes.Clear();

        var topNode = new TreeNodeVm("I'm a top node", isFolder: true);
        fallbackRootNodes.Add(topNode);
        for (var i = 0; i < 5; i++)
        {
            topNode.Children.Add(new TreeNodeVm($"Hello {i}", isFolder: false, icon: fileIconUrl));
        }

        var folderNode = new TreeNodeVm("A folder node", isFolder: true);
        topNode.Children.Add(folderNode);
        for (var i = 0; i < 5; i++)
        {
            folderNode.Children.Add(new TreeNodeVm($"Good {i}", isFolder: false, icon: fileIconUrl));
        }

        for (var i = 0; i < 3; i++)
        {
            topNode.Children.Add(new TreeNodeVm($"World {i}", isFolder: false, icon: fileIconUrl));
        }

        fallbackRootNodes.Add(new TreeNodeVm("I'm a top node too", isFolder: false, icon: heartIconUrl));
    }

    private void RenderFallbackTree2()
    {
        if (tree2 == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tree2.DefaultItem))
        {
            tree2.DefaultItem = global::TreeView.TreeItem.URL;
        }

        fallbackVisibleNodes.Clear();
        foreach (var rootNode in fallbackRootNodes)
        {
            CollectVisibleNodes(rootNode, level: 0, fallbackVisibleNodes);
        }

        tree2.RemoveChildrenToPool();
        for (var i = 0; i < fallbackVisibleNodes.Count; i++)
        {
            var node = fallbackVisibleNodes[i];
            var item = tree2.AddItemFromPool();
            item.Data = node;

            if (item is GButton button)
            {
                button.Text = node.Text;
                button.Icon = node.Icon;
            }

            if (item is global::TreeView.TreeItem treeItem)
            {
                if (treeItem.leaf != null)
                {
                    treeItem.leaf.SelectedIndex = node.IsFolder ? 0 : 1;
                }

                if (treeItem.expanded != null)
                {
                    treeItem.expanded.SelectedIndex = node.Expanded ? 1 : 0;
                }

                if (treeItem.expandButton != null)
                {
                    treeItem.expandButton.Visible = node.IsFolder;
                }

                if (treeItem.indent != null)
                {
                    var indentWidth = MathF.Max(0f, node.Level * tree2.Indent);
                    treeItem.indent.SetSize(indentWidth, treeItem.indent.Height, true);
                }
            }
        }
    }

    private static void CollectVisibleNodes(TreeNodeVm node, int level, List<TreeNodeVm> output)
    {
        node.Level = level;
        output.Add(node);
        if (!node.IsFolder || !node.Expanded)
        {
            return;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            CollectVisibleNodes(node.Children[i], level + 1, output);
        }
    }

    private sealed class TreeNodeVm
    {
        public TreeNodeVm(string text, bool isFolder, string icon = "")
        {
            Text = text;
            IsFolder = isFolder;
            Icon = icon;
        }

        public string Text { get; }
        public bool IsFolder { get; }
        public string Icon { get; }
        public bool Expanded { get; set; } = true;
        public int Level { get; set; }
        public List<TreeNodeVm> Children { get; } = [];
    }
}
#endif
