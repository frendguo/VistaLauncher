using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace VistaLauncher.Services;

/// <summary>
/// 图标提取服务 - 从可执行文件中提取图标
/// </summary>
public static class IconExtractor
{
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_SMALLICON = 0x1;

    /// <summary>
    /// 图标缓存，使用 ConcurrentDictionary 确保线程安全
    /// Key 格式: normalizedPath|lastWriteTimeTicks|useLargeIcon
    /// </summary>
    private static readonly ConcurrentDictionary<string, BitmapImage?> _cache = new();

    /// <summary>
    /// 信号量，限制并发图标提取数量
    /// </summary>
    private static readonly SemaphoreSlim _semaphore = new(4, 4);  // 最多同时处理 4 个图标

    /// <summary>
    /// 最大缓存条目数
    /// </summary>
    private const int MaxCacheSize = 2000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// 从文件路径提取图标（同步版本，不使用缓存）
    /// </summary>
    /// <param name="filePath">可执行文件路径</param>
    /// <param name="useLargeIcon">是否使用大图标</param>
    /// <returns>BitmapImage 或 null</returns>
    public static BitmapImage? ExtractIcon(string filePath, bool useLargeIcon = true)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var shfi = new SHFILEINFO();
            var flags = SHGFI_ICON | (useLargeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON);

            var result = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                using var icon = Icon.FromHandle(shfi.hIcon);
                using var bitmap = icon.ToBitmap();

                return ConvertToBitmapImageSync(bitmap);
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 异步提取图标（带缓存）
    /// </summary>
    public static async Task<BitmapImage?> ExtractIconAsync(string filePath, bool useLargeIcon = true)
    {
        // 尝试获取缓存 key，如果文件不存在会返回 null
        var key = GetCacheKey(filePath, useLargeIcon);
        if (key is null)
        {
            return null;
        }

        // 检查缓存
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // 限制并发数量，避免同时加载过多图标
        await _semaphore.WaitAsync();
        try
        {
            // 再次检查缓存（可能在等待时已被其他请求加载）
            if (_cache.TryGetValue(key, out var cached2))
            {
                return cached2;
            }

            // 从文件系统提取图标（在后台线程执行）
            var icon = await Task.Run(() => ExtractIcon(filePath, useLargeIcon));

            // 简单的容量控制：达到上限时清空缓存
            if (_cache.Count >= MaxCacheSize)
            {
                _cache.Clear();
            }

            // 添加到缓存
            _cache.TryAdd(key, icon);
            return icon;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 将 System.Drawing.Bitmap 转换为 BitmapImage（同步版本）
    /// </summary>
    private static BitmapImage? ConvertToBitmapImageSync(Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();

            // 使用 RandomAccessStream
            // 注意：SetSourceAsync 必须在 UI 线程或具有 CoreDispatcher 的线程上调用
            // 这里使用 GetAwaiter().GetResult() 替代 .Wait() 以避免 AggregateException 包装
            var randomAccessStream = memoryStream.AsRandomAccessStream();
            bitmapImage.SetSourceAsync(randomAccessStream).AsTask().GetAwaiter().GetResult();

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 System.Drawing.Bitmap 转换为 BitmapImage（异步版本）
    /// </summary>
    private static async Task<BitmapImage?> ConvertToBitmapImageAsync(Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();

            // 使用 RandomAccessStream，异步设置图片源
            var randomAccessStream = memoryStream.AsRandomAccessStream();
            await bitmapImage.SetSourceAsync(randomAccessStream);

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 生成缓存 Key
    /// 格式: normalizedPath|lastWriteTimeTicks|useLargeIcon
    /// </summary>
    private static string? GetCacheKey(string filePath, bool useLargeIcon)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            var normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
            if (!File.Exists(normalizedPath))
            {
                return null;
            }

            var lastWrite = File.GetLastWriteTimeUtc(normalizedPath).Ticks;
            return $"{normalizedPath}|{lastWrite}|{useLargeIcon}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 清空图标缓存
    /// </summary>
    public static void ClearCache() => _cache.Clear();

    /// <summary>
    /// 获取当前缓存条目数
    /// </summary>
    public static int CacheCount => _cache.Count;
}
