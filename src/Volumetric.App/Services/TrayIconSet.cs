using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Volumetric_App.Services;

public sealed partial class TrayIconSet : IDisposable
{
    private readonly Icon _idleIcon;
    private readonly Icon _scanningIcon;
    private readonly IntPtr _scanningIconHandle;

    public TrayIconSet(Icon idleIcon)
    {
        _idleIcon = idleIcon;
        (_scanningIcon, _scanningIconHandle) = CreateScanningIcon(idleIcon);
    }

    public Icon CreateIcon(bool isScanning) => (Icon)(isScanning ? _scanningIcon : _idleIcon).Clone();

    public void Dispose()
    {
        _idleIcon.Dispose();
        _scanningIcon.Dispose();
        DestroyIcon(_scanningIconHandle);
    }

    private static (Icon Icon, IntPtr Handle) CreateScanningIcon(Icon baseIcon)
    {
        using var bitmap = baseIcon.ToBitmap();
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var badgeSize = bitmap.Width / 2;
        var badgeRect = new Rectangle(bitmap.Width - badgeSize, bitmap.Height - badgeSize, badgeSize, badgeSize);

        using var badgeBrush = new SolidBrush(Color.FromArgb(255, 16, 124, 16));
        graphics.FillEllipse(badgeBrush, badgeRect);
        using var borderPen = new Pen(Color.White, 1.5f);
        graphics.DrawEllipse(borderPen, badgeRect);

        var handle = bitmap.GetHicon();
        return (Icon.FromHandle(handle), handle);
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);
}
