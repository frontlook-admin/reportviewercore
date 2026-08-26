using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportViewerParameterValidationError
    {
        public ReportViewerParameterValidationError(string parameterName, string code, string message)
        {
            ParameterName = parameterName ?? string.Empty;
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public string ParameterName { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public sealed class ReportViewerParameterValidationResult
    {
        private ReportViewerParameterValidationResult(IEnumerable<ReportViewerParameterValidationError> errors)
        {
            Errors = new ReadOnlyCollection<ReportViewerParameterValidationError>((errors ?? Enumerable.Empty<ReportViewerParameterValidationError>()).ToList());
        }

        public IReadOnlyList<ReportViewerParameterValidationError> Errors { get; }
        public bool IsValid => Errors.Count == 0;

        internal static ReportViewerParameterValidationResult From(params ReportViewerParameterValidationError[] errors) => new ReportViewerParameterValidationResult(errors);
    }

    public static class ReportViewerParameterValidator
    {
        public static ReportViewerParameterValidationResult ValidateRequired(string parameterName, IEnumerable<string> values, bool allowBlank, bool nullable, bool multiValue)
        {
            var items = (values ?? Enumerable.Empty<string>()).ToArray();
            var hasValue = multiValue ? items.Any(value => !string.IsNullOrWhiteSpace(value)) : items.Length > 0 && (allowBlank || !string.IsNullOrWhiteSpace(items[0]));
            return hasValue || nullable
                ? ReportViewerParameterValidationResult.From()
                : ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "required", $"Parameter '{parameterName}' is required."));
        }

        public static ReportViewerParameterValidationResult ValidateMultiValue(string parameterName, IEnumerable<string> values, bool multiValue, int minimumSelections = 0, IEnumerable<string> validValues = null)
        {
            var items = (values ?? Enumerable.Empty<string>()).Where(value => value != null).ToArray();
            if (!multiValue && items.Length > 1)
            {
                return ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "single-value", $"Parameter '{parameterName}' accepts one value."));
            }
            if (items.Length < Math.Max(0, minimumSelections))
            {
                return ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "minimum-selections", $"Parameter '{parameterName}' requires at least {minimumSelections} selection(s)."));
            }
            if (validValues != null)
            {
                var allowed = new HashSet<string>(validValues, StringComparer.OrdinalIgnoreCase);
                var invalid = items.FirstOrDefault(value => !allowed.Contains(value));
                if (invalid != null)
                {
                    return ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "invalid-value", $"Value '{invalid}' is not valid for parameter '{parameterName}'."));
                }
            }
            return ReportViewerParameterValidationResult.From();
        }

        public static ReportViewerParameterValidationResult ValidateDateRange(string parameterName, string start, string end)
        {
            if (!DateTimeOffset.TryParse(start, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var startDate)
                || !DateTimeOffset.TryParse(end, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var endDate))
            {
                return ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "date", $"Parameter '{parameterName}' must contain valid dates."));
            }
            return startDate <= endDate
                ? ReportViewerParameterValidationResult.From()
                : ReportViewerParameterValidationResult.From(new ReportViewerParameterValidationError(parameterName, "date-range", $"Parameter '{parameterName}' start date must not be after its end date."));
        }
    }

    public enum ReportViewerDiagnosticSeverity { Info, Warning, Error }

    public sealed class ReportViewerDiagnostic
    {
        public ReportViewerDiagnostic(ReportViewerDiagnosticSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
        public ReportViewerDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public sealed class ReportViewerDiagnosticPanel
    {
        private readonly List<ReportViewerDiagnostic> m_items = new List<ReportViewerDiagnostic>();
        public IReadOnlyList<ReportViewerDiagnostic> Items => new ReadOnlyCollection<ReportViewerDiagnostic>(m_items);
        public ReportViewerDiagnosticSeverity? HighestSeverity => m_items.Count == 0 ? (ReportViewerDiagnosticSeverity?)null : m_items.Max(item => item.Severity);
        public void Add(ReportViewerDiagnosticSeverity severity, string code, string message) => m_items.Add(new ReportViewerDiagnostic(severity, code, message));
        public void Clear() => m_items.Clear();
        public string ExportSafeText() => string.Join(Environment.NewLine, m_items.Select(item => $"[{item.Severity}] {item.Code}: {Redact(item.Message)}"));
        private static string Redact(string value) => Regex.Replace(value ?? string.Empty, @"(?i)(password|secret|token|credential|api[_-]?key)\s*[=:]\s*[^\s,;]+", "$1=[REDACTED]");
    }
}
