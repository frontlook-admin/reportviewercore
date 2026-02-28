using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.ReportViewer.MAUI.FrontLookCode;

/// <summary>
/// Cross-platform compatible print settings that can be serialized/deserialized.
/// This is a MAUI-compatible equivalent of the WinForms CustomPrintDialog settings.
/// </summary>
public class PrintDialogSettings
{
    /// <summary>
    /// Gets or sets the name of the printer.
    /// </summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether page range selection is allowed.
    /// </summary>
    public bool AllowSomePages { get; set; }

    /// <summary>
    /// Gets or sets whether selection printing is allowed.
    /// </summary>
    public bool AllowSelection { get; set; }

    /// <summary>
    /// Gets or sets whether print to file is allowed.
    /// </summary>
    public bool AllowPrintToFile { get; set; }

    /// <summary>
    /// Gets or sets whether to print to file.
    /// </summary>
    public bool PrintToFile { get; set; }

    /// <summary>
    /// Gets or sets whether network printers are shown.
    /// </summary>
    public bool ShowNetwork { get; set; } = true;

    /// <summary>
    /// Gets or sets the print range type.
    /// </summary>
    public PrintRangeType PrintRange { get; set; } = PrintRangeType.AllPages;

    /// <summary>
    /// Gets or sets the number of copies.
    /// </summary>
    public short Copies { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether copies are collated.
    /// </summary>
    public bool Collate { get; set; }

    /// <summary>
    /// Gets or sets the starting page (1-based).
    /// </summary>
    public int FromPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the ending page (1-based).
    /// </summary>
    public int ToPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum page number.
    /// </summary>
    public int MinimumPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum page number.
    /// </summary>
    public int MaximumPage { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets duplex printing mode.
    /// </summary>
    public DuplexType Duplex { get; set; } = DuplexType.Default;

    /// <summary>
    /// Gets or sets the page settings.
    /// </summary>
    public PageSettingsModel PageSettings { get; set; } = new PageSettingsModel();

    /// <summary>
    /// Serializes the settings to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserializes settings from JSON.
    /// </summary>
    public static PrintDialogSettings? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<PrintDialogSettings>(json);
    }

    /// <summary>
    /// Creates a copy of this settings object.
    /// </summary>
    public PrintDialogSettings Clone()
    {
        return new PrintDialogSettings
        {
            PrinterName = PrinterName,
            AllowSomePages = AllowSomePages,
            AllowSelection = AllowSelection,
            AllowPrintToFile = AllowPrintToFile,
            PrintToFile = PrintToFile,
            ShowNetwork = ShowNetwork,
            PrintRange = PrintRange,
            Copies = Copies,
            Collate = Collate,
            FromPage = FromPage,
            ToPage = ToPage,
            MinimumPage = MinimumPage,
            MaximumPage = MaximumPage,
            Duplex = Duplex,
            PageSettings = PageSettings.Clone()
        };
    }
}

/// <summary>
/// Print range type enumeration.
/// </summary>
public enum PrintRangeType
{
    /// <summary>
    /// Print all pages.
    /// </summary>
    AllPages,

    /// <summary>
    /// Print selected content.
    /// </summary>
    Selection,

    /// <summary>
    /// Print specific pages.
    /// </summary>
    SomePages,

    /// <summary>
    /// Print current page only.
    /// </summary>
    CurrentPage
}

/// <summary>
/// Cross-platform page settings model.
/// </summary>
public class PageSettingsModel
{
    /// <summary>
    /// Gets or sets whether to print in color.
    /// </summary>
    public bool Color { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to print in landscape orientation.
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    /// Gets or sets the paper size.
    /// </summary>
    public PaperSizeModel? PaperSize { get; set; } = new PaperSizeModel();

    /// <summary>
    /// Gets or sets the margins.
    /// </summary>
    public MarginsModel? Margins { get; set; } = new MarginsModel();

    /// <summary>
    /// Creates a copy of this settings object.
    /// </summary>
    public PageSettingsModel Clone()
    {
        return new PageSettingsModel
        {
            Color = Color,
            Landscape = Landscape,
            PaperSize = PaperSize?.Clone(),
            Margins = Margins?.Clone()
        };
    }
}

/// <summary>
/// Cross-platform paper size model.
/// </summary>
public class PaperSizeModel
{
    /// <summary>
    /// Creates a paper size with default Letter size values.
    /// </summary>
    public PaperSizeModel()
    {
        Name = "Letter";
        Width = 850;  // 8.5 inches in hundredths
        Height = 1100; // 11 inches in hundredths
    }

    /// <summary>
    /// Creates a paper size with the specified values.
    /// </summary>
    /// <param name="name">Paper size name (e.g., "Letter", "A4").</param>
    /// <param name="width">Width in hundredths of an inch.</param>
    /// <param name="height">Height in hundredths of an inch.</param>
    public PaperSizeModel(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets or sets the paper size name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the paper size name (alias for Name property).
    /// </summary>
    public string? PaperName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    /// Gets or sets the width in hundredths of an inch.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height in hundredths of an inch.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Common paper sizes.
    /// </summary>
    public static PaperSizeModel Letter => new() { Name = "Letter", Width = 850, Height = 1100 };
    public static PaperSizeModel Legal => new() { Name = "Legal", Width = 850, Height = 1400 };
    public static PaperSizeModel A4 => new() { Name = "A4", Width = 827, Height = 1169 };
    public static PaperSizeModel A3 => new() { Name = "A3", Width = 1169, Height = 1654 };
    public static PaperSizeModel A5 => new() { Name = "A5", Width = 583, Height = 827 };

    /// <summary>
    /// Creates a copy of this size object.
    /// </summary>
    public PaperSizeModel Clone()
    {
        return new PaperSizeModel
        {
            Name = Name,
            Width = Width,
            Height = Height
        };
    }
}

/// <summary>
/// Cross-platform margins model.
/// </summary>
public class MarginsModel
{
    /// <summary>
    /// Creates margins with default values (100 hundredths of an inch on all sides).
    /// </summary>
    public MarginsModel()
    {
    }

    /// <summary>
    /// Creates margins with the specified values.
    /// </summary>
    /// <param name="left">Left margin in hundredths of an inch.</param>
    /// <param name="right">Right margin in hundredths of an inch.</param>
    /// <param name="top">Top margin in hundredths of an inch.</param>
    /// <param name="bottom">Bottom margin in hundredths of an inch.</param>
    public MarginsModel(int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    /// <summary>
    /// Gets or sets the left margin in hundredths of an inch.
    /// </summary>
    public int Left { get; set; } = 100;

    /// <summary>
    /// Gets or sets the top margin in hundredths of an inch.
    /// </summary>
    public int Top { get; set; } = 100;

    /// <summary>
    /// Gets or sets the right margin in hundredths of an inch.
    /// </summary>
    public int Right { get; set; } = 100;

    /// <summary>
    /// Gets or sets the bottom margin in hundredths of an inch.
    /// </summary>
    public int Bottom { get; set; } = 100;

    /// <summary>
    /// Creates margins from millimeter values.
    /// </summary>
    public static MarginsModel FromMillimeters(double left, double top, double right, double bottom)
    {
        const double mmToHundredths = 3.937; // 1 mm = 0.03937 inches * 100
        return new MarginsModel
        {
            Left = (int)(left * mmToHundredths),
            Top = (int)(top * mmToHundredths),
            Right = (int)(right * mmToHundredths),
            Bottom = (int)(bottom * mmToHundredths)
        };
    }

    /// <summary>
    /// Creates a copy of this margins object.
    /// </summary>
    public MarginsModel Clone()
    {
        return new MarginsModel
        {
            Left = Left,
            Top = Top,
            Right = Right,
            Bottom = Bottom
        };
    }
}

/// <summary>
/// Duplex printing type enumeration.
/// </summary>
public enum DuplexType
{
    /// <summary>
    /// Use default printer duplex setting.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Single-sided printing.
    /// </summary>
    Simplex = 1,

    /// <summary>
    /// Double-sided, flip on long edge (top-to-bottom).
    /// </summary>
    Horizontal = 2,

    /// <summary>
    /// Double-sided, flip on short edge (left-to-right).
    /// </summary>
    Vertical = 3
}

/// <summary>
/// Print type enumeration.
/// </summary>
public enum PrintType
{
    /// <summary>
    /// Use default printer settings.
    /// </summary>
    Default = 1,

    /// <summary>
    /// Use modified settings.
    /// </summary>
    Modified = 2
}
