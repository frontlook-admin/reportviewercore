using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class RenderingTablix : RenderingItemContainer
	{
		private List<RenderingItemBorderTablix> m_cornerBorders = new List<RenderingItemBorderTablix>();

		private List<RenderingItemBorderTablix> m_columnHeaderBorders = new List<RenderingItemBorderTablix>();

		private List<RenderingItemBorderTablix> m_rowHeaderBorders = new List<RenderingItemBorderTablix>();

		private List<RenderingItemBorderTablix> m_detailCellBorders = new List<RenderingItemBorderTablix>();

		private RPLFormat.Directions Direction
		{
			get
			{
				object stylePropertyValueObject = SharedRenderer.GetStylePropertyValueObject(InstanceProperties, 29);
				if (stylePropertyValueObject == null)
				{
					return RPLFormat.Directions.LTR;
				}
				return (RPLFormat.Directions)stylePropertyValueObject;
			}
		}

		internal override void ProcessRenderingElementContent(RPLElement rplElement, GdiContext context, RectangleF bounds)
		{
			RPLTablix rPLTablix = (RPLTablix)rplElement;
			FixedHeaderItem fixedHeaderItem = null;
			FixedHeaderItem fixedHeaderItem2 = null;
			FixedHeaderItem fixedHeaderItem3 = null;
			float num = 0f;
			float num2 = bounds.Top;
			float[] array = null;
			array = ((rPLTablix.ColumnWidths == null) ? new float[0] : new float[rPLTablix.ColumnWidths.Length]);
			int[] array2 = new int[array.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = int.MaxValue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (j > 0)
				{
					array[j] = array[j - 1] + rPLTablix.ColumnWidths[j - 1];
				}
				if (rPLTablix.FixedColumns[j])
				{
					if (fixedHeaderItem3 == null)
					{
						fixedHeaderItem3 = new FixedHeaderItem(this, base.Position, FixedHeaderItem.LayoutType.Vertical);
						fixedHeaderItem3.Bounds.X += array[j];
						fixedHeaderItem3.Bounds.Width = rPLTablix.ColumnWidths[j];
					}
					else
					{
						fixedHeaderItem3.Bounds.X = Math.Min(fixedHeaderItem3.Bounds.X, base.Position.X + array[j]);
						fixedHeaderItem3.Bounds.Width += rPLTablix.ColumnWidths[j];
					}
				}
			}
			if (rPLTablix.FixedRow(0))
			{
				fixedHeaderItem = new FixedHeaderItem(this, base.Position, FixedHeaderItem.LayoutType.Horizontal);
				fixedHeaderItem.Bounds.Height = 0f;
				context.RenderingReport.FixedHeaders.Add(fixedHeaderItem);
			}
			if (fixedHeaderItem3 != null)
			{
				context.RenderingReport.FixedHeaders.Add(fixedHeaderItem3);
				if (rPLTablix.FixedRow(0))
				{
					fixedHeaderItem2 = new FixedHeaderItem(this, base.Position, FixedHeaderItem.LayoutType.Corner);
					context.RenderingReport.FixedHeaders.Add(fixedHeaderItem2);
				}
			}
			int num3 = 0;
			int num4 = -1;
			int sourceOrder = context.RenderingReport.AllocateTablixRowTargetSourceOrder();
			string source = rPLTablix.ElementProps?.UniqueName ?? string.Empty;
			RPLTablixRow nextRow;
			while ((nextRow = rPLTablix.GetNextRow()) != null)
			{
				if (nextRow is RPLTablixOmittedRow)
				{
					continue;
				}
				if (rPLTablix.RowHeights == null || num3 < 0 || num3 >= rPLTablix.RowHeights.Length || nextRow.NumCells == 0 || !IsValidRowHeight(rPLTablix.RowHeights[num3]))
				{
					num3++;
					continue;
				}
				List<string> rowCells = new List<string>();
				SharedRenderer.CalculateColumnZIndexes(rPLTablix, nextRow, num3, array2);
				if (nextRow.OmittedHeaders != null)
				{
					for (int k = 0; k < nextRow.OmittedHeaders.Count; k++)
					{
						RPLTablixMemberCell rPLTablixMemberCell = nextRow.OmittedHeaders[k];
						if (!string.IsNullOrEmpty(rPLTablixMemberCell.GroupLabel))
						{
							float x = bounds.X;
							if (rPLTablixMemberCell.ColIndex < array.Length)
							{
								x = array[rPLTablixMemberCell.ColIndex];
							}
							context.RenderingReport.Labels.Add(rPLTablixMemberCell.UniqueName, new LabelPoint(x, num2));
						}
					}
				}
				int num5 = int.MaxValue;
				for (int l = 0; l < nextRow.NumCells; l++)
				{
					RPLTablixCell rPLTablixCell = nextRow[l];
					RPLItemMeasurement rPLItemMeasurement = new RPLItemMeasurement();
					rPLItemMeasurement.Element = rPLTablixCell.Element;
					rPLItemMeasurement.Left = array[rPLTablixCell.ColIndex];
					rPLItemMeasurement.Top = num;
					rPLItemMeasurement.Width = rPLTablix.GetColumnWidth(rPLTablixCell.ColIndex, rPLTablixCell.ColSpan);
					rPLItemMeasurement.Height = rPLTablix.GetRowHeight(rPLTablixCell.RowIndex, rPLTablixCell.RowSpan);
					RenderingItem renderingItem = RenderingItem.CreateRenderingItem(context, rPLItemMeasurement, bounds);
					if (renderingItem == null)
					{
						rowCells.Add(string.Empty);
						continue;
					}
					rowCells.Add(renderingItem is RenderingTextBox textBox ? textBox.GetText() ?? string.Empty : string.Empty);
					if (renderingItem is RenderingDynamicImage)
					{
						((RenderingDynamicImage)renderingItem).Sizing = RPLFormat.Sizings.Fit;
					}
					RPLTablixMemberCell rPLTablixMemberCell2 = rPLTablixCell as RPLTablixMemberCell;
					if (rPLTablixMemberCell2 != null && !string.IsNullOrEmpty(rPLTablixMemberCell2.GroupLabel))
					{
						context.RenderingReport.Labels.Add(rPLTablixMemberCell2.UniqueName, new LabelPoint(rPLItemMeasurement.Left, rPLItemMeasurement.Top));
					}
					if (renderingItem.HasBorders)
					{
						renderingItem.DelayDrawBorders = true;
						if (num3 < rPLTablix.ColumnHeaderRows && rPLTablixCell is RPLTablixCornerCell)
						{
							m_cornerBorders.Add(new RenderingItemBorderTablix(0, 0, rPLTablixCell.RowIndex, rPLTablixCell.ColIndex, renderingItem));
						}
						else
						{
							if (num5 == int.MaxValue)
							{
								num5 = SharedRenderer.CalculateRowZIndex(nextRow);
							}
							if (num5 == int.MaxValue)
							{
								num5 = num4;
							}
							RenderingItemBorderTablix renderingItemBorderTablix = new RenderingItemBorderTablix(num5, array2[rPLTablixCell.ColIndex], rPLTablixCell.RowIndex, rPLTablixCell.ColIndex, renderingItem);
							if (rPLTablixMemberCell2 != null)
							{
								if (num3 < rPLTablix.ColumnHeaderRows)
								{
									m_columnHeaderBorders.Add(renderingItemBorderTablix);
								}
								else
								{
									renderingItemBorderTablix.CompareRowFirst = false;
									m_rowHeaderBorders.Add(renderingItemBorderTablix);
								}
							}
							else
							{
								m_detailCellBorders.Add(renderingItemBorderTablix);
							}
						}
					}
					base.Children.Add(renderingItem);
					fixedHeaderItem?.RenderingItems.Add(renderingItem);
					if (rPLTablix.FixedColumns[rPLTablixCell.ColIndex])
					{
						fixedHeaderItem3.RenderingItems.Add(renderingItem);
					}
					if (fixedHeaderItem2 != null && rPLTablixCell is RPLTablixCornerCell)
					{
						fixedHeaderItem2.RenderingItems.Add(renderingItem);
						fixedHeaderItem2.Bounds = renderingItem.Position;
					}
				}
				context.RenderingReport.AddTablixRowTargetWithCells(
					new RectangleF(bounds.X, bounds.Top + num, GetTablixWidth(rPLTablix), rPLTablix.RowHeights[num3]),
					num3 < rPLTablix.ColumnHeaderRows,
					num3,
					source,
					sourceOrder,
					rowCells);
				num4 = num5;
				if (fixedHeaderItem != null)
				{
					fixedHeaderItem.Bounds.Height += rPLTablix.RowHeights[num3];
					if (num3 == rPLTablix.RowHeights.Length - 1 || !rPLTablix.FixedRow(num3 + 1))
					{
						fixedHeaderItem = null;
					}
				}
				num += rPLTablix.RowHeights[num3];
				num2 += rPLTablix.RowHeights[num3];
				num3++;
				for (int m = 0; m < nextRow.NumCells; m++)
				{
					nextRow[m] = null;
				}
			}
			m_detailCellBorders.Sort(new ZIndexComparer());
			m_columnHeaderBorders.Sort(new ZIndexComparer());
			m_rowHeaderBorders.Sort(new ZIndexComparer());
			m_cornerBorders.Sort(new ZIndexComparer());
			RectangleF position = base.Position;
			if (array.Length != 0)
			{
				position.Width = array[array.Length - 1] + rPLTablix.ColumnWidths[rPLTablix.ColumnWidths.Length - 1];
			}
			position.Height = num;
		}

		private static bool IsValidRowHeight(float height)
		{
			return !float.IsNaN(height) && !float.IsInfinity(height) && height > 0f;
		}

		private static float GetTablixWidth(RPLTablix tablix)
		{
			if (tablix.ColumnWidths == null || tablix.ColumnWidths.Length == 0)
			{
				return 0f;
			}
			float width = 0f;
			for (int i = 0; i < tablix.ColumnWidths.Length; i++)
			{
				if (float.IsNaN(tablix.ColumnWidths[i]) || float.IsInfinity(tablix.ColumnWidths[i]) || tablix.ColumnWidths[i] < 0f)
				{
					return 0f;
				}
				width += tablix.ColumnWidths[i];
			}
			return width;
		}

		internal override void DrawContent(GdiContext context)
		{
			base.DrawContent(context);
			DrawBorders(context, m_detailCellBorders);
			DrawBorders(context, m_columnHeaderBorders);
			DrawBorders(context, m_rowHeaderBorders);
			DrawBorders(context, m_cornerBorders);
		}

		private void DrawBorders(GdiContext context, List<RenderingItemBorderTablix> borders)
		{
			foreach (RenderingItemBorderTablix border in borders)
			{
				border.Item.DrawBorders(context);
			}
		}

		private int CalculateZIndex(RPLTablixMemberCell header, bool forHeader)
		{
			int num = header.TablixMemberDef.Level * 3;
			if (!header.TablixMemberDef.StaticHeadersTree)
			{
				num--;
			}
			if (!forHeader)
			{
				num--;
			}
			return num;
		}
	}
}
