using System;
using System.Runtime.Serialization;

namespace Microsoft.Reporting.WinForms;

[Serializable]
public sealed class ReportSecurityException : Exception
{
    public ReportSecurityException(string message) : base(message) { }

    private ReportSecurityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}