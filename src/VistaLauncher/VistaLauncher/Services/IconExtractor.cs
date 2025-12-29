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
    /// 从文件路径提取图标
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
                
                return ConvertToBitmapImage(bitmap);
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
    /// 将 System.Drawing.Bitmap 转换为 BitmapImage
    /// </summary>
    private static BitmapImage? ConvertToBitmapImage(Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            
            // 使用 RandomAccessStream
            var randomAccessStream = memoryStream.AsRandomAccessStream();
            bitmapImage.SetSourceAsync(randomAccessStream).AsTask().Wait();
            
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 异步提取图标
    /// </summary>
    public static async Task<BitmapImage?> ExtractIconAsync(string filePath, bool useLargeIcon = true)
    {
        return await Task.Run(() => ExtractIcon(filePath, useLargeIcon));
    }
}
