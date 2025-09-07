using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections;

namespace Tests;

public class TestLogger : TaskLoggingHelper
{
    private readonly TestBuildEngine _buildEngine;
    public TestLogger() : base(CreateTask(out var buildEngine))
        => _buildEngine = buildEngine;

    static TestTask CreateTask(out TestBuildEngine buildEngine)
    {
        buildEngine = new TestBuildEngine();
        return new TestTask(buildEngine);
    }

    public List<BuildMessageEventArgs> Messages => _buildEngine.Messages;
    public List<BuildWarningEventArgs> Warnings => _buildEngine.Warnings;
    public List<BuildErrorEventArgs> Errors => _buildEngine.Errors;
    public List<CustomBuildEventArgs> CustomEvents => _buildEngine.CustomEvents;
}

public class TestTask : ITask
{
    public TestTask(IBuildEngine buildEngine)
        => BuildEngine = buildEngine;
    public IBuildEngine BuildEngine { get; set; }
    public ITaskHost HostObject { get; set; } = null!;
    public bool Execute() => true;
}

public class TestBuildEngine : IBuildEngine
{
    public List<BuildMessageEventArgs> Messages { get; } = [];
    public List<BuildWarningEventArgs> Warnings { get; } = [];
    public List<BuildErrorEventArgs> Errors { get; } = [];
    public List<CustomBuildEventArgs> CustomEvents { get; } = [];

    public bool BuildProjectFile(
        string projectFileName,
        string[] targetNames,
        IDictionary globalProperties,
        IDictionary targetOutputs) => true;
    public int ColumnNumberOfTaskNode => 0;
    public bool ContinueOnError => false;
    public int LineNumberOfTaskNode => 0;
    public string ProjectFileOfTaskNode => "test.proj";
    public void LogCustomEvent(CustomBuildEventArgs e) => CustomEvents.Add(e);
    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);
    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
