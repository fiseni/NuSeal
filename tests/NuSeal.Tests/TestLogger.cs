using Microsoft.Build.Framework;
using System.Collections;

namespace Tests;

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
