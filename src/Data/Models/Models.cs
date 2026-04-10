namespace MRVA.Reports.Data.Models;

public sealed record Analysis
{
    public int RowId { get; init; }
    public string ToolName { get; init; } = string.Empty;
    public string ToolVersion { get; init; } = string.Empty;
    public string AnalysisId { get; init; } = string.Empty;
    public string ControllerRepo { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string QueryLanguage { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string CompletedAt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string FailureReason { get; init; } = string.Empty;
    public int ScannedReposCount { get; init; }
    public int SkippedReposCount { get; init; }
    public int NotFoundReposCount { get; init; }
    public int NoCodeqlDbReposCount { get; init; }
    public int OverLimitReposCount { get; init; }
    public long ActionsWorkflowRunId { get; init; }
    public int TotalReposCount { get; init; }
}

public sealed record Repository
{
    public int RowId { get; init; }
    public string RepositoryFullName { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = string.Empty;
    public string AnalysisStatus { get; init; } = string.Empty;
    public int ResultCount { get; init; }
    public int ArtifactSizeInBytes { get; init; }
    public string AnalysisId { get; init; } = string.Empty;
}

public sealed record Rule
{
    public int RowId { get; init; }
    public string Id { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public string RuleDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> PropertyTags { get; init; } = [];
    public string Kind { get; init; } = string.Empty;
    public string SeverityLevel { get; init; } = string.Empty;
}

public sealed record Alert
{
    public int RowId { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int StartColumn { get; init; }
    public int EndLine { get; init; }
    public int EndColumn { get; init; }
    public string CodeSnippetSource { get; init; } = string.Empty;
    public string CodeSnippetSink { get; init; } = string.Empty;
    public string CodeSnippet { get; init; } = string.Empty;
    public string CodeSnippetContext { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string ResultFingerprint { get; init; } = string.Empty;
    public int StepCount { get; init; }
    public int RepositoryRowId { get; init; }
    public int AnalysisRowId { get; init; }
    public int RuleRowId { get; init; }
}
