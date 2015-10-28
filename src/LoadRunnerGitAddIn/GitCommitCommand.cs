using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public sealed class GitCommitCommand : GitCommand
    {
        protected override void RunInternal(string fileName, Action callback)
        {
            GitGuiWrapper.Commit(fileName, callback);
        }
    }
}