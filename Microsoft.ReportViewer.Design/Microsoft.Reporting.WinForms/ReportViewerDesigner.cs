using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Windows.Forms.Design;

namespace Microsoft.Reporting.WinForms;

/// <summary>Visual Studio designer adapter for the ReportViewer control.</summary>
[DesignerCategory("Component")]
public sealed class ReportViewerDesigner : ControlDesigner
{
    private static readonly string[] HiddenRuntimeProperties =
    [
        nameof(ReportViewer.CurrentPage),
        nameof(ReportViewer.LocalReport),
        nameof(ReportViewer.ServerReport),
        nameof(ReportViewer.Theme)
    ];

    private DesignerActionListCollection? _actionLists;

    public override void Initialize(IComponent component)
    {
        base.Initialize(component);
    }

    protected override void PreFilterProperties(IDictionary properties)
    {
        base.PreFilterProperties(properties);

        foreach (var propertyName in HiddenRuntimeProperties)
        {
            if (properties[propertyName] is PropertyDescriptor descriptor &&
                descriptor.SerializationVisibility == DesignerSerializationVisibility.Hidden)
            {
                properties.Remove(propertyName);
            }
        }
    }

    /// <summary>
    /// Supplies a small, deterministic smart-tag surface without putting
    /// design-time dependencies into the runtime ReportViewer assembly.
    /// </summary>
    public override DesignerActionListCollection ActionLists =>
        _actionLists ??= new DesignerActionListCollection
        {
            new ReportViewerDesignerActionList(Component!)
        };

    /// <summary>
    /// Exposes the same operation through the traditional Properties window
    /// verbs used by older WinForms designer hosts.
    /// </summary>
    public override DesignerVerbCollection Verbs => new(
    [
        new DesignerVerb("Reset viewer appearance", (_, _) =>
            ResetViewerAppearance())
    ]);

    private void ResetViewerAppearance()
    {
        if (Component is not Control control)
        {
            return;
        }

        foreach (var propertyName in ReportViewerDesignerActionList.ResettablePropertyNames)
        {
            TypeDescriptor.GetProperties(control)[propertyName]?.ResetValue(control);
        }
    }
}

internal sealed class ReportViewerDesignerActionList : DesignerActionList
{
    internal static readonly string[] ResettablePropertyNames =
    [
        nameof(Control.BackColor),
        nameof(Control.ForeColor),
        nameof(Control.Font),
        nameof(Control.RightToLeft)
    ];

    public ReportViewerDesignerActionList(IComponent component)
        : base(component)
    {
    }

    public void ResetViewerAppearance()
    {
        if (Component is not Control control)
        {
            return;
        }

        foreach (var propertyName in ResettablePropertyNames)
        {
            TypeDescriptor.GetProperties(control)[propertyName]?.ResetValue(control);
        }
    }

    public override DesignerActionItemCollection GetSortedActionItems() => new()
    {
        new DesignerActionHeaderItem("ReportViewer"),
        new DesignerActionMethodItem(
            this,
            nameof(ResetViewerAppearance),
            "Reset viewer appearance",
            "Appearance",
            "Restore the standard WinForms appearance values.",
            includeAsDesignerVerb: true)
    };
}

/// <summary>
/// Keeps the standard CodeDOM serializer available to hosts that discover
/// serializers through the design assembly rather than the control assembly.
/// </summary>
public sealed class ReportViewerDesignerCodeDomSerializer : CodeDomSerializer
{
    public override object? Serialize(IDesignerSerializationManager manager, object value)
    {
        return base.Serialize(manager, value);
    }
}
