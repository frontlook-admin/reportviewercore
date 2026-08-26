using System.ComponentModel;
using System.Xml.Linq;

namespace Microsoft.Reporting.WinForms;

public sealed class RdlcDesignerSession
{
    private readonly RdlcDocument _document;
    private readonly string _initialXml;
    private readonly Stack<EditBatch> _undo = new();
    private readonly Stack<EditBatch> _redo = new();
    private EditBatch? _active;
    public RdlcDesignerSession(RdlcDocument document) { _document = document ?? throw new ArgumentNullException(nameof(document)); _initialXml = document.ToXml(); }
    public bool IsDirty => _document.ToXml() != _initialXml;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public RdlcDesignTransaction BeginTransaction(string description) => _active is null ? new(this, description) : throw new InvalidOperationException("A design transaction is already active.");
    public PropertyDescriptorCollection GetProperties(object target) => RdlcPropertyGrid.GetProperties(target, this);
    public void SetProperty(object target, string name, object? value)
    {
        var binding = RdlcPropertyGrid.Binding(target, name); var old = binding.Get(); binding.Set(value);
        Record(new EditAction(() => binding.Set(old), () => binding.Set(value)));
    }
    public bool Undo() => Apply(_undo, _redo, undo: true);
    public bool Redo() => Apply(_redo, _undo, undo: false);
    public string Copy(RdlcReportItem item) => item.Xml.ToString(SaveOptions.DisableFormatting);
    public RdlcReportItem Paste(RdlcBody body, string fragment)
    {
        var element = XElement.Parse(fragment, LoadOptions.PreserveWhitespace); body.ItemsElement.Add(element);
        Record(new EditAction(element.Remove, () => body.ItemsElement.Add(element)));
        return new RdlcReportItem(element);
    }
    internal void Start(EditBatch batch) => _active = batch;
    internal void Finish(EditBatch batch, bool commit)
    {
        if (!ReferenceEquals(_active, batch)) throw new InvalidOperationException("Transaction is not active."); _active = null;
        if (!commit) { for (var i = batch.Actions.Count - 1; i >= 0; i--) batch.Actions[i].Undo(); }
        else if (batch.Actions.Count > 0) { _undo.Push(batch); _redo.Clear(); }
    }
    private void Record(EditAction action) { if (_active is null) { _undo.Push(new EditBatch("Edit", [action])); _redo.Clear(); } else _active.Actions.Add(action); }
    private bool Apply(Stack<EditBatch> source, Stack<EditBatch> destination, bool undo)
    {
        if (_active is not null || source.Count == 0) return false; var batch = source.Pop();
        if (undo) for (var i = batch.Actions.Count - 1; i >= 0; i--) batch.Actions[i].Undo(); else foreach (var action in batch.Actions) action.Redo();
        destination.Push(batch); return true;
    }
}

public sealed class RdlcDesignTransaction : IDisposable
{
    private readonly RdlcDesignerSession _session; private bool _done;
    internal RdlcDesignTransaction(RdlcDesignerSession session, string description) { _session = session; Batch = new(description); session.Start(Batch); }
    internal EditBatch Batch { get; }
    public void Commit() { if (_done) throw new InvalidOperationException("Transaction is already completed."); _done = true; _session.Finish(Batch, true); }
    public void Rollback() { if (_done) return; _done = true; _session.Finish(Batch, false); }
    public void Dispose() => Rollback();
}

internal sealed class EditBatch(string description, List<EditAction>? actions = null) { public string Description { get; } = description; public List<EditAction> Actions { get; } = actions ?? []; }
internal sealed class EditAction(Action undo, Action redo) { public Action Undo { get; } = undo; public Action Redo { get; } = redo; }

public static class RdlcPropertyGrid
{
    public static PropertyDescriptorCollection GetProperties(object target, RdlcDesignerSession session)
    {
        var names = target switch
        {
            RdlcReport => new[] { "Name" },
            RdlcPage => new[] { "PageWidth", "PageHeight" },
            RdlcReportItem item => new[] { "Name", "Left", "Top", "Width", "Height" }.Where(x => x == "Name" || item.GetProperty(x) is not null).ToArray(),
            _ => []
        };
        return new(names.Select(x => new RdlcPropertyDescriptor(target, x, session)).ToArray());
    }
    internal static (Func<object?> Get, Action<object?> Set) Binding(object target, string name) => target switch
    {
        RdlcReport report => (() => report.GetProperty(name), value => report.SetProperty(name, value)),
        RdlcPage page => (() => page.GetProperty(name), value => page.SetProperty(name, value)),
        RdlcReportItem item => (() => item.GetProperty(name), value => item.SetProperty(name, value)),
        _ => throw new ArgumentException("Unsupported RDLC design object.", nameof(target))
    };
}

internal sealed class RdlcPropertyDescriptor(object target, string name, RdlcDesignerSession session) : PropertyDescriptor(name, null)
{
    public override Type ComponentType => target.GetType(); public override Type PropertyType => typeof(string); public override bool IsReadOnly => false;
    public override object? GetValue(object? component) => RdlcPropertyGrid.Binding(target, Name).Get();
    public override void SetValue(object? component, object? value) => session.SetProperty(target, Name, value);
    public override bool CanResetValue(object? component) => false; public override void ResetValue(object? component) { }
    public override bool ShouldSerializeValue(object? component) => false;
}
