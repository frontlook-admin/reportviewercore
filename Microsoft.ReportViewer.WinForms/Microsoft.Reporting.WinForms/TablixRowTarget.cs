using System.Drawing;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class TablixRowTarget
	{
		internal RectangleF Bounds { get; }

		internal bool IsHeader { get; }

		internal int RowIndex { get; }

		internal string Source { get; }

		internal int SourceOrder { get; }

		internal TablixRowTarget(RectangleF bounds, bool isHeader, int rowIndex, string source, int sourceOrder)
		{
			Bounds = bounds;
			IsHeader = isHeader;
			RowIndex = rowIndex;
			Source = source ?? string.Empty;
			SourceOrder = sourceOrder;
		}
	}
}