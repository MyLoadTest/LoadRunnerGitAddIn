// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Util;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public static class GitStatusCache
    {
        private static readonly Regex StatusParseRegex = new Regex(@"^([DMA ][DMA ])\s(\S.*)$");
        private static readonly List<KeyValuePair<string, GitStatusSet>> StatusSetMap = new List<KeyValuePair<string, GitStatusSet>>();

        public static void ClearCachedStatus(string fileName)
        {
            lock (StatusSetMap)
            {
                for (var i = 0; i < StatusSetMap.Count; i++)
                {
                    if (FileUtility.IsBaseDirectory(StatusSetMap[i].Key, fileName))
                    {
                        StatusSetMap.RemoveAt(i--);
                    }
                }
            }
        }

        public static GitStatus GetFileStatus(string fileName)
        {
            var wcroot = Git.FindWorkingCopyRoot(fileName);
            if (wcroot == null)
                return GitStatus.None;
            var gss = GetStatusSet(wcroot);
            return gss.GetStatus(Git.AdaptFileNameNoQuotes(wcroot, fileName));
        }

        public static GitStatusSet GetStatusSet(string wcRoot)
        {
            lock (StatusSetMap)
            {
                GitStatusSet statusSet;
                foreach (var pair in StatusSetMap)
                {
                    if (FileUtility.IsEqualFileName(pair.Key, wcRoot))
                        return pair.Value;
                }

                statusSet = new GitStatusSet();
                GitGetFiles(wcRoot, statusSet);
                GitGetStatus(wcRoot, statusSet);
                StatusSetMap.Add(new KeyValuePair<string, GitStatusSet>(wcRoot, statusSet));
                return statusSet;
            }
        }

        private static void GitGetFiles(string wcRoot, GitStatusSet statusSet)
        {
            var git = Git.FindGit();
            if (git == null)
                return;

            var runner = new ProcessRunner();
            runner.WorkingDirectory = wcRoot;
            runner.LogStandardOutputAndError = false;
            runner.OutputLineReceived += delegate (object sender, LineReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Line))
                {
                    statusSet.AddEntry(e.Line, GitStatus.OK);
                }
            };

            var command = "ls-files";
            var hasErrors = false;
            runner.ErrorLineReceived += delegate (object sender, LineReceivedEventArgs e)
            {
                if (!hasErrors)
                {
                    hasErrors = true;
                    GitMessageView.AppendLine(runner.WorkingDirectory + "> git " + command);
                }
                GitMessageView.AppendLine(e.Line);
            };
            runner.Start(git, command);
            runner.WaitForExit();
        }

        private static void GitGetStatus(string wcRoot, GitStatusSet statusSet)
        {
            var git = Git.FindGit();
            if (git == null)
                return;

            var command = "status --porcelain --untracked-files=no";
            var hasErrors = false;

            var runner = new ProcessRunner();
            runner.WorkingDirectory = wcRoot;
            runner.LogStandardOutputAndError = false;
            runner.OutputLineReceived += delegate (object sender, LineReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Line))
                {
                    var m = StatusParseRegex.Match(e.Line);
                    if (m.Success)
                    {
                        statusSet.AddEntry(m.Groups[2].Value, StatusFromText(m.Groups[1].Value));
                    }
                    else
                    {
                        if (!hasErrors)
                        {
                            // in front of first output line, print the command line we invoked
                            hasErrors = true;
                            GitMessageView.AppendLine(runner.WorkingDirectory + "> git " + command);
                        }
                        GitMessageView.AppendLine("unknown output: " + e.Line);
                    }
                }
            };
            runner.ErrorLineReceived += delegate (object sender, LineReceivedEventArgs e)
            {
                if (!hasErrors)
                {
                    hasErrors = true;
                    GitMessageView.AppendLine(runner.WorkingDirectory + "> git " + command);
                }
                GitMessageView.AppendLine(e.Line);
            };
            runner.Start(git, command);
            runner.WaitForExit();
        }

        private static GitStatus StatusFromText(string text)
        {
            if (text.Contains("A"))
                return GitStatus.Added;
            if (text.Contains("D"))
                return GitStatus.Deleted;
            if (text.Contains("M"))
                return GitStatus.Modified;
            return GitStatus.None;
        }
    }
}