// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;
using MyLoadTest.LoadRunnerGitAddIn.Properties;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public static class OverlayIconManager
    {
        private static readonly Queue<AbstractProjectBrowserTreeNode> Queue = new Queue<AbstractProjectBrowserTreeNode>();
        private static readonly HashSet<AbstractProjectBrowserTreeNode> InQueue = new HashSet<AbstractProjectBrowserTreeNode>();
        private static readonly Image[] StatusIcons = new Image[10];
        private static readonly Bitmap StatusImages = Resources.SvnStatusImages;
        private static bool _threadRunning;

        public static void Enqueue(AbstractProjectBrowserTreeNode node)
        {
            lock (Queue)
            {
                if (InQueue.Add(node))
                {
                    Queue.Enqueue(node);
                    if (!_threadRunning)
                    {
                        _threadRunning = true;
                        ThreadPool.QueueUserWorkItem(Run);
                    }
                }
            }
        }

        public static void EnqueueRecursive(AbstractProjectBrowserTreeNode node)
        {
            lock (Queue)
            {
                if (InQueue.Add(node))
                    Queue.Enqueue(node);

                // use breadth-first search
                var q = new Queue<AbstractProjectBrowserTreeNode>();
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
                            if (InQueue.Add(node))
                                Queue.Enqueue(node);
                        }
                    }
                }

                if (!_threadRunning)
                {
                    _threadRunning = true;
                    ThreadPool.QueueUserWorkItem(Run);
                }
            }
        }

        public static void EnqueueParents(AbstractProjectBrowserTreeNode node)
        {
            lock (Queue)
            {
                while (node != null)
                {
                    if (InQueue.Add(node))
                        Queue.Enqueue(node);
                    node = node.Parent as AbstractProjectBrowserTreeNode;
                }

                if (!_threadRunning)
                {
                    _threadRunning = true;
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
                lock (Queue)
                {
                    if (Queue.Count == 0)
                    {
                        LoggingService.Debug("Git: OverlayIconManager Thread finished");
                        Debug.Assert(InQueue.Count == 0);
                        InQueue.Clear();
                        _threadRunning = false;
                        return;
                    }
                    node = Queue.Dequeue();
                    InQueue.Remove(node);
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
            var status = node?.GetNodeStatus();
            if (!status.HasValue)
            {
                return;
            }

            WorkbenchSingleton.SafeThreadAsyncCall(
                () =>
                {
                    var image = GetImage(status.Value);
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

        private static Image GetImage(GitStatus status)
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

        [SuppressMessage("ReSharper", "UnusedMember.Local",
            Justification = "Left as is from original SharpDevelop Git Add-in")]
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

        private static Image GetImage(StatusIcon status)
        {
            var index = (int)status;
            if (StatusIcons[index] == null)
            {
                var statusImages = StatusImages;
                var smallImage = new Bitmap(7, 10);
                using (var g = Graphics.FromImage(smallImage))
                {
                    //g.DrawImageUnscaled(statusImages, -index * 7, -3);
                    var srcRect = new Rectangle(index * 7, 3, 7, 10);
                    var destRect = new Rectangle(0, 0, 7, 10);
                    g.DrawImage(statusImages, destRect, srcRect, GraphicsUnit.Pixel);

                    //g.DrawLine(Pens.Black, 0, 0, 7, 10);
                }
                smallImage.Tag = typeof(OverlayIconManager);
                StatusIcons[index] = smallImage;
            }
            return StatusIcons[index];
        }
    }
}