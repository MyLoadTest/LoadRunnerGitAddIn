// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.SharpDevelop.Gui;

namespace MyLoadTest.LoadRunnerGitAddIn
{
	/// <summary>
	/// Output pad category for git.
	/// </summary>
	public static class GitMessageView
	{
		static MessageViewCategory category;
		
		/// <summary>
		/// Gets the git message view category.
		/// </summary>
		public static MessageViewCategory Category {
			get {
				if (category == null) {
					MessageViewCategory.Create(ref category, "Git");
				}
				return category;
			}
		}
		
		/// <summary>
		/// Appends a line to the git message view.
		/// </summary>
		public static void AppendLine(string text)
		{
			Category.AppendLine(text);
		}
	}
}
