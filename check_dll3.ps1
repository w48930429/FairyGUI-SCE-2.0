$files = @(
  'D:/ProgramFiles/星火编辑器2/Update/editor-alpha.spark.xd.com/Res/_m/wasm/wasicoresdk/75/wasicoresdk/lib/client/gamesparkcore.dll',
  'D:/ProgramFiles/星火编辑器2/Update/editor-alpha.spark.xd.com/Res/_m/wasm/wasicoresdk/75/wasicoresdk/lib/client/gameui.dll',
  'D:/ProgramFiles/星火编辑器2/Update/editor-alpha.spark.xd.com/Res/_m/wasm/wasicoresdk/76/wasicoresdk/lib/client/gamesparkcore.dll',
  'D:/ProgramFiles/星火编辑器2/Update/editor-alpha.spark.xd.com/Res/_m/wasm/wasicoresdk/76/wasicoresdk/lib/client/gameui.dll'
)
foreach ($f in $files) {
  if (Test-Path $f) {
    $bytes = [System.IO.File]::ReadAllBytes($f)
    $txt = [System.Text.Encoding]::UTF8.GetString($bytes)
    $has = $txt -match 'GameDataLoadingUI'
    Write-Output ("{0} => GameDataLoadingUI: {1}" -f $f, $has)
  } else {
    Write-Output ("MISSING: {0}" -f $f)
  }
}
