namespace net.adamec.dev.vs.extension.radprojects.git
{
    /// <summary>
    /// Information about single (modified) file retrieved from git status --porcelain=v2
    /// </summary>
    public class GitPorcelainFileItemInfo
    {
        /// <summary>
        /// Name of the file relative to the git root
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Modification status of file at git index (staged)
        /// For paths with merge conflicts, the status of "local" side of merge
        /// </summary>
        public GitChangeTypeEnum StatusIndex { get; set; } = GitChangeTypeEnum.Unmodified;
        /// <summary>
        /// Modification status of file at git work tree (not staged)
        /// For paths with merge conflicts, the status of "other" side of merge
        /// </summary>
        public GitChangeTypeEnum StatusWorkTree { get; set; } = GitChangeTypeEnum.Unmodified;
        /// <summary>
        /// General modification status of file
        /// </summary>
        public GitChangeTypeEnum StatusMaster => GetGitMasterChangeType();
        /// <summary>
        /// Flag whether there is a merge conflict at the file
        /// </summary>
        public bool IsUnMerged { get; set; }
        /// <summary>
        /// Flag whether the file is not tracked by git (new file)
        /// </summary>
        public bool IsNotTracked { get; set; }
        /// <summary>
        /// Flag whether the file is ignored by git
        /// </summary>
        public bool IsIgnored { get; set; }
        /// <summary>
        /// XY git modification status code
        /// </summary>
        public string ChangeCode { get; set; }

        /// <summary>
        /// Provides the "master" change type based on both index (staged) and work tree (unstaged) change types
        /// </summary>
        /// <returns>"Master" change type based on both index and work tree change types</returns>
        private GitChangeTypeEnum GetGitMasterChangeType()
        {
            var staged = StatusIndex;
            var unStaged = StatusWorkTree;

            if (staged == GitChangeTypeEnum.Unmodified && unStaged == GitChangeTypeEnum.Unmodified)
                return GitChangeTypeEnum.Unmodified;

            if (staged == GitChangeTypeEnum.NotTracked || unStaged == GitChangeTypeEnum.NotTracked)
                return GitChangeTypeEnum.NotTracked;

            if (staged == GitChangeTypeEnum.Ignored || unStaged == GitChangeTypeEnum.Ignored)
                return GitChangeTypeEnum.Ignored;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (staged)
            {
                case GitChangeTypeEnum.Modified:
                case GitChangeTypeEnum.Added:
                case GitChangeTypeEnum.Deleted:
                case GitChangeTypeEnum.Renamed:
                case GitChangeTypeEnum.Copied:
                    return staged;
                case GitChangeTypeEnum.UnMerged
                    when unStaged == GitChangeTypeEnum.UnMerged:
                    return GitChangeTypeEnum.Modified;
            }

            return unStaged;
        }

        /// <summary>
        /// Returns short string representation of file item (XY FileName)
        /// </summary>
        /// <returns>Short string representation of file item (XY FileName)</returns>
        public override string ToString()
        {
            return $"{ChangeCode} {FileName}";
        }
    }
}
