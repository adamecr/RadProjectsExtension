using System.Collections.Generic;

namespace net.adamec.dev.vs.extension.radprojects.git
{
    /// <summary>
    /// Information retrieved from git status --porcelain=v2
    /// </summary>
    public class GitPorcelainInfo
    {
        /// <summary>
        /// Current branch
        /// </summary>
        public string Branch { get; set; }
        /// <summary>
        /// Current commit
        /// </summary>
        public string Commit { get; set; }
        /// <summary>
        /// Current commit (short)
        /// </summary>
        public string CommitShort
        {
            get
            {
                if (string.IsNullOrEmpty(Commit)) return null;
                return Commit == "(initial)" ? "(init)" : Commit.Substring(0, 7);
            }
        }
        /// <summary>
        /// Upstream branch
        /// </summary>
        public string Upstream { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Ahead/behind the upstream
        /// </summary>
        public string AB { get; set; }

        /// <summary>
        /// Information about individual files
        /// </summary>
        public List<GitPorcelainFileItemInfo> Files { get; } = new List<GitPorcelainFileItemInfo>();

        /// <summary>
        /// Count of modified files (based on <see cref="GitPorcelainFileItemInfo.StatusMaster"/>)
        /// </summary>
        public int ModifiedCnt => Files.FindAll(i => i.StatusMaster == GitChangeTypeEnum.Modified).Count;
        /// <summary>
        /// Count of added files (based on <see cref="GitPorcelainFileItemInfo.StatusMaster"/>)
        /// </summary>
        public int AddedCnt => Files.FindAll(i => i.StatusMaster == GitChangeTypeEnum.Added).Count;
        /// <summary>
        /// Count of deleted files (based on <see cref="GitPorcelainFileItemInfo.StatusMaster"/>)
        /// </summary>
        public int DeletedCnt => Files.FindAll(i => i.StatusMaster == GitChangeTypeEnum.Deleted).Count;
        /// <summary>
        /// Count of renamed files (based on <see cref="GitPorcelainFileItemInfo.StatusMaster"/>)
        /// </summary>
        public int RenamedCnt => Files.FindAll(i => i.StatusMaster == GitChangeTypeEnum.Renamed).Count;
        /// <summary>
        /// Count of copied files (based on <see cref="GitPorcelainFileItemInfo.StatusMaster"/>)
        /// </summary>
        public int CopiedCnt => Files.FindAll(i => i.StatusMaster == GitChangeTypeEnum.Copied).Count;

        /// <summary>
        /// Count of files with merge conflict (based on <see cref="GitPorcelainFileItemInfo.IsUnMerged"/>)
        /// </summary>
        public int UnMergedCnt => Files.FindAll(i => i.IsUnMerged).Count;
        /// <summary>
        /// Count of not tracked files (based on <see cref="GitPorcelainFileItemInfo.IsNotTracked"/>)
        /// </summary>
        public int UnTrackedCnt => Files.FindAll(i => i.IsNotTracked).Count;
        /// <summary>
        /// Count of ignored files (based on <see cref="GitPorcelainFileItemInfo.IsIgnored"/>)
        /// </summary>
        public int IgnoredCnt => Files.FindAll(i => i.IsIgnored).Count;

        /// <summary>
        /// Returns the short string information about number of changed  files
        /// Format: +Added/Not tracked ~Modified -Deleted !UnMerged
        /// </summary>
        public string CountsString => $"+{AddedCnt + UnTrackedCnt} ~{ModifiedCnt} -{DeletedCnt} !{UnMergedCnt}";

        /// <summary>
        /// Flag whether there is any pending (not committed) change
        /// </summary>
        public bool IsDirty => ModifiedCnt + AddedCnt + RenamedCnt + CopiedCnt + UnMergedCnt + UnTrackedCnt > 0;
    }
}
