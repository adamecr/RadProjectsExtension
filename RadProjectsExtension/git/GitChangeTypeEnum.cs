namespace net.adamec.dev.vs.extension.radprojects.git
{
    /// <summary>
    /// Type of git tracked file change
    /// </summary>
    public enum GitChangeTypeEnum
    {
        /// <summary>
        /// File is not modified (no change)
        /// </summary>
        Unmodified,
        /// <summary>
        /// File is modified
        /// </summary>
        Modified,
        /// <summary>
        /// File has been added
        /// </summary>
        Added,
        /// <summary>
        /// File has been deleted
        /// </summary>
        Deleted,
        /// <summary>
        /// File has been renamed
        /// </summary>
        Renamed,
        /// <summary>
        /// File has been copied
        /// </summary>
        Copied,
        /// <summary>
        /// File is unmerged
        /// </summary>
        UnMerged,
        /// <summary>
        /// File is not tracked
        /// </summary>
        NotTracked,
        /// <summary>
        /// File is ignored
        /// </summary>
        Ignored
    }
}
