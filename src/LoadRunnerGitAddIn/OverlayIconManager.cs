﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.Core.WinForms;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using MyLoadTest.LoadRunnerGitAddIn.Properties;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public static class OverlayIconManager
    {
        private static readonly Queue<AbstractProjectBrowserTreeNode> queue = new Queue<AbstractProjectBrowserTreeNode>();
        private static readonly HashSet<AbstractProjectBrowserTreeNode> inQueue = new HashSet<AbstractProjectBrowserTreeNode>();
        private static bool threadRunning;

        public static void Enqueue(AbstractProjectBrowserTreeNode node)
        {
            lock (queue)
            {
                if (inQueue.Add(node))
                {
                    queue.Enqueue(node);
                    if (!threadRunning)
                    {
                        threadRunning = true;
                        ThreadPool.QueueUserWorkItem(Run);
                    }
                }
            }
        }

        public static void EnqueueRecursive(AbstractProjectBrowserTreeNode node)
        {
            lock (queue)
            {
                if (inQueue.Add(node))
                    queue.Enqueue(node);

                // use breadth-first search
                Queue<AbstractProjectBrowserTreeNode> q = new Queue<AbstractProjectBrowserTreeNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    node = q.Dequeue();
                    foreach (TreeNode n in node.Nodes)
                    {
                        node = n as AbstractProjectBrowserTreeNode;
                        if (node != null)
                        {
                            q.Enqueue(node);
                            if (inQueue.Add(node))
                                queue.Enqueue(node);
                        }
                    }
                }

                if (!threadRunning)
                {
                    threadRunning = true;
                    ThreadPool.QueueUserWorkItem(Run);
                }
            }
        }

        public static void EnqueueParents(AbstractProjectBrowserTreeNode node)
        {
            lock (queue)
            {
                while (node != null)
                {
                    if (inQueue.Add(node))
                        queue.Enqueue(node);
                    node = node.Parent as AbstractProjectBrowserTreeNode;
                }

                if (!threadRunning)
                {
                    threadRunning = true;
                    ThreadPool.QueueUserWorkItem(Run);
                }
            }
        }

        private static void Run(object state)
        {
            LoggingService.Debug("Git: OverlayIconManager Thread started");

            // sleep a tiny bit to give main thread time to add more jobs to the queue
            Thread.Sleep(100);
            while (true)
            {
                if (ICSharpCode.SharpDevelop.ParserService.LoadSolutionProjectsThreadRunning)
                {
                    // Run OverlayIconManager much more slowly while solution is being loaded.
                    // This prevents the disk from seeking too much
                    Thread.Sleep(100);
                }
                AbstractProjectBrowserTreeNode node;
                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        LoggingService.Debug("Git: OverlayIconManager Thread finished");
                        Debug.Assert(inQueue.Count == 0);
                        inQueue.Clear();
                        threadRunning = false;
                        return;
                    }
                    node = queue.Dequeue();
                    inQueue.Remove(node);
                }
                try
                {
                    RunStep(node);
                }
                catch (Exception ex)
                {
                    MessageService.ShowException(ex);
                }
            }
        }

        private static void RunStep(AbstractProjectBrowserTreeNode node)
        {
            if (node.IsDisposed)
                return;

            FileNode fileNode = node as FileNode;
            GitStatus status;
            if (fileNode != null)
            {
                status = GitStatusCache.GetFileStatus(fileNode.FileName);
            }
            else
            {
                DirectoryNode directoryNode = node as DirectoryNode;
                if (directoryNode != null)
                {
                    status = GitStatusCache.GetFileStatus(directoryNode.Directory);
                }
                else
                {
                    SolutionNode solNode = node as SolutionNode;
                    if (solNode != null)
                    {
                        status = GitStatusCache.GetFileStatus(solNode.Solution.Directory);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            WorkbenchSingleton.SafeThreadAsyncCall(
                delegate
                {
                    Image image = GetImage(status);
                    if (image != null)
                    {
                        node.Overlay = image;
                    }
                    else if (node.Overlay != null && (node.Overlay.Tag as Type) == typeof(OverlayIconManager))
                    {
                        // reset overlay to null only if the old overlay belongs to the OverlayIconManager
                        node.Overlay = null;
                    }
                });
        }

        public static Image GetImage(GitStatus status)
        {
            switch (status)
            {
                case GitStatus.Added:
                    return GetImage(StatusIcon.Added);

                case GitStatus.Deleted:
                    return GetImage(StatusIcon.Deleted);

                case GitStatus.Modified:
                    return GetImage(StatusIcon.Modified);

                case GitStatus.OK:
                    return GetImage(StatusIcon.OK);

                default:
                    return null;
            }
        }

        #region SVN icons

        private static Bitmap statusImages;

        public static Bitmap StatusImages
        {
            get
            {
                if (statusImages == null)
                {
                    statusImages = Resources.SvnStatusImages;
                }

                return statusImages;
            }
        }

        private enum StatusIcon
        {
            Empty = 0,
            OK,
            Added,
            Deleted,
            Info,
            Empty2,
            Exclamation,
            PropertiesModified,
            Unknown,
            Modified
        }

        private static Image[] statusIcons = new Image[10];

        private static Image GetImage(StatusIcon status)
        {
            int index = (int)status;
            if (statusIcons[index] == null)
            {
                Bitmap statusImages = StatusImages;
                Bitmap smallImage = new Bitmap(7, 10);
                using (Graphics g = Graphics.FromImage(smallImage))
                {
                    //g.DrawImageUnscaled(statusImages, -index * 7, -3);
                    Rectangle srcRect = new Rectangle(index * 7, 3, 7, 10);
                    Rectangle destRect = new Rectangle(0, 0, 7, 10);
                    g.DrawImage(statusImages, destRect, srcRect, GraphicsUnit.Pixel);

                    //g.DrawLine(Pens.Black, 0, 0, 7, 10);
                }
                smallImage.Tag = typeof(OverlayIconManager);
                statusIcons[index] = smallImage;
            }
            return statusIcons[index];
        }

        #endregion
    }
}