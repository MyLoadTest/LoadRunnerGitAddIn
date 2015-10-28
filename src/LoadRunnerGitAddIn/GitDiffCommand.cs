using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public sealed class GitDiffCommand : GitCommand
    {
        protected override void RunInternal(string fileName, Action callback)
        {
            GitGuiWrapper.Diff(fileName, callback);
        }
    }
}