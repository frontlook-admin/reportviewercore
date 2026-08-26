using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportClipboardOptions
    {
        public bool IncludeHeaders { get; set; }
        public bool PreserveFormatting { get; set; }
        public IReadOnlyList<string> Headers { get; set; }
        public int MaxCellCharacters { get; set; } = 1_000_000;
    }

    public sealed class ReportClipboardPayload
    {
        private ReportClipboardPayload(string text, string tsv, string csv, string html, string diagnostics)
        {
            Text = text;
            Tsv = tsv;
            Csv = csv;
            Html = html;
            Diagnostics = diagnostics;
        }

        public string Text { get; }
        public string Tsv { get; }
        public string Csv { get; }
        public string Html { get; }
        public string Diagnostics { get; }

        public static ReportClipboardPayload Create(
            IEnumerable<IReadOnlyList<string>> rows,
            ReportClipboardOptions options = null)
        {
            options ??= new ReportClipboardOptions();
            var normalized = new List<IReadOnlyList<string>>();
            if (options.IncludeHeaders && options.Headers != null && options.Headers.Count > 0)
            {
                normalized.Add(options.Headers);
            }

            foreach (var row in rows ?? Enumerable.Empty<IReadOnlyList<string>>())
            {
                normalized.Add((row ?? Array.Empty<string>()).Select(value => NormalizeCell(value, options)).ToArray());
            }

            if (normalized.Count == 0 || normalized.All(row => row.Count == 0))
            {
                return new ReportClipboardPayload(string.Empty, string.Empty, string.Empty, string.Empty, "No report cells selected.");
            }

            var tsv = string.Join(Environment.NewLine, normalized.Select(row => string.Join("\t", row)));
            var csv = string.Join(Environment.NewLine, normalized.Select(row => string.Join(",", row.Select(EscapeCsv))));
            var html = BuildHtml(normalized);
            return new ReportClipboardPayload(tsv, tsv, csv, html, $"Copied {normalized.Count} row(s), {normalized.Sum(row => row.Count)} cell(s).");
        }

        public DataObject ToDataObject()
        {
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, Text);
            data.SetData(DataFormats.Text, Text);
            if (!string.IsNullOrEmpty(Tsv))
            {
                data.SetData("text/tab-separated-values", Tsv);
                data.SetData("Csv", Csv);
                data.SetData(DataFormats.CommaSeparatedValue, Csv);
                data.SetData(DataFormats.Html, Html);
            }
            return data;
        }

        private static string NormalizeCell(string value, ReportClipboardOptions options)
        {
            var normalized = (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " ");
            if (options.MaxCellCharacters > 0 && normalized.Length > options.MaxCellCharacters)
            {
                normalized = normalized.Substring(0, options.MaxCellCharacters);
            }
            return normalized;
        }

        private static string EscapeCsv(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string BuildHtml(IEnumerable<IReadOnlyList<string>> rows)
        {
            var builder = new StringBuilder("<table>");
            foreach (var row in rows)
            {
                builder.Append("<tr>");
                foreach (var cell in row)
                {
                    builder.Append("<td>").Append(WebUtility.HtmlEncode(cell)).Append("</td>");
                }
                builder.Append("</tr>");
            }
            return builder.Append("</table>").ToString();
        }
    }
}
