using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace CliReportCompiler;

/// <summary>
/// Immutable command-line state captured for one report operation.
/// </summary>
public sealed record ReportCompilerOptions
{
    public ReportCompilerOptions(
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, string> subReports)
    {
        Parameters = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase));
        SubReports = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(subReports, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<string, string> Parameters { get; }

    public IReadOnlyDictionary<string, string> SubReports { get; }

    internal ReportCompilerUtility.ExecutionState ToExecutionState()
    {
        var state = new ReportCompilerUtility.ExecutionState();
        foreach (var parameter in Parameters)
        {
            state.Parameters[parameter.Key] = parameter.Value;
        }

        foreach (var subReport in SubReports)
        {
            state.SubReports[subReport.Key] = subReport.Value;
        }

        return state;
    }
}

/// <summary>
/// Reentrant runner for one immutable CLI options snapshot.
/// </summary>
public sealed class ReportCompilerRunner
{
    public ReportCompilerRunner(ReportCompilerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ReportCompilerOptions Options { get; }

    public static ReportCompilerRunner Parse(string[] args)
    {
        if (!ReportCompilerUtility.ParseArguments(args))
        {
            throw new ArgumentException("The supplied arguments do not describe a report operation.", nameof(args));
        }

        return new ReportCompilerRunner(new ReportCompilerOptions(
            ReportCompilerUtility.GetCurrentParameters(),
            ReportCompilerUtility.GetCurrentSubReports()));
    }

    public static ReportCompilerRunner TryParse(string[] args)
    {
        return ReportCompilerUtility.ParseArguments(args)
            ? new ReportCompilerRunner(new ReportCompilerOptions(
                ReportCompilerUtility.GetCurrentParameters(),
                ReportCompilerUtility.GetCurrentSubReports()))
            : null;
    }

    public void Execute(CancellationToken cancellationToken = default)
    {
        ReportCompilerUtility.Execute(Options, cancellationToken);
    }

    public void ValidateInputs(CancellationToken cancellationToken = default)
    {
        ReportCompilerUtility.ValidateInputs(Options, cancellationToken);
    }
}
