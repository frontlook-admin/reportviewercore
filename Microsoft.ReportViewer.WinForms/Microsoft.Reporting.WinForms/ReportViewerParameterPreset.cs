using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>A named, report-scoped set of non-sensitive parameter values.</summary>
    public sealed class ReportViewerParameterPreset
    {
        private static readonly string[] SensitiveNameParts =
        {
            "password", "secret", "token", "credential", "privatekey", "apikey", "api_key"
        };

        public string Name { get; set; }

        public Dictionary<string, string[]> Values { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public ReportViewerParameterPreset()
        {
        }

        public ReportViewerParameterPreset(string name, IDictionary<string, string[]> values)
        {
            Name = ValidateName(name);
            Values = CloneValues(values);
        }

        public static ReportViewerParameterPreset CreateSafe(string name, IDictionary<string, string[]> values)
        {
            return new ReportViewerParameterPreset(name, values);
        }

        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        public static ReportViewerParameterPreset FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("The parameter preset is empty.");
            }

            var preset = JsonSerializer.Deserialize<ReportViewerParameterPreset>(json)
                ?? throw new InvalidDataException("The parameter preset is invalid.");
            preset.Name = ValidateName(preset.Name);
            preset.Values = CloneValues(preset.Values);
            return preset;
        }

        internal static bool IsSensitiveName(string name)
        {
            var normalized = new string((name ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            return SensitiveNameParts.Any(normalized.Contains);
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A parameter preset name is required.", nameof(name));
            }

            return name.Trim();
        }

        private static Dictionary<string, string[]> CloneValues(IDictionary<string, string[]> values)
        {
            var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in values ?? throw new ArgumentNullException(nameof(values)))
            {
                if (string.IsNullOrWhiteSpace(item.Key) || IsSensitiveName(item.Key))
                {
                    continue;
                }

                result[item.Key] = (item.Value ?? Array.Empty<string>()).ToArray();
            }

            return result;
        }
    }
}