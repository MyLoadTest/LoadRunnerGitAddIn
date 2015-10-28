// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using MyLoadTest.LoadRunnerGitAddIn.Properties;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public abstract class GitCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var node = ProjectBrowserPad.Instance?.SelectedNode;

            var nodeFileName = node?.GetNodeFileSystemPath();
            if (nodeFileName == null)
            {
                return;
            }

            var unsavedFiles = FileService.OpenedFiles
                .Where(
                    file =>
                        file.IsDirty && !file.IsUntitled && !string.IsNullOrEmpty(file.FileName)
                            && !FileUtility.IsUrl(file.FileName)
                            && FileUtility.IsBaseDirectory(nodeFileName, file.FileName))
                .ToArray();

            if (unsavedFiles.Length != 0)
            {
                if (MessageService.ShowCustomDialog(
                    MessageService.DefaultMessageBoxTitle,
                    Resources.SaveUnsavedFilesConfirmationMessage,
                    0,
                    1,
                    Resources.SaveFilesButtonText,
                    Resources.CancelButtonText) != 0)
                {
                    return;
                }

                foreach (var file in unsavedFiles)
                {
                    ICSharpCode.SharpDevelop.Commands.SaveFile.Save(file);
                }
            }

            // now run the actual operation:
            RunInternal(nodeFileName, CreateAfterCommandCallback(nodeFileName, node));
        }

        protected abstract void RunInternal(string fileName, Action callback);

        private static Action CreateAfterCommandCallback(string nodeFileName, AbstractProjectBrowserTreeNode node)
        {
            return delegate
            {
                WorkbenchSingleton.AssertMainThread();
                // and then refresh the project browser:
                GitStatusCache.ClearCachedStatus(nodeFileName);
                OverlayIconManager.EnqueueRecursive(node);
                OverlayIconManager.EnqueueParents(node);
            };
        }
    }
}