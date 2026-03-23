using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using DrawingIcon = System.Drawing.Icon;
using WpfApplication = System.Windows.Application;
using WpfImageSource = System.Windows.Media.ImageSource;

namespace ComfortScreen.Infrastructure;

internal static class AppIconProvider
{
    private static readonly Uri IcoUri = new("pack://application:,,,/Assets/Brand/FlatCandidates/flat-a.ico", UriKind.Absolute);
    private static readonly Uri PngUri = new("pack://application:,,,/Assets/Brand/FlatCandidates/flat-a.png", UriKind.Absolute);
    private static readonly Lazy<WpfImageSource> WindowIconSource = new(CreateWindowIconSource);
    private static readonly Lazy<WpfImageSource> TitleBarIconSource = new(CreateTitleBarIconSource);

    public static WpfImageSource GetWindowIconSource() => WindowIconSource.Value;

    public static WpfImageSource GetTitleBarIconSource() => TitleBarIconSource.Value;

    public static ImageIcon CreateTitleBarIconElement()
    {
        return new ImageIcon
        {
            Source = GetTitleBarIconSource()
        };
    }

    public static DrawingIcon CreateTrayIcon()
    {
        using var stream = OpenResourceStream(IcoUri);
        using var icon = new DrawingIcon(stream);
        return (DrawingIcon)icon.Clone();
    }

    private static WpfImageSource CreateWindowIconSource()
    {
        using var stream = OpenResourceStream(IcoUri);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        frame.Freeze();
        return frame;
    }

    private static WpfImageSource CreateTitleBarIconSource()
    {
        using var stream = OpenResourceStream(PngUri);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    private static Stream OpenResourceStream(Uri resourceUri)
    {
        var resource = WpfApplication.GetResourceStream(resourceUri);
        if (resource?.Stream is null)
        {
            throw new InvalidOperationException($"Unable to load icon resource '{resourceUri}'.");
        }

        var buffer = new MemoryStream();
        resource.Stream.CopyTo(buffer);
        buffer.Position = 0;
        return buffer;
    }
}
