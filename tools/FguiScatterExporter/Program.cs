using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class Program
{
    private const uint FguiMagic = 0x46475549;

    private static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            Directory.CreateDirectory(options.OutputDir);

            var bytesFiles = Directory
                .EnumerateFiles(options.InputDir, "*_fui.bytes", SearchOption.AllDirectories)
                .OrderBy(Path.GetFileName)
                .ToArray();

            if (bytesFiles.Length == 0)
            {
                Console.Error.WriteLine($"[FAIL] no *_fui.bytes found under: {options.InputDir}");
                return 2;
            }

            var manifest = new ScatterManifest
            {
                Version = 1,
                GeneratedAt = DateTimeOffset.Now.ToString("O"),
                Entries = new List<ScatterEntry>()
            };
            var movieClipManifest = new MovieClipScatterManifest
            {
                Version = 1,
                GeneratedAt = DateTimeOffset.Now.ToString("O"),
                Entries = new List<MovieClipScatterEntry>()
            };

            var dedup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var movieClipSharedDedup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var atlasCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var bytesPath in bytesFiles)
                {
                    ProcessPackage(
                        bytesPath,
                        options.OutputDir,
                        manifest.Entries,
                        movieClipManifest.Entries,
                        dedup,
                        movieClipSharedDedup,
                        atlasCache);
                }
            }
            finally
            {
                foreach (var bmp in atlasCache.Values)
                {
                    bmp.Dispose();
                }
            }

            var manifestDir = Path.GetDirectoryName(options.ManifestPath);
            if (!string.IsNullOrWhiteSpace(manifestDir))
            {
                Directory.CreateDirectory(manifestDir);
            }
            var movieClipManifestDir = Path.GetDirectoryName(options.MovieClipManifestPath);
            if (!string.IsNullOrWhiteSpace(movieClipManifestDir))
            {
                Directory.CreateDirectory(movieClipManifestDir);
            }

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(options.ManifestPath, json, Encoding.UTF8);
            var movieClipJson = JsonSerializer.Serialize(movieClipManifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(options.MovieClipManifestPath, movieClipJson, Encoding.UTF8);

            Console.WriteLine(
                $"[PASS] exported scatter entries={manifest.Entries.Count}, movieclip entries={movieClipManifest.Entries.Count}, manifest={options.ManifestPath}, movieclipManifest={options.MovieClipManifestPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FAIL] {ex}");
            return 1;
        }
    }

    private static void ProcessPackage(
        string bytesPath,
        string outputDir,
        List<ScatterEntry> manifestEntries,
        List<MovieClipScatterEntry> movieClipEntries,
        Dictionary<string, string> dedup,
        Dictionary<string, string> movieClipSharedDedup,
        Dictionary<string, Bitmap> atlasCache)
    {
        var bytes = File.ReadAllBytes(bytesPath);
        var packagePrefix = GetPackagePrefix(bytesPath);
        var parsed = PackageParser.Parse(bytes, packagePrefix);

        if (string.IsNullOrWhiteSpace(parsed.PackageId))
        {
            Console.WriteLine($"[WARN] skip package (missing id): {bytesPath}");
            return;
        }

        var packageName = string.IsNullOrWhiteSpace(parsed.PackageName)
            ? Path.GetFileNameWithoutExtension(packagePrefix)
            : parsed.PackageName;

        var packageOutDir = Path.Combine(outputDir, SanitizeSegment(packageName));
        Directory.CreateDirectory(packageOutDir);

        var bytesDir = Path.GetDirectoryName(bytesPath) ?? string.Empty;
        var packageCount = 0;
        var packageToken = SanitizeSegment(packageName);

        foreach (var image in parsed.Images.Values)
        {
            if (parsed.Sprites.TryGetValue(image.Id, out var sprite))
            {
                if (!parsed.Atlases.TryGetValue(sprite.AtlasId, out var atlas))
                {
                    Console.WriteLine($"[WARN] skip image(no atlas) package={packageName} item={image.Id}");
                    continue;
                }

                var atlasPath = ResolveAtlasPath(bytesDir, atlas.File);
                if (atlasPath == null)
                {
                    Console.WriteLine($"[WARN] skip image(missing atlas file) package={packageName} item={image.Id} atlas={atlas.File}");
                    continue;
                }

                var dedupKey = $"atlas|{atlasPath}|{sprite.X}|{sprite.Y}|{sprite.W}|{sprite.H}|{sprite.Rotated}";
                string relativeImagePath;
                if (!dedup.TryGetValue(dedupKey, out relativeImagePath!))
                {
                    var fileName = $"{image.Id}__{SanitizeSegment(image.Name)}.png";
                    var absoluteOut = Path.Combine(packageOutDir, fileName);
                    var bitmap = GetOrLoadAtlas(atlasPath, atlasCache);
                    ExportSprite(bitmap, sprite, absoluteOut);

                    relativeImagePath = $"image/fgui/scatter/{packageToken}/{fileName}";
                    dedup[dedupKey] = relativeImagePath;
                }

                manifestEntries.Add(new ScatterEntry
                {
                    PackageId = parsed.PackageId,
                    PackageName = packageName,
                    ItemId = image.Id,
                    ItemName = image.Name,
                    ImagePath = relativeImagePath,
                    Width = image.Width,
                    Height = image.Height
                });
                packageCount++;
                continue;
            }

            var standalonePath = ResolveStandaloneImagePath(bytesDir, packageName, image);
            if (standalonePath == null)
            {
                Console.WriteLine($"[WARN] skip image(no sprite/source) package={packageName} item={image.Id} name={image.Name}");
                continue;
            }

            var standaloneDedupKey = $"standalone|{standalonePath}";
            string standaloneRelativePath;
            if (!dedup.TryGetValue(standaloneDedupKey, out standaloneRelativePath!))
            {
                var fileName = $"{image.Id}__{SanitizeSegment(image.Name)}.png";
                var absoluteOut = Path.Combine(packageOutDir, fileName);
                ExportStandaloneImage(standalonePath, absoluteOut);
                standaloneRelativePath = $"image/fgui/scatter/{packageToken}/{fileName}";
                dedup[standaloneDedupKey] = standaloneRelativePath;
            }

            manifestEntries.Add(new ScatterEntry
            {
                PackageId = parsed.PackageId,
                PackageName = packageName,
                ItemId = image.Id,
                ItemName = image.Name,
                ImagePath = standaloneRelativePath,
                Width = image.Width,
                Height = image.Height
            });
            packageCount++;
        }

        var movieClipCount = 0;
        var movieClipFrameCount = 0;
        foreach (var clip in parsed.MovieClips.Values)
        {
            var clipToken = SanitizeSegment(Path.GetFileNameWithoutExtension(clip.Name));
            if (string.IsNullOrWhiteSpace(clipToken) || clipToken == "unnamed")
            {
                clipToken = clip.Id;
            }

            var logicalClipDir = $"image/fgui/scatter/{packageToken}/{clipToken}";
            Directory.CreateDirectory(Path.Combine(outputDir, packageToken, clipToken));
            movieClipCount++;

            for (var frameIndex = 0; frameIndex < clip.Frames.Count; frameIndex++)
            {
                var frame = clip.Frames[frameIndex];
                var spriteId = frame.SpriteId;
                if (string.IsNullOrWhiteSpace(spriteId))
                {
                    throw new InvalidOperationException(
                        $"movieclip frame missing spriteId package={packageName} clip={clip.Name} frame={frameIndex}");
                }

                if (!parsed.Sprites.TryGetValue(spriteId, out var sprite))
                {
                    throw new InvalidOperationException(
                        $"movieclip frame sprite missing in sprite table package={packageName} clip={clip.Name} frame={frameIndex} spriteId={spriteId}");
                }

                if (!parsed.Atlases.TryGetValue(sprite.AtlasId, out var atlas))
                {
                    throw new InvalidOperationException(
                        $"movieclip frame atlas missing package={packageName} clip={clip.Name} frame={frameIndex} spriteId={spriteId} atlasId={sprite.AtlasId}");
                }

                var atlasPath = ResolveAtlasPath(bytesDir, atlas.File);
                if (atlasPath == null)
                {
                    throw new InvalidOperationException(
                        $"movieclip frame atlas file missing package={packageName} clip={clip.Name} frame={frameIndex} spriteId={spriteId} atlas={atlas.File}");
                }

                var dedupKey = $"atlas|{atlasPath}|{sprite.X}|{sprite.Y}|{sprite.W}|{sprite.H}|{sprite.Rotated}";
                if (!movieClipSharedDedup.TryGetValue(dedupKey, out var sharedImagePath))
                {
                    var hash = ComputeDeterministicHash(dedupKey);
                    var sharedFileName = $"{hash}.png";
                    var sharedRelativePath = $"image/fgui/scatter/_movieclip_shared/{sharedFileName}";
                    var sharedAbsolutePath = Path.Combine(outputDir, "_movieclip_shared", sharedFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(sharedAbsolutePath)!);

                    var bitmap = GetOrLoadAtlas(atlasPath, atlasCache);
                    ExportSprite(bitmap, sprite, sharedAbsolutePath);
                    movieClipSharedDedup[dedupKey] = sharedRelativePath;
                    sharedImagePath = sharedRelativePath;
                }

                movieClipEntries.Add(new MovieClipScatterEntry
                {
                    PackageId = parsed.PackageId,
                    PackageName = packageName,
                    ClipItemId = clip.Id,
                    ClipName = clip.Name,
                    FrameIndex = frameIndex,
                    SpriteItemId = spriteId,
                    LogicalPath = $"{logicalClipDir}/frame_{frameIndex:D4}.png",
                    ImagePath = sharedImagePath
                });
                movieClipFrameCount++;
            }
        }

        Console.WriteLine(
            $"[INFO] package={packageName} images={packageCount} movieclips={movieClipCount} movieclipFrames={movieClipFrameCount}");
    }

    private static Bitmap GetOrLoadAtlas(string atlasPath, Dictionary<string, Bitmap> atlasCache)
    {
        if (atlasCache.TryGetValue(atlasPath, out var cached))
        {
            return cached;
        }

        var bmp = new Bitmap(atlasPath);
        atlasCache[atlasPath] = bmp;
        return bmp;
    }

    private static void ExportSprite(Bitmap atlas, SpriteRecord sprite, string outputFile)
    {
        var rect = Rectangle.FromLTRB(sprite.X, sprite.Y, sprite.X + sprite.W, sprite.Y + sprite.H);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            throw new InvalidOperationException($"invalid sprite rect: {rect}");
        }

        if (rect.Right > atlas.Width || rect.Bottom > atlas.Height)
        {
            throw new InvalidOperationException($"sprite rect out of atlas bounds: rect={rect}, atlas={atlas.Width}x{atlas.Height}");
        }

        using var cropped = atlas.Clone(rect, atlas.PixelFormat);
        if (sprite.Rotated)
        {
            cropped.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        cropped.Save(outputFile, ImageFormat.Png);
    }

    private static void ExportStandaloneImage(string sourcePath, string outputFile)
    {
        using var source = new Bitmap(sourcePath);
        source.Save(outputFile, ImageFormat.Png);
    }

    private static string? ResolveAtlasPath(string bytesDir, string atlasFile)
    {
        if (string.IsNullOrWhiteSpace(atlasFile))
        {
            return null;
        }

        var normalized = atlasFile.Replace('\\', '/').TrimStart('/');
        var candidates = new List<string>(8);

        if (Path.IsPathRooted(atlasFile))
        {
            candidates.Add(atlasFile);
        }

        candidates.Add(Path.Combine(bytesDir, Path.GetFileName(normalized)));
        candidates.Add(Path.Combine(bytesDir, normalized.Replace('/', Path.DirectorySeparatorChar)));
        candidates.Add(Path.Combine(bytesDir, "..", normalized.Replace('/', Path.DirectorySeparatorChar)));

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    private static string? ResolveStandaloneImagePath(string bytesDir, string packageName, ImageItem image)
    {
        var candidates = new List<string>();
        var inputFile = NormalizeLoosePath(image.File);
        var fileName = Path.GetFileName(inputFile);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = Path.GetFileName(NormalizeLoosePath(image.Name));
        }

        if (!string.IsNullOrWhiteSpace(inputFile))
        {
            if (Path.IsPathRooted(inputFile))
            {
                candidates.Add(inputFile);
            }

            candidates.Add(Path.Combine(bytesDir, inputFile.Replace('/', Path.DirectorySeparatorChar)));
            candidates.Add(Path.Combine(bytesDir, fileName));
            candidates.Add(Path.Combine(bytesDir, "..", inputFile.Replace('/', Path.DirectorySeparatorChar)));
            candidates.Add(Path.Combine(bytesDir, "..", fileName));
        }
        else if (!string.IsNullOrWhiteSpace(fileName))
        {
            candidates.Add(Path.Combine(bytesDir, fileName));
            candidates.Add(Path.Combine(bytesDir, "..", fileName));
        }

        var projectRoot = FindProjectRoot(bytesDir);
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            var uiProjectAssets = Path.Combine(projectRoot, "UIProject", "assets", packageName);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                candidates.Add(Path.Combine(uiProjectAssets, "images", fileName));
                candidates.Add(Path.Combine(uiProjectAssets, fileName));
            }

            if (!string.IsNullOrWhiteSpace(inputFile))
            {
                candidates.Add(Path.Combine(uiProjectAssets, inputFile.Replace('/', Path.DirectorySeparatorChar)));
            }

            var runtimeUiImage = Path.Combine(projectRoot, "rpg_3d_2604140", "ui", "image");
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                candidates.Add(Path.Combine(runtimeUiImage, fileName));
                candidates.Add(Path.Combine(runtimeUiImage, packageName, fileName));
            }
        }

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch
            {
                // ignore malformed candidate and continue probing
            }
        }

        return null;
    }

    private static string NormalizeLoosePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string? FindProjectRoot(string startDir)
    {
        var current = new DirectoryInfo(startDir);
        while (current != null)
        {
            var probe = Path.Combine(current.FullName, "UIProject", "assets");
            if (Directory.Exists(probe))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string GetPackagePrefix(string bytesPath)
    {
        var name = Path.GetFileNameWithoutExtension(bytesPath);
        const string suffix = "_fui";
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^suffix.Length];
        }

        var dir = Path.GetDirectoryName(bytesPath) ?? string.Empty;
        return Path.Combine(dir, name);
    }

    private static string SanitizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unnamed";
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append('_');
            }
        }

        var result = sb.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "unnamed" : result;
    }

    private static string ComputeDeterministicHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}

internal sealed class CliOptions
{
    public required string InputDir { get; init; }
    public required string OutputDir { get; init; }
    public required string ManifestPath { get; init; }
    public required string MovieClipManifestPath { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            var value = i + 1 < args.Length ? args[i + 1] : string.Empty;
            if (!value.StartsWith("--", StringComparison.Ordinal))
            {
                map[key] = value;
                i++;
            }
        }

        var inputDir = map.TryGetValue("input-dir", out var input) && !string.IsNullOrWhiteSpace(input)
            ? Path.GetFullPath(input)
            : throw new ArgumentException("--input-dir is required");
        var outputDir = map.TryGetValue("output-dir", out var output) && !string.IsNullOrWhiteSpace(output)
            ? Path.GetFullPath(output)
            : throw new ArgumentException("--output-dir is required");
        var manifest = map.TryGetValue("manifest", out var manifestPath) && !string.IsNullOrWhiteSpace(manifestPath)
            ? Path.GetFullPath(manifestPath)
            : throw new ArgumentException("--manifest is required");
        var movieClipManifest = map.TryGetValue("movieclip-manifest", out var movieClipManifestPath) && !string.IsNullOrWhiteSpace(movieClipManifestPath)
            ? Path.GetFullPath(movieClipManifestPath)
            : throw new ArgumentException("--movieclip-manifest is required");

        return new CliOptions
        {
            InputDir = inputDir,
            OutputDir = outputDir,
            ManifestPath = manifest,
            MovieClipManifestPath = movieClipManifest
        };
    }
}

internal sealed class ScatterManifest
{
    public int Version { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
    public List<ScatterEntry> Entries { get; set; } = new();
}

internal sealed class ScatterEntry
{
    public string PackageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

internal sealed class MovieClipScatterManifest
{
    public int Version { get; set; }
    public string GeneratedAt { get; set; } = string.Empty;
    public List<MovieClipScatterEntry> Entries { get; set; } = new();
}

internal sealed class MovieClipScatterEntry
{
    public string PackageId { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string ClipItemId { get; set; } = string.Empty;
    public string ClipName { get; set; } = string.Empty;
    public int FrameIndex { get; set; }
    public string SpriteItemId { get; set; } = string.Empty;
    public string LogicalPath { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

internal static class PackageParser
{
    public static ParsedPackage Parse(byte[] bytes, string packagePrefix)
    {
        var buffer = new ByteReader(bytes);
        if (buffer.ReadUInt() != FguiMagicConst.Value)
        {
            throw new InvalidOperationException("not a valid fgui package");
        }

        var version = buffer.ReadInt();
        buffer.Version = version;
        buffer.ReadBool();
        var packageId = buffer.ReadString();
        var packageName = buffer.ReadString();

        buffer.Skip(20);
        var indexTablePos = buffer.Position;

        buffer.Seek(indexTablePos, 4);
        var strCount = buffer.ReadInt();
        var stringTable = new string[strCount];
        for (var i = 0; i < strCount; i++)
        {
            stringTable[i] = buffer.ReadString();
        }

        buffer.StringTable = stringTable;
        if (buffer.Seek(indexTablePos, 5))
        {
            var cnt = buffer.ReadInt();
            for (var i = 0; i < cnt; i++)
            {
                var index = buffer.ReadUShort();
                var len = buffer.ReadInt();
                stringTable[index] = buffer.ReadString(len);
            }
        }

        var assetNamePrefix = Path.GetFileName(packagePrefix) + "_";

        buffer.Seek(indexTablePos, 1);
        var itemCount = buffer.ReadShort();
        var images = new Dictionary<string, ImageItem>(StringComparer.Ordinal);
        var atlases = new Dictionary<string, AtlasItem>(StringComparer.Ordinal);
        var movieClips = new Dictionary<string, MovieClipItem>(StringComparer.Ordinal);

        for (var i = 0; i < itemCount; i++)
        {
            var nextPos = buffer.ReadInt() + buffer.Position;
            var type = (PackageItemType)buffer.ReadByte();
            var id = buffer.ReadS() ?? string.Empty;
            var name = buffer.ReadS() ?? string.Empty;
            buffer.ReadS();
            var file = buffer.ReadS() ?? string.Empty;
            buffer.ReadBool();
            var width = buffer.ReadInt();
            var height = buffer.ReadInt();

            switch (type)
            {
                case PackageItemType.Image:
                    images[id] = new ImageItem(id, name, width, height, file);
                    break;
                case PackageItemType.Atlas:
                    atlases[id] = new AtlasItem(id, assetNamePrefix + file);
                    break;
                case PackageItemType.MovieClip:
                {
                    buffer.ReadBool();
                    var rawDataLength = buffer.ReadInt();
                    if (rawDataLength < 0)
                    {
                        throw new InvalidOperationException($"invalid movieclip rawData length package={packageName} clip={name} len={rawDataLength}");
                    }

                    var rawData = buffer.ReadBytes(rawDataLength);
                    var frames = ParseMovieClipFrames(rawData, stringTable, version);
                    movieClips[id] = new MovieClipItem(id, name, width, height, frames);
                    break;
                }
            }

            buffer.Position = nextPos;
        }

        buffer.Seek(indexTablePos, 2);
        var spriteCount = buffer.ReadShort();
        var sprites = new Dictionary<string, SpriteRecord>(StringComparer.Ordinal);
        for (var i = 0; i < spriteCount; i++)
        {
            var nextPos = buffer.ReadUShort() + buffer.Position;
            var itemId = buffer.ReadS() ?? string.Empty;
            var atlasId = buffer.ReadS() ?? string.Empty;
            var x = buffer.ReadInt();
            var y = buffer.ReadInt();
            var w = buffer.ReadInt();
            var h = buffer.ReadInt();
            var rotated = buffer.ReadBool();
            if (version >= 2 && buffer.ReadBool())
            {
                buffer.ReadInt();
                buffer.ReadInt();
                buffer.ReadInt();
                buffer.ReadInt();
            }

            sprites[itemId] = new SpriteRecord(itemId, atlasId, x, y, w, h, rotated);
            buffer.Position = nextPos;
        }

        return new ParsedPackage(packageId, packageName, images, atlases, sprites, movieClips);
    }

    private static List<MovieClipFrame> ParseMovieClipFrames(byte[] rawData, string[] stringTable, int version)
    {
        var buffer = new ByteReader(rawData)
        {
            StringTable = stringTable,
            Version = version
        };

        buffer.Seek(0, 0);
        buffer.ReadInt(); // interval(ms)
        buffer.ReadBool(); // swing
        buffer.ReadInt(); // repeat delay(ms)
        if (!buffer.Seek(0, 1))
        {
            return [];
        }

        var frameCount = buffer.ReadShort();
        var frames = new List<MovieClipFrame>(Math.Max(0, (int)frameCount));
        for (var i = 0; i < frameCount; i++)
        {
            var nextPos = buffer.ReadUShort() + buffer.Position;
            buffer.ReadInt(); // x
            buffer.ReadInt(); // y
            buffer.ReadInt(); // width
            buffer.ReadInt(); // height
            buffer.ReadInt(); // addDelay(ms)
            var spriteId = buffer.ReadS() ?? string.Empty;
            frames.Add(new MovieClipFrame(i, spriteId));
            buffer.Position = nextPos;
        }

        return frames;
    }

    private static class FguiMagicConst
    {
        public const uint Value = 0x46475549;
    }
}

internal sealed record ParsedPackage(
    string PackageId,
    string PackageName,
    Dictionary<string, ImageItem> Images,
    Dictionary<string, AtlasItem> Atlases,
    Dictionary<string, SpriteRecord> Sprites,
    Dictionary<string, MovieClipItem> MovieClips);

internal sealed record ImageItem(string Id, string Name, int Width, int Height, string File);
internal sealed record AtlasItem(string Id, string File);
internal sealed record SpriteRecord(string ItemId, string AtlasId, int X, int Y, int W, int H, bool Rotated);
internal sealed record MovieClipItem(string Id, string Name, int Width, int Height, List<MovieClipFrame> Frames);
internal sealed record MovieClipFrame(int FrameIndex, string SpriteId);

internal enum PackageItemType
{
    Image,
    MovieClip,
    Sound,
    Component,
    Atlas,
    Font,
    Swf,
    Misc,
    Unknown,
    Spine,
    DragonBones
}

internal sealed class ByteReader
{
    private readonly byte[] _data;
    private readonly int _length;
    private int _pointer;

    public int Version { get; set; }
    public string[]? StringTable { get; set; }
    public bool LittleEndian { get; set; }

    public ByteReader(byte[] data)
    {
        _data = data;
        _length = data.Length;
        _pointer = 0;
        LittleEndian = false;
    }

    public int Position
    {
        get => _pointer;
        set => _pointer = value;
    }

    public void Skip(int count) => _pointer += count;

    public byte[] ReadBytes(int count)
    {
        if (count < 0 || _pointer + count > _length)
        {
            throw new IndexOutOfRangeException($"ReadBytes out of range: position={_pointer}, count={count}, length={_length}");
        }

        var bytes = new byte[count];
        Buffer.BlockCopy(_data, _pointer, bytes, 0, count);
        _pointer += count;
        return bytes;
    }

    public byte ReadByte() => _data[_pointer++];

    public bool ReadBool()
    {
        var result = _data[_pointer] == 1;
        _pointer++;
        return result;
    }

    public short ReadShort()
    {
        var start = _pointer;
        _pointer += 2;
        if (LittleEndian)
        {
            return (short)(_data[start] | (_data[start + 1] << 8));
        }

        return (short)((_data[start] << 8) | _data[start + 1]);
    }

    public ushort ReadUShort() => (ushort)ReadShort();

    public int ReadInt()
    {
        var start = _pointer;
        _pointer += 4;
        if (LittleEndian)
        {
            return _data[start] | (_data[start + 1] << 8) | (_data[start + 2] << 16) | (_data[start + 3] << 24);
        }

        return (_data[start] << 24) | (_data[start + 1] << 16) | (_data[start + 2] << 8) | _data[start + 3];
    }

    public uint ReadUInt() => unchecked((uint)ReadInt());

    public string ReadString()
    {
        var len = ReadUShort();
        var result = Encoding.UTF8.GetString(_data, _pointer, len);
        _pointer += len;
        return result;
    }

    public string ReadString(int len)
    {
        var result = Encoding.UTF8.GetString(_data, _pointer, len);
        _pointer += len;
        return result;
    }

    public string? ReadS()
    {
        var index = ReadUShort();
        if (index == 65534)
        {
            return null;
        }

        if (index == 65533)
        {
            return string.Empty;
        }

        if (StringTable == null || index >= StringTable.Length)
        {
            throw new IndexOutOfRangeException($"ReadS: StringTable index={index}, len={StringTable?.Length ?? 0}");
        }

        return StringTable[index];
    }

    public bool Seek(int indexTablePos, int blockIndex)
    {
        var tmp = _pointer;
        _pointer = indexTablePos;

        var segCount = _data[_pointer++];
        if (blockIndex < segCount)
        {
            var useShort = _data[_pointer++] == 1;
            int newPos;
            if (useShort)
            {
                _pointer += 2 * blockIndex;
                newPos = ReadShort();
            }
            else
            {
                _pointer += 4 * blockIndex;
                newPos = ReadInt();
            }

            if (newPos > 0)
            {
                _pointer = indexTablePos + newPos;
                return true;
            }

            _pointer = tmp;
            return false;
        }

        _pointer = tmp;
        return false;
    }
}
