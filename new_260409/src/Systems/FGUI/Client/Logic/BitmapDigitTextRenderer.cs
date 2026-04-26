#if CLIENT
using FairyGUI;

namespace GameEntry;

public sealed class BitmapDigitTextRenderer : IDisposable
{
    private const int DiagLogLimit = 320;
    private readonly GComponent host;
    private readonly GTextField target;
    private readonly IReadOnlyDictionary<char, string> charToUrl;
    private readonly IReadOnlyDictionary<char, float> advanceMap;
    private readonly bool skipNonDigits;
    private readonly char placeholderChar;
    private readonly GComponent root;
    private readonly List<GLoader> activeLoaders = new();
    private readonly Stack<GLoader> pooledLoaders = new();

    private bool disposed;
    private int loaderSerial;
    private int diagLogCount;
    private bool hostPathWarned;

    public BitmapDigitTextRenderer(
        GComponent host,
        GTextField target,
        IReadOnlyDictionary<char, string> charToUrl,
        IReadOnlyDictionary<char, float> advanceMap,
        bool skipNonDigits = true,
        char placeholderChar = '0')
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.charToUrl = charToUrl ?? throw new ArgumentNullException(nameof(charToUrl));
        this.advanceMap = advanceMap ?? throw new ArgumentNullException(nameof(advanceMap));
        this.skipNonDigits = skipNonDigits;
        this.placeholderChar = placeholderChar;

        root = new GComponent
        {
            Name = $"{target.Name}_bitmap_digits",
            Touchable = false,
            Visible = true,
        };

        this.host.AddChild(root);
        BringRootToFront();
        SyncRootPlacement();
        this.target.Visible = false;
    }

    public void SetText(string? text)
    {
        if (disposed)
        {
            return;
        }

        SyncRootPlacement();
        RecycleActiveLoaders();

        var content = text ?? string.Empty;
        var cursorX = 0f;
        var targetHeight = MathF.Max(1f, target.Height);
        var index = 0;
        GetAbsolutePosition(root, out var rootAbsX, out var rootAbsY);
        LogDiag(
            "[FGUI][BitmapDigit][DIAG] begin text='{Text}' target={Target} targetPos={TX},{TY} targetSize={TW}x{TH} targetVisible={TVisible} targetFinal={TFinal} targetAlpha={TAlpha} host={Host} hostVisible={HVisible} hostFinal={HFinal} hostAlpha={HAlpha} hostSize={HW}x{HH} rootPos={RX},{RY} rootAbs={RAX},{RAY} rootSize={RW}x{RH} rootVisible={RVisible} rootFinal={RFinal} rootAlpha={RAlpha}",
            content, target.Name, target.X, target.Y, target.Width, target.Height, target.Visible, target.FinalVisible,
            target.Alpha,
            host.Name, host.Visible, host.FinalVisible, host.Alpha, host.Width, host.Height,
            root.X, root.Y, rootAbsX, rootAbsY, root.Width, root.Height, root.Visible, root.FinalVisible, root.Alpha);

        foreach (var rawChar in content)
        {
            if (!TryResolveDigit(rawChar, out var digit, out var imageUrl))
            {
                LogDiag(
                    "[FGUI][BitmapDigit][DIAG] skip idx={Idx} rawChar={Raw} reason=resolve-failed skipNonDigits={SkipNonDigits}",
                    index, rawChar, skipNonDigits);
                index++;
                continue;
            }

            var loader = AcquireLoader();
            loader.Url = imageUrl;

            string contentType;
            string packageId;
            string itemId;
            bool hasSprite;
            if (loader.Content is GImage loadedImage && loadedImage.PackageItem != null)
            {
                contentType = nameof(GImage);
                packageId = loadedImage.PackageItem.Owner?.Id ?? "null";
                itemId = loadedImage.PackageItem.Id ?? "null";
                hasSprite = loadedImage.PackageItem.Sprite != null;

                if (!hasSprite && TryResolveScatterPathForNoSprite(loadedImage.PackageItem, out var fallbackPath))
                {
                    loadedImage.Icon = fallbackPath;
                    LogDiag(
                        "[FGUI][BitmapDigit][DIAG] no-sprite force icon pkgId={PkgId} itemId={ItemId} path={Path}",
                        packageId,
                        itemId,
                        fallbackPath);
                }
            }
            else if (loader.Content != null)
            {
                contentType = loader.Content.GetType().Name;
                packageId = "n/a";
                itemId = "n/a";
                hasSprite = false;
            }
            else
            {
                contentType = "null";
                packageId = "null";
                itemId = "null";
                hasSprite = false;
            }

            var sourceWidth = loader.SourceWidth > 0 ? loader.SourceWidth : ResolveAdvance(digit);
            var sourceHeight = loader.SourceHeight > 0 ? loader.SourceHeight : targetHeight;
            if (sourceHeight <= 0)
            {
                sourceHeight = targetHeight;
            }

            var scale = targetHeight / sourceHeight;
            // Keep bitmap digits close to source pixel size; only shrink when target area is smaller.
            if (scale > 1f)
            {
                scale = 1f;
            }

            var drawWidth = MathF.Max(1f, sourceWidth * scale);
            var drawHeight = MathF.Max(1f, sourceHeight * scale);
            var drawY = MathF.Max(0f, (targetHeight - drawHeight) * 0.5f);
            loader.SetSize(drawWidth, drawHeight, true);
            loader.SetXY(cursorX, drawY);

            root.AddChild(loader);
            activeLoaders.Add(loader);

            var advance = ResolveAdvance(digit) * scale;
            if (advance <= 0)
            {
                advance = drawWidth;
            }

            cursorX += MathF.Max(1f, advance);
            LogDiag(
                "[FGUI][BitmapDigit][DIAG] item idx={Idx} rawChar={Raw} digit={Digit} url={Url} loader={Loader} content={ContentType} pkgId={PkgId} itemId={ItemId} hasSprite={HasSprite} source={SW}x{SH} drawPos={X},{Y} drawSize={W}x{H} advance={Advance} visible={Visible} final={Final} alpha={Alpha}",
                index, rawChar, digit, imageUrl, loader.Name,
                contentType, packageId, itemId, hasSprite,
                sourceWidth, sourceHeight,
                loader.X, loader.Y, drawWidth, drawHeight, advance, loader.Visible, loader.FinalVisible, loader.Alpha);
            index++;
        }

        root.SetSize(MathF.Max(1f, cursorX), MathF.Max(1f, targetHeight), true);
        LogDiag(
            "[FGUI][BitmapDigit][DIAG] end text='{Text}' activeLoaders={Count} rootPos={RX},{RY} rootSize={RW}x{RH}",
            content, activeLoaders.Count, root.X, root.Y, root.Width, root.Height);
    }

    public void RefreshFromTarget()
    {
        SetText(target.Text);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            if (!target.Disposed)
            {
                target.Visible = true;
            }
        }
        catch
        {
            // Ignore target visibility restoration failures on disposed tree.
        }

        RecycleActiveLoaders();
        while (pooledLoaders.Count > 0)
        {
            var pooled = pooledLoaders.Pop();
            SafeDispose(pooled);
        }

        if (!root.Disposed)
        {
            if (root.Parent != null)
            {
                root.RemoveFromParent();
            }

            root.Dispose();
        }
    }

    private void SyncRootPlacement()
    {
        ResolveTargetPositionInHost(out var localX, out var localY);
        root.SetXY(localX, localY);
        root.SetSize(MathF.Max(1f, target.Width), MathF.Max(1f, target.Height), true);
        BringRootToFront();
        LogDiag(
            "[FGUI][BitmapDigit][DIAG] sync-root target={Target} targetPos={TX},{TY} targetSize={TW}x{TH} rootPos={RX},{RY} rootSize={RW}x{RH}",
            target.Name, target.X, target.Y, target.Width, target.Height,
            root.X, root.Y, root.Width, root.Height);
    }

    private void RecycleActiveLoaders()
    {
        for (var i = 0; i < activeLoaders.Count; i++)
        {
            var loader = activeLoaders[i];
            if (loader.Disposed)
            {
                continue;
            }

            if (loader.Parent != null)
            {
                loader.RemoveFromParent();
            }

            loader.Visible = false;
            pooledLoaders.Push(loader);
        }

        activeLoaders.Clear();
    }

    private GLoader AcquireLoader()
    {
        GLoader loader;
        if (pooledLoaders.Count > 0)
        {
            loader = pooledLoaders.Pop();
        }
        else
        {
            loader = new GLoader
            {
                Name = $"digit_loader_{++loaderSerial}",
                Touchable = false,
                Fill = FillType.None,
            };
        }

        loader.Visible = true;
        return loader;
    }

    private bool TryResolveDigit(char rawChar, out char digit, out string imageUrl)
    {
        imageUrl = string.Empty;
        digit = rawChar;

        if (!char.IsDigit(rawChar))
        {
            if (skipNonDigits)
            {
                return false;
            }

            digit = placeholderChar;
        }

        if (!charToUrl.TryGetValue(digit, out var resolvedUrl) || string.IsNullOrWhiteSpace(resolvedUrl))
        {
            return false;
        }

        imageUrl = resolvedUrl;
        return true;
    }

    private float ResolveAdvance(char digit)
    {
        if (advanceMap.TryGetValue(digit, out var advance) && advance > 0)
        {
            return advance;
        }

        return 32f;
    }

    private static void SafeDispose(GObject obj)
    {
        if (obj.Disposed)
        {
            return;
        }

        if (obj.Parent != null)
        {
            obj.RemoveFromParent();
        }

        obj.Dispose();
    }

    private void BringRootToFront()
    {
        if (root.Parent is not GComponent parent || parent.NumChildren <= 0)
        {
            return;
        }

        var topIndex = parent.NumChildren - 1;
        if (parent.GetChildAt(topIndex) == root)
        {
            return;
        }

        parent.SetChildIndex(root, topIndex);
    }

    private static void GetAbsolutePosition(GObject obj, out float x, out float y)
    {
        x = obj.X;
        y = obj.Y;

        var parent = obj.Parent;
        while (parent != null)
        {
            x += parent.X;
            y += parent.Y;
            parent = parent.Parent;
        }
    }

    private static bool TryResolveScatterPathForNoSprite(PackageItem item, out string path)
    {
        path = string.Empty;
        var owner = item.Owner;
        var packageName = owner?.Name;
        if (string.IsNullOrWhiteSpace(packageName))
        {
            packageName = owner?.Id;
        }

        if (string.IsNullOrWhiteSpace(packageName))
        {
            return false;
        }

        var itemId = item.Id;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        var rawName = item.Name;
        if (string.IsNullOrWhiteSpace(rawName))
        {
            rawName = itemId;
        }

        var normalizedName = Path.GetFileNameWithoutExtension(rawName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = itemId;
        }

        var chars = normalizedName.ToCharArray();
        var invalid = Path.GetInvalidFileNameChars();
        for (var i = 0; i < chars.Length; i++)
        {
            if (invalid.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }

        normalizedName = new string(chars).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = itemId;
        }

        path = $"image/fgui/scatter/{packageName}/{itemId}__{normalizedName}.png";
        return true;
    }

    private void ResolveTargetPositionInHost(out float x, out float y)
    {
        x = target.X;
        y = target.Y;

        var parent = target.Parent;
        while (parent != null && !ReferenceEquals(parent, host))
        {
            x += parent.X;
            y += parent.Y;
            parent = parent.Parent;
        }

        if (ReferenceEquals(parent, host))
        {
            return;
        }

        if (hostPathWarned)
        {
            return;
        }

        hostPathWarned = true;
        LogDiag(
            "[FGUI][BitmapDigit][DIAG] resolve-path-miss target={Target} host={Host} fallbackToTargetLocal=true",
            target.Name,
            host.Name);
    }

    private void LogDiag(string template, params object[] args)
    {
        if (diagLogCount >= DiagLogLimit)
        {
            return;
        }

        diagLogCount++;
        Game.Logger.LogWarning(template, args);
    }
}
#endif
