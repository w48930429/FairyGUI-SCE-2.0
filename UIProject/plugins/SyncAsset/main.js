'use strict';
Object.defineProperty(exports, '__esModule', { value: true });
exports.onDestroy = exports.onPublish = void 0;
// 把 srcDir 下匹配 pattern 的文件复制到 dstDir，返回复制数量。
function copyByPattern(srcDir, pattern, dstDir) {
    let files = CS.System.IO.Directory.GetFiles(srcDir, pattern);
    let n = files.Length;
    for (let i = 0; i < n; i++) {
        let f = files.get_Item(i);
        let dst = CS.System.IO.Path.Combine(dstDir, CS.System.IO.Path.GetFileName(f));
        CS.System.IO.File.Copy(f, dst, true);
    }
    return n;
}
// 根据发布目录 exportPath(=<client>/res/ui) 推导客户端并分发资源。
function syncAssets(exportPath) {
    if (!exportPath) { console.log('[SyncAsset] exportPath 为空，跳过'); return; }
    let resDir = CS.System.IO.Path.GetDirectoryName(exportPath); // <client>/res
    let clientRoot = CS.System.IO.Path.GetDirectoryName(resDir); // <client>
    let userFilesUi = CS.System.IO.Path.Combine(clientRoot, 'ui\\AppBundle\\user_files\\ui');
    let imageUi = CS.System.IO.Path.Combine(clientRoot, 'ui\\image\\ui');
    if (!CS.System.IO.Directory.Exists(userFilesUi)) CS.System.IO.Directory.CreateDirectory(userFilesUi);
    if (!CS.System.IO.Directory.Exists(imageUi)) CS.System.IO.Directory.CreateDirectory(imageUi);
    let desc = copyByPattern(exportPath, '*_fui.bytes', userFilesUi);
    let snd = copyByPattern(exportPath, '*.wav', userFilesUi);
    let png = copyByPattern(exportPath, '*.png', imageUi);
    console.log('[SyncAsset] 分发完成: 描述=' + desc + ' 音效=' + snd + ' 图集=' + png + '  -> ' + clientRoot);
}
function onPublish(handler) {
    // onPublish 在导出之前调用（此时资源还没写盘），所以注册“发布完成”回调再分发。
    handler.add_onComplete(() => {
        try {
            if (handler.isSuccess)
                syncAssets(handler.exportPath);
        }
        catch (e) {
            console.log('[SyncAsset] 分发失败: ' + e);
        }
    });
}
exports.onPublish = onPublish;
function onDestroy() {
    //do cleanup here
}
exports.onDestroy = onDestroy;
