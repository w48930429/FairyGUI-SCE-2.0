'use strict';
Object.defineProperty(exports, '__esModule', { value: true });
exports.onDestroy = void 0;
// SkyMenu：AzureSail 自定义工具菜单集（单独 NPOT 批处理 + 勾选导出自动设置）。
const App = CS.FairyEditor.App;
const EditorEvents = CS.FairyEditor.EditorEvents;

function log(msg) { try { console.log('[SkyMenu] ' + msg); } catch (e) { } }

// 资源值更新后安全地重新选中资源，让编辑器自行刷新属性面板。
// 不直接重建检查器，避免在 PackageItemChanged 回调期间破坏编辑器 UI 生命周期。
function refreshResourceInspector(item) {
    try {
        if (item != null) App.libView.Highlight(item, false);
    } catch (e) { }
}

// 尽力从事件参数里取出 FPackageItem（不同版本 context 结构可能不同）。
function resolveItem(context) {
    try {
        if (context == null) return null;
        if (context.data != null && context.data.type !== undefined) return context.data;
        if (context.sender != null && context.sender.type !== undefined) return context.sender;
        if (context.type !== undefined && context.GetAsset !== undefined) return context;
    } catch (e) { }
    return null;
}

// 单项转换：type=image 且 exported 且 atlas!=alone_npot 时设为单独(NPOT)，返回是否改动。
function convertItemToAloneNpot(item) {
    try {
        if (item == null || item.type !== 'image' || !item.exported) return false;
        const asset = item.GetAsset();
        if (asset == null) { log(item.name + ' GetAsset=null'); return false; }
        const before = asset.atlas;
        if (before === 'alone_npot') return false;
        asset.atlas = 'alone_npot';
        const after = asset.atlas;
        item.SetChanged();
        log('转单独(NPOT): ' + item.name + '  atlas before=[' + before + '] after=[' + after + ']  assetType=' + (asset.GetType ? asset.GetType().Name : '?'));
        return true;
    } catch (e) {
        log('convert 异常: ' + e);
        return false;
    }
}

// 批处理：遍历所有包，把导出图片设为单独(NPOT)，返回改动数量。
function setExportedImagesToNpot() {
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

// 自动：勾选导出的瞬间把该图设为单独(NPOT)（带防递归）。
let applying = false;
function onPackageItemChanged(context) {
    if (applying) return;
    const item = resolveItem(context);
    if (item == null) return;
    const shouldRefresh = item.type === 'image' && item.exported;
    applying = true;
    try { convertItemToAloneNpot(item); } finally { applying = false; }
    if (shouldRefresh) refreshResourceInspector(item);
}

// —— 顶层菜单 SkyMenu ——
let skyMenu = null;
let rootMenu = null;
let createdRootMenu = false;
try {
    rootMenu = App.menu;
    // 清除旧插件实例留下的同名菜单，避免回调指向已释放的 JsEnv。
    try { rootMenu.RemoveItem('SkyMenu'); } catch (e) { }
    rootMenu.AddItem('SkyMenu', 'SkyMenu', -1, true, function (_n) { });
    skyMenu = rootMenu.GetSubMenu('SkyMenu');
    createdRootMenu = true;
} catch (e) {
    log('SkyMenu 建顶层失败, 兜底到 tool: ' + e);
    skyMenu = App.menu.GetSubMenu('tool');
    rootMenu = skyMenu;
    try { skyMenu.RemoveItem('sky_export_npot'); } catch (removeError) { }
}
if (skyMenu != null) {
    skyMenu.AddItem('导出图设为单独(NPOT)', 'sky_export_npot', function (_n) {
        const n = setExportedImagesToNpot();
        try { refreshResourceInspector(App.libView.GetSelectedResource()); } catch (e) { }
        App.Alert('已将 ' + n + ' 张导出图片设为 单独(NPOT)');
    });
}

// —— 自动监听 ——
App.On(EditorEvents.PackageItemChanged, onPackageItemChanged);
log('SkyMenu 已加载');

function onDestroy() {
    try { if (skyMenu != null) skyMenu.RemoveItem('sky_export_npot'); } catch (e) { }
    try { if (createdRootMenu && rootMenu != null) rootMenu.RemoveItem('SkyMenu'); } catch (e) { }
    try { App.Off(EditorEvents.PackageItemChanged, onPackageItemChanged); } catch (e) { }
}
exports.onDestroy = onDestroy;
