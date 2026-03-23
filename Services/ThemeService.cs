using System.Windows;
using System.Windows.Media;
using ComfortScreen.Contracts;
using ComfortScreen.Models;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MediaColor = System.Windows.Media.Color;

namespace ComfortScreen.Services;

public sealed class ThemeService : IThemeService
{
    public void ApplyTheme(AppThemeMode themeMode, Window window)
    {
        var palette = themeMode == AppThemeMode.Dark ? ThemePalette.Dark : ThemePalette.Light;
        var applicationTheme = themeMode == AppThemeMode.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        ApplicationThemeManager.Apply(applicationTheme, WindowBackdropType.Mica, updateAccent: false);
        ApplicationAccentColorManager.Apply(palette.AccentColor, applicationTheme, false, false);

        ApplyPalette(palette);

        // Force the shell to redraw after both the WPF-UI theme and our palette are in sync.
        window.InvalidateVisual();
        window.UpdateLayout();
    }

    private static void ApplyPalette(ThemePalette palette)
    {
        UpdateBrushColor("ComfortScreenShellBackgroundBrush", palette.ShellBackgroundColor);
        UpdateBrushColor("ComfortScreenPaneBackgroundBrush", palette.PaneBackgroundColor);
        UpdateBrushColor("ComfortScreenHeroBrush", palette.HeroColor);
        UpdateBrushColor("ComfortScreenAccentBrush", palette.AccentColor);
        UpdateBrushColor("ComfortScreenPrimaryTextBrush", palette.PrimaryTextColor);
        UpdateBrushColor("ComfortScreenSecondaryTextBrush", palette.SecondaryTextColor);
        UpdateBrushColor("ComfortScreenSurfaceBrush", palette.SurfaceColor);
        UpdateBrushColor("ComfortScreenSurfaceBorderBrush", palette.SurfaceBorderColor);
        UpdateBrushColor("ComfortScreenNavigationHostBrush", palette.NavigationHostColor);
        UpdateBrushColor("ComfortScreenNavigationHostBorderBrush", palette.NavigationHostBorderColor);
        UpdateBrushColor("ComfortScreenDialogBackgroundBrush", palette.DialogBackgroundColor);
        UpdateBrushColor("ComfortScreenDialogSurfaceBrush", palette.DialogSurfaceColor);
        UpdateBrushColor("ComfortScreenDialogTitleBackgroundBrush", palette.DialogTitleBackgroundColor);
        UpdateBrushColor("ComfortScreenDialogTitleBorderBrush", palette.DialogTitleBorderColor);
        UpdateBrushColor("ComfortScreenDialogTitleTextBrush", palette.DialogTitleTextColor);
        UpdateBrushColor("ComfortScreenDialogFooterBackgroundBrush", palette.DialogFooterBackgroundColor);
        UpdateBrushColor("ComfortScreenDialogIconBackgroundBrush", palette.DialogIconBackgroundColor);
        UpdateBrushColor("ComfortScreenDialogIconForegroundBrush", palette.DialogIconForegroundColor);
        UpdateBrushColor("ComfortScreenDialogCloseButtonHoverBrush", palette.DialogCloseButtonHoverColor);
        UpdateBrushColor("ComfortScreenDialogPrimaryTextBrush", palette.DialogPrimaryTextColor);
        UpdateBrushColor("ComfortScreenDialogSecondaryTextBrush", palette.DialogSecondaryTextColor);
        UpdateBrushColor("ComfortScreenDialogAccentBrush", palette.AccentColor);
        UpdateBrushColor("ComfortScreenOverlayPrimaryTextBrush", palette.OverlayPrimaryTextColor);
        UpdateBrushColor("ComfortScreenOverlayAccentBrush", palette.OverlayAccentColor);
        UpdateBrushColor("ComfortScreenOverlayButtonBackgroundBrush", palette.OverlayButtonBackgroundColor);
        UpdateBrushColor("ComfortScreenOverlayButtonForegroundBrush", palette.OverlayButtonForegroundColor);
        UpdateBrushColor("ComfortScreenInputBackgroundBrush", palette.InputBackgroundColor);
        UpdateBrushColor("ComfortScreenInputHoverBackgroundBrush", palette.InputHoverBackgroundColor);
        UpdateBrushColor("ComfortScreenInputBorderBrush", palette.InputBorderColor);
        UpdateBrushColor("ComfortScreenInputFocusBorderBrush", palette.InputFocusBorderColor);
        UpdateBrushColor("ComfortScreenInputForegroundBrush", palette.InputForegroundColor);
        UpdateBrushColor("ComfortScreenInputPlaceholderBrush", palette.InputPlaceholderColor);
        UpdateBrushColor("ComfortScreenInputSelectionBrush", palette.InputSelectionColor);
        UpdateBrushColor("ComfortScreenInputSelectionForegroundBrush", palette.InputSelectionForegroundColor);
        UpdateBrushColor("ComfortScreenInputDisabledBackgroundBrush", palette.InputDisabledBackgroundColor);
        UpdateBrushColor("ComfortScreenInputDisabledForegroundBrush", palette.InputDisabledForegroundColor);
        UpdateBrushColor("ComfortScreenDropDownBackgroundBrush", palette.DropDownBackgroundColor);
        UpdateBrushColor("ComfortScreenDropDownBorderBrush", palette.DropDownBorderColor);
        UpdateBrushColor("ComfortScreenDropDownItemForegroundBrush", palette.DropDownItemForegroundColor);
        UpdateBrushColor("ComfortScreenDropDownItemHoverBrush", palette.DropDownItemHoverColor);
        UpdateBrushColor("ComfortScreenDropDownItemSelectedBrush", palette.DropDownItemSelectedColor);
        UpdateBrushColor("ComfortScreenCheckBoxForegroundBrush", palette.CheckBoxForegroundColor);
        UpdateBrushColor("ComfortScreenCheckBoxBorderBrush", palette.CheckBoxBorderColor);
        UpdateBrushColor("ComfortScreenCheckBoxBackgroundBrush", palette.CheckBoxBackgroundColor);
        UpdateBrushColor("ComfortScreenCheckBoxHoverBackgroundBrush", palette.CheckBoxHoverBackgroundColor);
        UpdateBrushColor("ComfortScreenCheckBoxCheckedBackgroundBrush", palette.CheckBoxCheckedBackgroundColor);
        UpdateBrushColor("ComfortScreenCheckBoxCheckedBorderBrush", palette.CheckBoxCheckedBorderColor);
        UpdateBrushColor("ComfortScreenCheckBoxGlyphBrush", palette.CheckBoxGlyphColor);
        UpdateBrushColor("ComfortScreenButtonPrimaryBackgroundBrush", palette.ButtonPrimaryBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonPrimaryForegroundBrush", palette.ButtonPrimaryForegroundColor);
        UpdateBrushColor("ComfortScreenButtonPrimaryHoverBackgroundBrush", palette.ButtonPrimaryHoverBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonPrimaryPressedBackgroundBrush", palette.ButtonPrimaryPressedBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonSecondaryBackgroundBrush", palette.ButtonSecondaryBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonSecondaryForegroundBrush", palette.ButtonSecondaryForegroundColor);
        UpdateBrushColor("ComfortScreenButtonSecondaryBorderBrush", palette.ButtonSecondaryBorderColor);
        UpdateBrushColor("ComfortScreenButtonSecondaryHoverBackgroundBrush", palette.ButtonSecondaryHoverBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonSecondaryPressedBackgroundBrush", palette.ButtonSecondaryPressedBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonDisabledBackgroundBrush", palette.ButtonDisabledBackgroundColor);
        UpdateBrushColor("ComfortScreenButtonDisabledForegroundBrush", palette.ButtonDisabledForegroundColor);
        UpdateBrushColor("ComfortScreenButtonDisabledBorderBrush", palette.ButtonDisabledBorderColor);
        UpdateBrushColor("ComfortScreenSliderTrackBrush", palette.SliderTrackColor);
        UpdateBrushColor("ComfortScreenSliderActiveTrackBrush", palette.SliderActiveTrackColor);
        UpdateBrushColor("ComfortScreenSliderThumbBrush", palette.SliderThumbColor);
        UpdateBrushColor("ComfortScreenSliderThumbBorderBrush", palette.SliderThumbBorderColor);
        UpdateBrushColor("ComfortScreenSliderThumbHoverBrush", palette.SliderThumbHoverColor);
        UpdateBrushColor("ComfortScreenSliderThumbPressedBrush", palette.SliderThumbPressedColor);
        UpdateBrushColor("ComfortScreenSliderDisabledTrackBrush", palette.SliderDisabledTrackColor);
        UpdateBrushColor("ComfortScreenSliderDisabledThumbBrush", palette.SliderDisabledThumbColor);
        UpdateBrushColor("ComfortScreenSliderDisabledThumbBorderBrush", palette.SliderDisabledThumbBorderColor);
    }

    private static void UpdateBrushColor(string key, MediaColor color)
    {
        var resources = System.Windows.Application.Current.Resources;
        var owner = FindOwner(resources, key) ?? resources;

        if (owner[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = color;
            return;
        }

        if (owner[key] is SolidColorBrush frozenBrush)
        {
            var replacement = frozenBrush.CloneCurrentValue();
            replacement.Color = color;
            owner[key] = replacement;
            return;
        }

        owner[key] = new SolidColorBrush(color);
    }

    private static ResourceDictionary? FindOwner(ResourceDictionary dictionary, string key)
    {
        if (dictionary.Contains(key))
        {
            return dictionary;
        }

        for (var index = dictionary.MergedDictionaries.Count - 1; index >= 0; index--)
        {
            var owner = FindOwner(dictionary.MergedDictionaries[index], key);
            if (owner is not null)
            {
                return owner;
            }
        }

        return null;
    }

    private sealed record ThemePalette(
        MediaColor ShellBackgroundColor,
        MediaColor PaneBackgroundColor,
        MediaColor HeroColor,
        MediaColor AccentColor,
        MediaColor PrimaryTextColor,
        MediaColor SecondaryTextColor,
        MediaColor SurfaceColor,
        MediaColor SurfaceBorderColor,
        MediaColor NavigationHostColor,
        MediaColor NavigationHostBorderColor,
        MediaColor DialogBackgroundColor,
        MediaColor DialogSurfaceColor,
        MediaColor DialogTitleBackgroundColor,
        MediaColor DialogTitleBorderColor,
        MediaColor DialogTitleTextColor,
        MediaColor DialogFooterBackgroundColor,
        MediaColor DialogIconBackgroundColor,
        MediaColor DialogIconForegroundColor,
        MediaColor DialogCloseButtonHoverColor,
        MediaColor DialogPrimaryTextColor,
        MediaColor DialogSecondaryTextColor,
        MediaColor OverlayPrimaryTextColor,
        MediaColor OverlayAccentColor,
        MediaColor OverlayButtonBackgroundColor,
        MediaColor OverlayButtonForegroundColor,
        MediaColor InputBackgroundColor,
        MediaColor InputHoverBackgroundColor,
        MediaColor InputBorderColor,
        MediaColor InputFocusBorderColor,
        MediaColor InputForegroundColor,
        MediaColor InputPlaceholderColor,
        MediaColor InputSelectionColor,
        MediaColor InputSelectionForegroundColor,
        MediaColor InputDisabledBackgroundColor,
        MediaColor InputDisabledForegroundColor,
        MediaColor DropDownBackgroundColor,
        MediaColor DropDownBorderColor,
        MediaColor DropDownItemForegroundColor,
        MediaColor DropDownItemHoverColor,
        MediaColor DropDownItemSelectedColor,
        MediaColor CheckBoxForegroundColor,
        MediaColor CheckBoxBorderColor,
        MediaColor CheckBoxBackgroundColor,
        MediaColor CheckBoxHoverBackgroundColor,
        MediaColor CheckBoxCheckedBackgroundColor,
        MediaColor CheckBoxCheckedBorderColor,
        MediaColor CheckBoxGlyphColor,
        MediaColor ButtonPrimaryBackgroundColor,
        MediaColor ButtonPrimaryForegroundColor,
        MediaColor ButtonPrimaryHoverBackgroundColor,
        MediaColor ButtonPrimaryPressedBackgroundColor,
        MediaColor ButtonSecondaryBackgroundColor,
        MediaColor ButtonSecondaryForegroundColor,
        MediaColor ButtonSecondaryBorderColor,
        MediaColor ButtonSecondaryHoverBackgroundColor,
        MediaColor ButtonSecondaryPressedBackgroundColor,
        MediaColor ButtonDisabledBackgroundColor,
        MediaColor ButtonDisabledForegroundColor,
        MediaColor ButtonDisabledBorderColor,
        MediaColor SliderTrackColor,
        MediaColor SliderActiveTrackColor,
        MediaColor SliderThumbColor,
        MediaColor SliderThumbBorderColor,
        MediaColor SliderThumbHoverColor,
        MediaColor SliderThumbPressedColor,
        MediaColor SliderDisabledTrackColor,
        MediaColor SliderDisabledThumbColor,
        MediaColor SliderDisabledThumbBorderColor
    )
    {
        public static ThemePalette Light =>
            new(
                ShellBackgroundColor: MediaColor.FromRgb(247, 249, 252),
                PaneBackgroundColor: MediaColor.FromRgb(236, 242, 250),
                HeroColor: MediaColor.FromRgb(229, 239, 255),
                AccentColor: MediaColor.FromRgb(36, 123, 232),
                PrimaryTextColor: MediaColor.FromRgb(30, 41, 59),
                SecondaryTextColor: MediaColor.FromRgb(82, 98, 121),
                SurfaceColor: MediaColor.FromArgb(0xF8, 0xFF, 0xFF, 0xFF),
                SurfaceBorderColor: MediaColor.FromArgb(0x1A, 0xFF, 0xFF, 0xFF),
                NavigationHostColor: MediaColor.FromArgb(0xF6, 0xFF, 0xFF, 0xFF),
                NavigationHostBorderColor: MediaColor.FromArgb(0x26, 0xFF, 0xFF, 0xFF),
                DialogBackgroundColor: MediaColor.FromRgb(250, 252, 255),
                DialogSurfaceColor: MediaColor.FromRgb(255, 255, 255),
                DialogTitleBackgroundColor: MediaColor.FromRgb(238, 244, 252),
                DialogTitleBorderColor: MediaColor.FromRgb(215, 225, 237),
                DialogTitleTextColor: MediaColor.FromRgb(30, 41, 59),
                DialogFooterBackgroundColor: MediaColor.FromRgb(244, 248, 252),
                DialogIconBackgroundColor: MediaColor.FromRgb(229, 239, 255),
                DialogIconForegroundColor: MediaColor.FromRgb(36, 123, 232),
                DialogCloseButtonHoverColor: MediaColor.FromRgb(225, 233, 243),
                DialogPrimaryTextColor: MediaColor.FromRgb(30, 41, 59),
                DialogSecondaryTextColor: MediaColor.FromRgb(82, 98, 121),
                OverlayPrimaryTextColor: MediaColor.FromRgb(248, 250, 252),
                OverlayAccentColor: MediaColor.FromRgb(164, 198, 255),
                OverlayButtonBackgroundColor: MediaColor.FromRgb(229, 239, 255),
                OverlayButtonForegroundColor: MediaColor.FromRgb(31, 41, 55),
                InputBackgroundColor: MediaColor.FromRgb(255, 255, 255),
                InputHoverBackgroundColor: MediaColor.FromRgb(246, 250, 255),
                InputBorderColor: MediaColor.FromRgb(200, 213, 230),
                InputFocusBorderColor: MediaColor.FromRgb(36, 123, 232),
                InputForegroundColor: MediaColor.FromRgb(30, 41, 59),
                InputPlaceholderColor: MediaColor.FromRgb(123, 140, 164),
                InputSelectionColor: MediaColor.FromRgb(36, 123, 232),
                InputSelectionForegroundColor: MediaColor.FromRgb(255, 255, 255),
                InputDisabledBackgroundColor: MediaColor.FromRgb(238, 243, 249),
                InputDisabledForegroundColor: MediaColor.FromRgb(151, 167, 187),
                DropDownBackgroundColor: MediaColor.FromRgb(255, 255, 255),
                DropDownBorderColor: MediaColor.FromRgb(200, 213, 230),
                DropDownItemForegroundColor: MediaColor.FromRgb(30, 41, 59),
                DropDownItemHoverColor: MediaColor.FromRgb(232, 241, 255),
                DropDownItemSelectedColor: MediaColor.FromRgb(214, 232, 255),
                CheckBoxForegroundColor: MediaColor.FromRgb(30, 41, 59),
                CheckBoxBorderColor: MediaColor.FromRgb(143, 165, 194),
                CheckBoxBackgroundColor: MediaColor.FromRgb(255, 255, 255),
                CheckBoxHoverBackgroundColor: MediaColor.FromRgb(243, 248, 255),
                CheckBoxCheckedBackgroundColor: MediaColor.FromRgb(36, 123, 232),
                CheckBoxCheckedBorderColor: MediaColor.FromRgb(36, 123, 232),
                CheckBoxGlyphColor: MediaColor.FromRgb(255, 255, 255),
                ButtonPrimaryBackgroundColor: MediaColor.FromRgb(36, 123, 232),
                ButtonPrimaryForegroundColor: MediaColor.FromRgb(255, 255, 255),
                ButtonPrimaryHoverBackgroundColor: MediaColor.FromRgb(29, 108, 208),
                ButtonPrimaryPressedBackgroundColor: MediaColor.FromRgb(24, 90, 175),
                ButtonSecondaryBackgroundColor: MediaColor.FromRgb(243, 248, 255),
                ButtonSecondaryForegroundColor: MediaColor.FromRgb(30, 41, 59),
                ButtonSecondaryBorderColor: MediaColor.FromRgb(200, 213, 230),
                ButtonSecondaryHoverBackgroundColor: MediaColor.FromRgb(230, 240, 255),
                ButtonSecondaryPressedBackgroundColor: MediaColor.FromRgb(215, 232, 255),
                ButtonDisabledBackgroundColor: MediaColor.FromRgb(232, 238, 246),
                ButtonDisabledForegroundColor: MediaColor.FromRgb(147, 162, 183),
                ButtonDisabledBorderColor: MediaColor.FromRgb(212, 222, 233),
                SliderTrackColor: MediaColor.FromRgb(216, 226, 240),
                SliderActiveTrackColor: MediaColor.FromRgb(36, 123, 232),
                SliderThumbColor: MediaColor.FromRgb(255, 255, 255),
                SliderThumbBorderColor: MediaColor.FromRgb(36, 123, 232),
                SliderThumbHoverColor: MediaColor.FromRgb(245, 249, 255),
                SliderThumbPressedColor: MediaColor.FromRgb(227, 238, 255),
                SliderDisabledTrackColor: MediaColor.FromRgb(231, 237, 245),
                SliderDisabledThumbColor: MediaColor.FromRgb(244, 247, 251),
                SliderDisabledThumbBorderColor: MediaColor.FromRgb(186, 199, 216)
            );

        public static ThemePalette Dark =>
            new(
                ShellBackgroundColor: MediaColor.FromRgb(14, 19, 29),
                PaneBackgroundColor: MediaColor.FromRgb(21, 28, 40),
                HeroColor: MediaColor.FromRgb(22, 34, 54),
                AccentColor: MediaColor.FromRgb(96, 165, 250),
                PrimaryTextColor: MediaColor.FromRgb(241, 245, 249),
                SecondaryTextColor: MediaColor.FromRgb(177, 190, 210),
                SurfaceColor: MediaColor.FromArgb(0xF0, 0x16, 0x21, 0x31),
                SurfaceBorderColor: MediaColor.FromArgb(0x40, 0xD7, 0xE3, 0xF4),
                NavigationHostColor: MediaColor.FromArgb(0xF2, 0x10, 0x18, 0x27),
                NavigationHostBorderColor: MediaColor.FromArgb(0x4A, 0xD0, 0xDB, 0xEC),
                DialogBackgroundColor: MediaColor.FromRgb(23, 30, 43),
                DialogSurfaceColor: MediaColor.FromRgb(30, 39, 56),
                DialogTitleBackgroundColor: MediaColor.FromRgb(28, 37, 52),
                DialogTitleBorderColor: MediaColor.FromRgb(67, 82, 104),
                DialogTitleTextColor: MediaColor.FromRgb(238, 243, 250),
                DialogFooterBackgroundColor: MediaColor.FromRgb(26, 35, 49),
                DialogIconBackgroundColor: MediaColor.FromRgb(37, 55, 79),
                DialogIconForegroundColor: MediaColor.FromRgb(125, 188, 255),
                DialogCloseButtonHoverColor: MediaColor.FromRgb(47, 60, 82),
                DialogPrimaryTextColor: MediaColor.FromRgb(238, 243, 250),
                DialogSecondaryTextColor: MediaColor.FromRgb(177, 190, 210),
                OverlayPrimaryTextColor: MediaColor.FromRgb(241, 245, 249),
                OverlayAccentColor: MediaColor.FromRgb(125, 188, 255),
                OverlayButtonBackgroundColor: MediaColor.FromRgb(96, 165, 250),
                OverlayButtonForegroundColor: MediaColor.FromRgb(12, 18, 28),
                InputBackgroundColor: MediaColor.FromRgb(27, 37, 52),
                InputHoverBackgroundColor: MediaColor.FromRgb(34, 46, 64),
                InputBorderColor: MediaColor.FromRgb(87, 105, 131),
                InputFocusBorderColor: MediaColor.FromRgb(96, 165, 250),
                InputForegroundColor: MediaColor.FromRgb(239, 246, 255),
                InputPlaceholderColor: MediaColor.FromRgb(174, 190, 212),
                InputSelectionColor: MediaColor.FromRgb(59, 130, 246),
                InputSelectionForegroundColor: MediaColor.FromRgb(255, 255, 255),
                InputDisabledBackgroundColor: MediaColor.FromRgb(23, 31, 44),
                InputDisabledForegroundColor: MediaColor.FromRgb(111, 131, 157),
                DropDownBackgroundColor: MediaColor.FromRgb(24, 34, 49),
                DropDownBorderColor: MediaColor.FromRgb(68, 88, 111),
                DropDownItemForegroundColor: MediaColor.FromRgb(241, 245, 249),
                DropDownItemHoverColor: MediaColor.FromRgb(36, 56, 79),
                DropDownItemSelectedColor: MediaColor.FromRgb(46, 75, 110),
                CheckBoxForegroundColor: MediaColor.FromRgb(241, 245, 249),
                CheckBoxBorderColor: MediaColor.FromRgb(109, 129, 153),
                CheckBoxBackgroundColor: MediaColor.FromRgb(27, 37, 52),
                CheckBoxHoverBackgroundColor: MediaColor.FromRgb(34, 48, 68),
                CheckBoxCheckedBackgroundColor: MediaColor.FromRgb(59, 130, 246),
                CheckBoxCheckedBorderColor: MediaColor.FromRgb(59, 130, 246),
                CheckBoxGlyphColor: MediaColor.FromRgb(255, 255, 255),
                ButtonPrimaryBackgroundColor: MediaColor.FromRgb(59, 130, 246),
                ButtonPrimaryForegroundColor: MediaColor.FromRgb(248, 250, 252),
                ButtonPrimaryHoverBackgroundColor: MediaColor.FromRgb(75, 145, 252),
                ButtonPrimaryPressedBackgroundColor: MediaColor.FromRgb(47, 105, 204),
                ButtonSecondaryBackgroundColor: MediaColor.FromRgb(31, 48, 68),
                ButtonSecondaryForegroundColor: MediaColor.FromRgb(241, 245, 249),
                ButtonSecondaryBorderColor: MediaColor.FromRgb(94, 117, 144),
                ButtonSecondaryHoverBackgroundColor: MediaColor.FromRgb(42, 64, 92),
                ButtonSecondaryPressedBackgroundColor: MediaColor.FromRgb(53, 82, 119),
                ButtonDisabledBackgroundColor: MediaColor.FromRgb(28, 37, 51),
                ButtonDisabledForegroundColor: MediaColor.FromRgb(111, 131, 157),
                ButtonDisabledBorderColor: MediaColor.FromRgb(66, 82, 103),
                SliderTrackColor: MediaColor.FromRgb(55, 70, 93),
                SliderActiveTrackColor: MediaColor.FromRgb(96, 165, 250),
                SliderThumbColor: MediaColor.FromRgb(29, 41, 59),
                SliderThumbBorderColor: MediaColor.FromRgb(125, 188, 255),
                SliderThumbHoverColor: MediaColor.FromRgb(38, 52, 74),
                SliderThumbPressedColor: MediaColor.FromRgb(48, 66, 93),
                SliderDisabledTrackColor: MediaColor.FromRgb(41, 52, 69),
                SliderDisabledThumbColor: MediaColor.FromRgb(32, 43, 58),
                SliderDisabledThumbBorderColor: MediaColor.FromRgb(93, 110, 133)
            );
    }
}
