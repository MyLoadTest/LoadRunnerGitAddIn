using System;
using System.Linq;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public enum GitStatus
    {
        None,
        Added,
        Modified,
        Deleted,
        OK,
        Ignored
    }
}