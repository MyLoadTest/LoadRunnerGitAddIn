// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public class RegisterEventsCommand : AbstractCommand
    {
        public override void Run()
        {
            FileService.FileCreated += OnFileCreated;
            FileService.FileCopied += OnFileCopied;
            FileService.FileRemoved += OnFileRemoved;
            FileService.FileRenamed += OnFileRenamed;

            FileUtility.FileSaved += OnFileSaved;

            AbstractProjectBrowserTreeNode.OnNewNode += OnNewTreeNodeCreated;
        }

        private static void ClearCachedStatusAndEnqueueParents(string fileName)
        {
            GitStatusCache.ClearCachedStatus(fileName);

            var node = ProjectBrowserPad.Instance?.ProjectBrowserControl?.FindFileNode(fileName);
            if (node != null)
            {
                OverlayIconManager.EnqueueParents(node);
            }
        }

        private static void AddFile(string fileName)
        {
            Git.Add(
                fileName,
                exitCode => WorkbenchSingleton.SafeThreadAsyncCall(ClearCachedStatusAndEnqueueParents, fileName));
        }

        private static void RemoveFile(string fileName)
        {
            if (GitStatusCache.GetFileStatus(fileName) == GitStatus.Added)
            {
                Git.Remove(
                    fileName,
                    true,
                    exitcode => WorkbenchSingleton.SafeThreadAsyncCall(ClearCachedStatusAndEnqueueParents, fileName));
            }
        }

        private static void RenameFile(string sourceFileName, string targetFileName)
        {
            Git.Add(
                targetFileName,
                exitcode => WorkbenchSingleton.SafeThreadAsyncCall(RemoveFile, sourceFileName));

            WorkbenchSingleton.SafeThreadAsyncCall(ClearCachedStatusAndEnqueueParents, targetFileName);
        }

        private static void HandleNode(AbstractProjectBrowserTreeNode node)
        {
            if (node == null)
            {
                return;
            }

            var solutionNode = node as SolutionNode;
            if (solutionNode != null)
            {
                GitStatusCache.ClearCachedStatus(solutionNode.Solution.FileName);
                OverlayIconManager.EnqueueRecursive(solutionNode);
                return;
            }

            OverlayIconManager.Enqueue(node);
        }

        private static void OnFileCreated(object sender, FileEventArgs e)
        {
            AddFile(e.FileName);
        }

        private static void OnFileCopied(object sender, FileRenameEventArgs e)
        {
            AddFile(e.TargetFile);
        }

        private static void OnFileRemoved(object sender, FileEventArgs e)
        {
            RemoveFile(e.FileName);
        }

        private static void OnFileRenamed(object sender, FileRenameEventArgs e)
        {
            RenameFile(e.SourceFile, e.TargetFile);
        }

        private static void OnFileSaved(object sender, FileNameEventArgs e)
        {
            ClearCachedStatusAndEnqueueParents(e.FileName);
        }

        private static void OnNewTreeNodeCreated(object sender, TreeViewEventArgs e)
        {
            HandleNode(e.Node as AbstractProjectBrowserTreeNode);
        }
    }
}