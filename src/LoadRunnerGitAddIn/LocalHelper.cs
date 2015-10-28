using System;
using System.Linq;
using HP.LR.Vugen.BackEnd.Project.ProjectSystem.ScriptItems;
using HP.Utt.ProjectSystem;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    internal static class LocalHelper
    {
        #region Public Methods

        public static string GetNodeFileSystemPath(this ExtTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.IsDisposed)
            {
                return null;
            }

            var path = ((node as UttBaseTreeNode)?.Item as FileBasedScriptItem)?.FullFileName
                ?? (node as FileNode)?.FileName
                    ?? (node as SolutionNode)?.Solution?.Directory
                        ?? (node as UttProjectNode)?.Project?.FileName
                            ?? (node as DirectoryNode)?.Directory;

            return path;
        }

        public static GitStatus? GetNodeStatus(this ExtTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.IsDisposed)
            {
                return null;
            }

            var path = GetNodeFileSystemPath(node);

            return string.IsNullOrEmpty(path) ? default(GitStatus?) : GitStatusCache.GetFileStatus(path);
        }

        #endregion
    }
}