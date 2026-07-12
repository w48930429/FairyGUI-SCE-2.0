// SkyMenu：AzureSail 自定义工具菜单集。
// 功能：
//   1) 顶层菜单 SkyMenu -> 各功能入口。
//   2) 「导出图设为单独(NPOT)」批处理：把所有勾选导出的图片纹理集设为 单独(NPOT)。
//   3) 勾选导出的瞬间自动把该图设为 单独(NPOT)（监听 PackageItemChanged）。
// 目的：散图渲染，用星火原生 SlicedEdges 九宫格，避免图集接缝/bleed。
import FairyEditor = CS.FairyEditor;

const App = FairyEditor.App;
const EditorEvents = FairyEditor.EditorEvents;

function log(message: string): void {
    try { console.log("[SkyMenu] " + message); } catch (e) { }
}

/**
 * 资源值更新后，安全地重新选中资源，让编辑器自行刷新资源属性面板。
 * 不直接重建检查器，避免在 PackageItemChanged 回调期间破坏编辑器 UI 生命周期。
 */
function refreshResourceInspector(item?: FairyEditor.FPackageItem | null): void {
    try {
        if (item != null) App.libView.Highlight(item, false);
    } catch (e) { }
}

/** 兼容不同编辑器版本的 PackageItemChanged 事件参数。 */
function resolveItem(context: FairyGUI.EventContext): FairyEditor.FPackageItem | null {
    try {
        const value = context as any;
        if (value == null) return null;
        if (value.data != null && value.data.type !== undefined) return value.data as FairyEditor.FPackageItem;
        if (value.sender != null && value.sender.type !== undefined) return value.sender as FairyEditor.FPackageItem;
        if (value.type !== undefined && value.GetAsset !== undefined) return value as FairyEditor.FPackageItem;
    } catch (e) { }
    return null;
}

/** 单项转换：type=image 且 exported 且 atlas!=alone_npot 时设为单独(NPOT)，返回是否改动。 */
function convertItemToAloneNpot(item: FairyEditor.FPackageItem): boolean {
    try {
        if (item == null || item.type !== "image" || !item.exported) return false;
        const asset = item.GetAsset() as FairyEditor.ImageAsset;
        if (asset == null) {
            log(item.name + " GetAsset=null");
            return false;
        }
        const before = asset.atlas;
        if (before === "alone_npot") return false;
        asset.atlas = "alone_npot";
        const after = asset.atlas;
        item.SetChanged();
        log("转单独(NPOT): " + item.name + "  atlas before=[" + before + "] after=[" + after + "]");
        return true;
    } catch (e) {
        log("convert 异常: " + e);
        return false;
    }
}

/** 批处理：遍历所有包，把导出图片设为 npot，返回改动数量。 */
function setExportedImagesToNpot(): number {
    let count = 0;
    const packages = App.project.allPackages;
    for (let i = 0; i < packages.Count; i++) {
        const items = packages.get_Item(i).items;
        for (let j = 0; j < items.Count; j++) {
            if (convertItemToAloneNpot(items.get_Item(j))) count++;
        }
    }
    if (count > 0) App.project.Save();
    return count;
}

/** 自动：项变化时，若是勾选导出的图片则立即转 npot（带防递归）。 */
let applying = false;
function onPackageItemChanged(context: FairyGUI.EventContext): void {
    if (applying) return;
    const item = resolveItem(context);
    if (item == null) return;
    const shouldRefresh = item.type === "image" && item.exported;
    applying = true;
    try { convertItemToAloneNpot(item); } finally { applying = false; }
    if (shouldRefresh) refreshResourceInspector(item);
}

// —— 顶层菜单 SkyMenu ——
let skyMenu: FairyEditor.Component.IMenu | null = null;
let rootMenu: FairyEditor.Component.IMenu | null = null;
let createdRootMenu = false;
try {
    rootMenu = App.menu;
    // 清除旧插件实例留下的同名菜单，避免回调指向已释放的 JsEnv。
    try { rootMenu.RemoveItem("SkyMenu"); } catch (e) { }
    rootMenu.AddItem("SkyMenu", "SkyMenu", -1, true, (_n: string) => { });
    skyMenu = rootMenu.GetSubMenu("SkyMenu");
    createdRootMenu = true;
} catch (e) {
    log("SkyMenu 建顶层失败，兜底到 tool: " + e);
    skyMenu = App.menu.GetSubMenu("tool");
    rootMenu = skyMenu;
    try { skyMenu.RemoveItem("sky_export_npot"); } catch (removeError) { }
}
if (skyMenu != null) {
    skyMenu.AddItem("导出图设为单独(NPOT)", "sky_export_npot", (_n: string) => {
        const n = setExportedImagesToNpot();
        try { refreshResourceInspector(App.libView.GetSelectedResource()); } catch (e) { }
        App.Alert("已将 " + n + " 张导出图片设为 单独(NPOT)");
    });
}

// —— 自动监听 ——
App.On(EditorEvents.PackageItemChanged, onPackageItemChanged);
log("SkyMenu 已加载");

function onDestroy() {
    try { if (skyMenu != null) skyMenu.RemoveItem("sky_export_npot"); } catch (e) { }
    try { if (createdRootMenu && rootMenu != null) rootMenu.RemoveItem("SkyMenu"); } catch (e) { }
    try { App.Off(EditorEvents.PackageItemChanged, onPackageItemChanged); } catch (e) { }
}

export { onDestroy };
