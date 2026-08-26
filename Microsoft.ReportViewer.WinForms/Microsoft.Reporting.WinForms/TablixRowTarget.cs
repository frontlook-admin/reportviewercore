using System.Drawing;
using System.Collections.Generic;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class TablixRowTarget
	{
		internal RectangleF Bounds { get; }

		internal bool IsHeader { get; }

		internal int RowIndex { get; }

		internal string Source { get; }

		internal int SourceOrder { get; }

		internal IReadOnlyList<string> Cells { get; }

		internal TablixRowTarget(RectangleF bounds, bool isHeader, int rowIndex, string source, int sourceOrder)
			: this(bounds, isHeader, rowIndex, source, sourceOrder, null)
		{
		}

		internal TablixRowTarget(RectangleF bounds, bool isHeader, int rowIndex, string source, int sourceOrder, IReadOnlyList<string> cells)
		{
			Bounds = bounds;
			IsHeader = isHeader;
			RowIndex = rowIndex;
			Source = source ?? string.Empty;
			SourceOrder = sourceOrder;
			Cells = cells ?? new List<string>();
		}
	}
}