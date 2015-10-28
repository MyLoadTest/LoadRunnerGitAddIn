// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Project;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    /// <summary>
    /// Description of IsUnderGitControlCondition.
    /// </summary>
    public class IsUnderGitControlCondition : IConditionEvaluator
    {
        public bool IsValid(object caller, Condition condition)
        {
            var path = ProjectBrowserPad.Instance?.SelectedNode?.GetNodeFileSystemPath();
            return !string.IsNullOrEmpty(path) && Git.IsInWorkingCopy(path);
        }
    }
}