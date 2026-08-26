using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Windows.Forms.Design;

namespace Microsoft.Reporting.WinForms;

/// <summary>
/// Minimal Visual Studio designer adapter for the ReportViewer control.
/// </summary>
public sealed class ReportViewerDesigner : ControlDesigner
{
    private static readonly string[] HiddenRuntimeProperties =
    [
        nameof(ReportViewer.CurrentPage),
        nameof(ReportViewer.LocalReport),
        nameof(ReportViewer.ServerReport),
        nameof(ReportViewer.Theme)
    ];

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
