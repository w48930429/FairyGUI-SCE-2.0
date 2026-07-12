//FYI: https://github.com/Tencent/puerts/blob/master/doc/unity/manual.md
// SyncAsset：FairyGUI 发布后，把资源自动分发到星火客户端。
//   描述文件 *_fui.bytes / 音效 *.wav  -> <client>/ui/AppBundle/user_files/ui  (客户端 File.ReadAllBytes)
//   图集/图片 *.png                     -> <client>/ui/image/ui                (引擎图片 image/ui/xxx)
// 发布目录已配为 <client>/res/ui，客户端根据 exportPath 上两级推导。

import FairyEditor = CS.FairyEditor;

/** 把 srcDir 下匹配 pattern 的文件复制到 dstDir，返回复制数量。 */
function copyByPattern(srcDir: string, pattern: string, dstDir: string): number {
    let files = CS.System.IO.Directory.GetFiles(srcDir, pattern);
    let n = files.Length;
    for (let i = 0; i < n; i++) {
        let f = files.get_Item(i);
        let dst = CS.System.IO.Path.Combine(dstDir, CS.System.IO.Path.GetFileName(f));
        CS.System.IO.File.Copy(f, dst, true);
    }
    return n;
}

/** 根据发布目录 exportPath(=<client>/res/ui) 推导客户端并分发资源。 */
function syncAssets(exportPath: string): void {
    if (!exportPath) { console.log('[SyncAsset] exportPath 为空，跳过'); return; }
    let resDir = CS.System.IO.Path.GetDirectoryName(exportPath);   // <client>/res
    let clientRoot = CS.System.IO.Path.GetDirectoryName(resDir);   // <client>
    let userFilesUi = CS.System.IO.Path.Combine(clientRoot, "ui\\AppBundle\\user_files\\ui");
    let imageUi = CS.System.IO.Path.Combine(clientRoot, "ui\\image\\ui");
    if (!CS.System.IO.Directory.Exists(userFilesUi)) CS.System.IO.Directory.CreateDirectory(userFilesUi);
    if (!CS.System.IO.Directory.Exists(imageUi)) CS.System.IO.Directory.CreateDirectory(imageUi);

    let desc = copyByPattern(exportPath, "*_fui.bytes", userFilesUi);
    let snd = copyByPattern(exportPath, "*.wav", userFilesUi);
    let png = copyByPattern(exportPath, "*.png", imageUi);
    console.log('[SyncAsset] 分发完成: 描述=' + desc + ' 音效=' + snd + ' 图集=' + png + '  -> ' + clientRoot);
}

function onPublish(handler: FairyEditor.PublishHandler) {
    // onPublish 在导出之前调用（此时资源还没写盘），所以注册“发布完成”回调再分发。
    handler.add_onComplete(() => {
        try {
            if (handler.isSuccess) syncAssets(handler.exportPath);
        } catch (e) {
            console.log('[SyncAsset] 分发失败: ' + e);
        }
    });
}

function onDestroy() {
    //do cleanup here
}

export { onPublish, onDestroy };
