using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLoadTest.LoadRunnerGitAddIn
{
    public sealed class GitStatusSet
    {
        private Dictionary<string, GitStatusSet> _entries;
        private GitStatus _status = GitStatus.OK;

        public GitStatus AddEntry(string path, GitStatus status)
        {
            if (string.IsNullOrEmpty(path) || path == ".")
            {
                this._status = status;
                return status;
            }
            if (_entries == null)
                _entries = new Dictionary<string, GitStatusSet>();
            string entry;
            string subpath;
            var pos = path.IndexOf('/');
            if (pos < 0)
            {
                entry = path;
                subpath = null;
            }
            else
            {
                entry = path.Substring(0, pos);
                subpath = path.Substring(pos + 1);
            }
            GitStatusSet subset;
            if (!_entries.TryGetValue(entry, out subset))
                _entries[entry] = subset = new GitStatusSet();
            status = subset.AddEntry(subpath, status);
            if (status == GitStatus.Added || status == GitStatus.Deleted || status == GitStatus.Modified)
            {
                this._status = GitStatus.Modified;
            }
            return this._status;
        }

        public GitStatus GetStatus(string path)
        {
            if (string.IsNullOrEmpty(path) || path == ".")
                return _status;
            if (_entries == null)
                return GitStatus.None;
            string entry;
            string subpath;
            var pos = path.IndexOf('/');
            if (pos < 0)
            {
                entry = path;
                subpath = null;
            }
            else
            {
                entry = path.Substring(0, pos);
                subpath = path.Substring(pos + 1);
            }
            GitStatusSet subset;
            if (!_entries.TryGetValue(entry, out subset))
                return GitStatus.None;
            else
                return subset.GetStatus(subpath);
        }
    }
}