using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace Microsoft.Reporting.WinForms;

public sealed class RdlcCanvasItem
{
    public RdlcCanvasItem(string id, string kind, RectangleF bounds, string? accessibleName = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Bounds = bounds;
        AccessibleName = accessibleName ?? $"{kind} {id}";
    }

    public string Id { get; }
    public string Kind { get; }
    public RectangleF Bounds { get; internal set; }
    public string AccessibleName { get; }
    public int ZIndex { get; internal set; }
}

public static class RdlcCanvasGeometry
{
    public static PointF ScreenToReport(PointF screen, PointF canvasOrigin, float zoom, float dpi)
    {
        if (zoom <= 0 || dpi <= 0) throw new ArgumentOutOfRangeException();
        var pixelsPerInch = 96f * zoom * dpi / 96f;
        return new PointF((screen.X - canvasOrigin.X) / pixelsPerInch, (screen.Y - canvasOrigin.Y) / pixelsPerInch);
    }

    public static PointF ReportToScreen(PointF report, PointF canvasOrigin, float zoom, float dpi)
    {
        if (zoom <= 0 || dpi <= 0) throw new ArgumentOutOfRangeException();
        var pixelsPerInch = 96f * zoom * dpi / 96f;
        return new PointF(canvasOrigin.X + report.X * pixelsPerInch, canvasOrigin.Y + report.Y * pixelsPerInch);
    }

    public static RectangleF SnapAndClamp(RectangleF bounds, SizeF pageSize, float grid)
    {
        if (grid <= 0) throw new ArgumentOutOfRangeException(nameof(grid));
        var x = MathF.Round(bounds.X / grid) * grid;
        var y = MathF.Round(bounds.Y / grid) * grid;
        var width = MathF.Max(grid, MathF.Round(bounds.Width / grid) * grid);
        var height = MathF.Max(grid, MathF.Round(bounds.Height / grid) * grid);
        return new RectangleF(MathF.Max(0, MathF.Min(x, pageSize.Width - width)), MathF.Max(0, MathF.Min(y, pageSize.Height - height)),
            MathF.Min(width, pageSize.Width), MathF.Min(height, pageSize.Height));
    }
}

public sealed class RdlcCanvasSelectionState
{
    private readonly IReadOnlyList<string> _order;
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
    private int _focusIndex;

    public RdlcCanvasSelectionState(IEnumerable<string> order)
    {
        _order = order.ToArray();
        if (_order.Count > 0) _focusIndex = 0;
    }

    public IReadOnlyList<string> SelectedIds => _order.Where(_selected.Contains).ToArray();
    public string? FocusedId => _order.Count == 0 ? null : _order[_focusIndex];
    public void Select(string id) { _selected.Clear(); ToggleCore(id); }
    public void Toggle(string id) { ToggleCore(id); }
    public string? MoveFocus(int delta)
    {
        if (_order.Count == 0) return null;
        _focusIndex = Math.Clamp(_focusIndex + delta, 0, _order.Count - 1);
        return FocusedId;
    }
    public void SelectFocused() { if (FocusedId is { } id) Select(id); }
    public bool IsSelected(string id) => _selected.Contains(id);
    public void Clear() => _selected.Clear();
    private void ToggleCore(string id)
    {
        var index = Array.IndexOf(_order.ToArray(), id);
        if (index < 0) return;
        _focusIndex = index;
        if (!_selected.Add(id)) _selected.Remove(id);
    }
}

public static class RdlcCanvasLayout
{
    public static void AlignTop(IEnumerable<RdlcCanvasItem> items)
    {
        var list = items.ToArray(); if (list.Length == 0) return;
        var top = list.Min(x => x.Bounds.Top);
        foreach (var item in list) item.Bounds = new RectangleF(item.Bounds.X, top, item.Bounds.Width, item.Bounds.Height);
    }

    public static void AlignLeft(IEnumerable<RdlcCanvasItem> items)
    {
        var list = items.ToArray(); if (list.Length == 0) return;
        var left = list.Min(x => x.Bounds.Left);
        foreach (var item in list) item.Bounds = new RectangleF(left, item.Bounds.Y, item.Bounds.Width, item.Bounds.Height);
    }

    public static void DistributeVertical(IEnumerable<RdlcCanvasItem> items)
    {
        var list = items.OrderBy(x => x.Bounds.Top).ToArray(); if (list.Length < 3) return;
        var step = (list[^1].Bounds.Top - list[0].Bounds.Top) / (list.Length - 1);
        for (var i = 1; i < list.Length - 1; i++)
            list[i].Bounds = new RectangleF(list[i].Bounds.X, list[0].Bounds.Top + step * i, list[i].Bounds.Width, list[i].Bounds.Height);
    }
}

public sealed class RdlcDesignerCanvas : ScrollableControl
{
    private static string? _clipboard;
    private readonly List<RdlcCanvasItem> _items = new();
    private RdlcCanvasSelectionState _selection = new(Array.Empty<string>());
    private float _zoom = 1f;
    private Point _dragStart;
    private Dictionary<string, RectangleF>? _dragBounds;

    public RdlcDesignerCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        AccessibleRole = AccessibleRole.Client;
        AccessibleName = "Report design canvas";
        TabStop = true;
        BackColor = Color.FromArgb(246, 247, 249);
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
    }

    public Collection<RdlcCanvasItem> Items => new(_items);
    public SizeF PageSize { get; set; } = new(8.5f, 11f);
    public float GridSize { get; set; } = .25f;
    public bool SnapToGrid { get; set; } = true;
    public Collection<float> Guides { get; } = new();
    public float Zoom { get => _zoom; set { _zoom = Math.Clamp(value, .1f, 4f); UpdateScrollSize(); Invalidate(); } }
    public IReadOnlyList<string> SelectedIds => _selection.SelectedIds;
    public event EventHandler? SelectionChanged;

    public void SelectItem(string id, bool toggle = false) { if (toggle) _selection.Toggle(id); else _selection.Select(id); SelectionChanged?.Invoke(this, EventArgs.Empty); Invalidate(); }
    public void DeleteSelected() { _items.RemoveAll(x => _selection.IsSelected(x.Id)); _selection = new RdlcCanvasSelectionState(_items.Select(x => x.Id)); Invalidate(); SelectionChanged?.Invoke(this, EventArgs.Empty); }
    public IEnumerable<string> GetAccessibleItemNames() => _items.Select(x => x.AccessibleName);
    public void AlignTopSelected() => ApplyLayout(RdlcCanvasLayout.AlignTop);
    public void AlignLeftSelected() => ApplyLayout(RdlcCanvasLayout.AlignLeft);
    public void DistributeVerticalSelected() => ApplyLayout(RdlcCanvasLayout.DistributeVertical);
    public void ResizeSelected(float width, float height)
    {
        foreach (var item in _items.Where(x => _selection.IsSelected(x.Id)))
            item.Bounds = RdlcCanvasGeometry.SnapAndClamp(new RectangleF(item.Bounds.X, item.Bounds.Y, width, height), PageSize, SnapToGrid ? GridSize : .0001f);
        Invalidate();
    }
    public void CopySelected()
    {
        _clipboard = string.Join("\n", _items.Where(x => _selection.IsSelected(x.Id)).Select(x => string.Join("|", x.Id, x.Kind, x.Bounds.X.ToString(CultureInfo.InvariantCulture), x.Bounds.Y.ToString(CultureInfo.InvariantCulture), x.Bounds.Width.ToString(CultureInfo.InvariantCulture), x.Bounds.Height.ToString(CultureInfo.InvariantCulture))));
    }
    public void PasteCopied()
    {
        if (string.IsNullOrWhiteSpace(_clipboard)) return;
        var pasted = new List<RdlcCanvasItem>();
        foreach (var row in _clipboard.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = row.Split('|'); if (parts.Length != 6) continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) || !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)) continue;
            var id = parts[0] + " Copy"; var suffix = 2; while (_items.Any(i => i.Id == id)) id = parts[0] + " Copy " + suffix++;
            pasted.Add(new RdlcCanvasItem(id, parts[1], RdlcCanvasGeometry.SnapAndClamp(new RectangleF(x + GridSize, y + GridSize, w, h), PageSize, GridSize)));
        }
        _items.AddRange(pasted); _selection = new RdlcCanvasSelectionState(_items.Select(x => x.Id)); foreach (var item in pasted) _selection.Toggle(item.Id); Invalidate(); SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    public void BringSelectedToFront() { var z = _items.Count; foreach (var item in _items.Where(x => _selection.IsSelected(x.Id))) item.ZIndex = z++; Invalidate(); }
    public void SendSelectedToBack() { var z = 0; foreach (var item in _items.Where(x => _selection.IsSelected(x.Id))) item.ZIndex = z++; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var origin = new PointF(48 - AutoScrollPosition.X, 32 - AutoScrollPosition.Y);
        e.Graphics.Clear(BackColor); DrawGrid(e.Graphics, origin); var page = new RectangleF(origin.X, origin.Y, PageSize.Width * PixelsPerInch, PageSize.Height * PixelsPerInch);
        using var pageBrush = new SolidBrush(Color.White); e.Graphics.FillRectangle(pageBrush, page); e.Graphics.DrawRectangle(Pens.DarkGray, page.X, page.Y, page.Width, page.Height);
        DrawRulers(e.Graphics, origin);
        foreach (var item in _items.OrderBy(x => x.ZIndex)) { var r = ToScreen(item.Bounds, origin); using var fill = new SolidBrush(_selection.IsSelected(item.Id) ? Color.FromArgb(220, 219, 234, 255) : Color.FromArgb(245, 250, 252)); e.Graphics.FillRectangle(fill, r); e.Graphics.DrawRectangle(_selection.IsSelected(item.Id) ? Pens.RoyalBlue : Pens.SlateGray, r.X, r.Y, r.Width, r.Height); TextRenderer.DrawText(e.Graphics, item.Kind, Font, Rectangle.Round(r), Color.FromArgb(23, 32, 42), TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
    }

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); var id = HitTest(e.Location); if (id is null) { _selection.Clear(); Invalidate(); return; } SelectItem(id, (ModifierKeys & Keys.Control) != 0); _dragStart = e.Location; _dragBounds = _items.Where(x => _selection.IsSelected(x.Id)).ToDictionary(x => x.Id, x => x.Bounds); }
    protected override void OnMouseMove(MouseEventArgs e) { if (_dragBounds is null || e.Button != MouseButtons.Left) return; var dx = (e.X - _dragStart.X) / PixelsPerInch; var dy = (e.Y - _dragStart.Y) / PixelsPerInch; foreach (var item in _items.Where(x => _dragBounds.ContainsKey(x.Id))) { var b = _dragBounds[item.Id]; var moved = new RectangleF(b.X + dx, b.Y + dy, b.Width, b.Height); item.Bounds = SnapToGrid ? RdlcCanvasGeometry.SnapAndClamp(moved, PageSize, GridSize) : RdlcCanvasGeometry.SnapAndClamp(moved, PageSize, .0001f); } Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { _dragBounds = null; base.OnMouseUp(e); }
    protected override bool IsInputKey(Keys keyData) => true;
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Delete) DeleteSelected(); else if (e.KeyCode == Keys.Left) Nudge(-.05f, 0); else if (e.KeyCode == Keys.Right) Nudge(.05f, 0); else if (e.KeyCode == Keys.Up) Nudge(0, -.05f); else if (e.KeyCode == Keys.Down) Nudge(0, .05f); else if (e.KeyCode == Keys.Tab) { _selection.MoveFocus(e.Shift ? -1 : 1); _selection.SelectFocused(); SelectionChanged?.Invoke(this, EventArgs.Empty); Invalidate(); } }

    private float PixelsPerInch => 96f * Zoom * DeviceDpi / 96f;
    private RectangleF ToScreen(RectangleF r, PointF o) => new(o.X + r.X * PixelsPerInch, o.Y + r.Y * PixelsPerInch, r.Width * PixelsPerInch, r.Height * PixelsPerInch);
    private string? HitTest(Point p) => _items.OrderByDescending(x => x.ZIndex).FirstOrDefault(x => ToScreen(x.Bounds, new PointF(48 - AutoScrollPosition.X, 32 - AutoScrollPosition.Y)).Contains(p))?.Id;
    private void Nudge(float dx, float dy) { foreach (var item in _items.Where(x => _selection.IsSelected(x.Id))) item.Bounds = RdlcCanvasGeometry.SnapAndClamp(new RectangleF(item.Bounds.X + dx, item.Bounds.Y + dy, item.Bounds.Width, item.Bounds.Height), PageSize, SnapToGrid ? GridSize : .0001f); Invalidate(); }
    private void ApplyLayout(Action<IEnumerable<RdlcCanvasItem>> action) { var selected = _items.Where(x => _selection.IsSelected(x.Id)).ToArray(); action(selected); Invalidate(); }
    private void DrawGrid(Graphics g, PointF o) { using var pen = new Pen(Color.FromArgb(228, 232, 238)); for (var x = 0f; x <= PageSize.Width; x += GridSize) g.DrawLine(pen, o.X + x * PixelsPerInch, o.Y, o.X + x * PixelsPerInch, o.Y + PageSize.Height * PixelsPerInch); for (var y = 0f; y <= PageSize.Height; y += GridSize) g.DrawLine(pen, o.X, o.Y + y * PixelsPerInch, o.X + PageSize.Width * PixelsPerInch, o.Y + y * PixelsPerInch); }
    private void DrawRulers(Graphics g, PointF o) { using var pen = new Pen(Color.FromArgb(91, 103, 117)); using var brush = new SolidBrush(Color.FromArgb(91, 103, 117)); for (var i = 0; i <= PageSize.Width; i++) { var x = o.X + i * PixelsPerInch; g.DrawLine(pen, x, 20, x, 31); if (i % 2 == 0) g.DrawString(i.ToString(CultureInfo.InvariantCulture), Font, brush, x + 2, 3); } for (var i = 0; i <= PageSize.Height; i++) { var y = o.Y + i * PixelsPerInch; g.DrawLine(pen, 36, y, 47, y); if (i % 2 == 0) g.DrawString(i.ToString(CultureInfo.InvariantCulture), Font, brush, 2, y + 2); } using var guidePen = new Pen(Color.FromArgb(37, 99, 235)) { DashStyle = DashStyle.Dash }; foreach (var guide in Guides) g.DrawLine(guidePen, o.X + guide * PixelsPerInch, o.Y, o.X + guide * PixelsPerInch, o.Y + PageSize.Height * PixelsPerInch); }
    private void UpdateScrollSize() { AutoScrollMinSize = new Size((int)(PageSize.Width * PixelsPerInch) + 64, (int)(PageSize.Height * PixelsPerInch) + 48); }
}