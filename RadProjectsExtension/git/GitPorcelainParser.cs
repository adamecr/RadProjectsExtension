using System;
using System.Collections.Generic;
using net.adamec.dev.vs.extension.radprojects.utils;

namespace net.adamec.dev.vs.extension.radprojects.git
{
    /// <summary>
    /// Parser of information retrieved from git status --porcelain=v2
    /// Supported/recommended git status parameters
    /// --branch - provides the information about current branch
    /// --untracked=all - provides also the information about untracked files in untracked directories
    /// --ignored - provides the information about ignored files as well
    /// Reference: https://git-scm.com/docs/git-status
    /// </summary>
    public class GitPorcelainParser
    {
        /// <summary>
        /// Signature of parser for porcelain line
        /// </summary>
        /// <param name="lineWithoutType">Single line from git status --porcelain=v2 without the leading char (line/parser type)</param>
        /// <param name="gitInfo">Reference to the root git info object in case the parser needs to manipulate root data</param>
        /// <returns>New instance of file item info if created by line parser otherwise null</returns>
        protected delegate GitPorcelainFileItemInfo GitPorcelainLineParser(string lineWithoutType, GitPorcelainInfo gitInfo);

        /// <summary>
        /// Parses the output of git status --porcelain=v2
        /// </summary>
        /// <param name="lines">Output lines</param>
        /// <returns>Parsed <see cref="GitPorcelainInfo"/> object</returns>
        public static GitPorcelainInfo ParseGitPorcelain(string lines)
        {
            if (string.IsNullOrEmpty(lines)) return null;

            //Definition of parsers per line type
            var parsers = new Dictionary<char, GitPorcelainLineParser>()
            {
                {'1', GitPorcelainItemType1Parser},
                {'#', GitPorcelainItemHeaderParser},
                {'?', GitPorcelainItemUnTrackedParser},
                {'!', GitPorcelainItemIgnoredParser}
            };

            var gitPorcelainInfo = new GitPorcelainInfo();
            foreach (var lineRaw in lines.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                var line = lineRaw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                //Get line type
                var lineType = line[0];
                line = line.Substring(1).Trim();
                //Try to get the parser based on the line type
                if (!parsers.TryGetValue(lineType, out var parser)) continue;

                //Call parser and add the new file item if available. Parser can also modify the root gitPorcelainInfo object if needed
                var newItem = parser(line, gitPorcelainInfo);
                if (newItem != null) { gitPorcelainInfo.Files.Add(newItem); }
            }
            return gitPorcelainInfo;
        }

        /// <summary>
        /// Parser of header porcelain line(line type "#")
        /// Currently supports #branch.xxxxx headers
        /// </summary>
        /// <param name="lineWithoutType">Single line from git status --porcelain=v2 without the leading char (line/parser type)</param>
        /// <param name="gitInfo">Reference to the root git info object as the parser manipulates root data</param>
        /// <returns>Always null</returns>
        private static GitPorcelainFileItemInfo GitPorcelainItemHeaderParser(string lineWithoutType, GitPorcelainInfo gitInfo)
        {
            if (string.IsNullOrEmpty(lineWithoutType)) return null;
            var key = lineWithoutType.SplitByFirstSpace(out var value);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (key)
            {
                case "branch.oid":
                    gitInfo.Commit = value;
                    break;
                case "branch.head":
                    gitInfo.Branch = value;
                    break;
                case "branch.upstream":
                    gitInfo.Upstream = value;
                    break;
                case "branch.ab":
                    gitInfo.AB = value;
                    break;
            }
            return null;
        }

        /// <summary>
        /// Parser of porcelain line for modified files (line type "1")
        /// </summary>
        /// <param name="lineWithoutType">Single line from git status --porcelain=v2 without the leading char (line/parser type)</param>
        /// <param name="gitInfo">(Not used) Reference to the root git info object in case the parser needs to manipulate root data</param>
        /// <returns>New instance of file item info if created by line parser otherwise null (when the <paramref name="lineWithoutType"/> is empty)</returns>
        private static GitPorcelainFileItemInfo GitPorcelainItemType1Parser(string lineWithoutType, GitPorcelainInfo gitInfo)
        {
            if (string.IsNullOrEmpty(lineWithoutType)) return null;
            var item = new GitPorcelainFileItemInfo();

            var line = lineWithoutType;
            var xy = line.SplitByFirstSpace(out line) + "  "; //ensure at least two chars
            var fileName = line.LastPart(" ").Trim();
            if (string.IsNullOrEmpty(fileName)) return null;

            item.ChangeCode = xy.Trim();
            item.StatusIndex = GetGitChangeType(xy[0]);
            item.StatusWorkTree = GetGitChangeType(xy[1]);

            item.IsUnMerged = xy.Contains("U") || xy == "AA" || xy == "DD";
            item.IsNotTracked = xy.Contains("?");
            item.IsIgnored = xy.Contains("!");

            item.FileName = fileName;

            return item;
        }

        /// <summary>
        /// Parser of porcelain line for not tracked files (line type "?")
        /// </summary>
        /// <param name="lineWithoutType">Single line from git status --porcelain=v2 without the leading char (line/parser type)</param>
        /// <param name="gitInfo">(Not used) Reference to the root git info object in case the parser needs to manipulate root data</param>
        /// <returns>New instance of file item info if created by line parser otherwise null (when the <paramref name="lineWithoutType"/> is empty)</returns>
        private static GitPorcelainFileItemInfo GitPorcelainItemUnTrackedParser(string lineWithoutType, GitPorcelainInfo gitInfo)
        {
            if (string.IsNullOrEmpty(lineWithoutType)) return null;
            var item = new GitPorcelainFileItemInfo
            {
                FileName = lineWithoutType,
                ChangeCode = "?",
                StatusIndex = GitChangeTypeEnum.NotTracked,
                StatusWorkTree = GitChangeTypeEnum.NotTracked,
                IsNotTracked = true
            };
            return item;
        }

        /// <summary>
        /// Parser of porcelain line for ignored files (line type "!")
        /// </summary>
        /// <param name="lineWithoutType">Single line from git status --porcelain=v2 without the leading char (line/parser type)</param>
        /// <param name="gitInfo">(Not used) Reference to the root git info object in case the parser needs to manipulate root data</param>
        /// <returns>New instance of file item info if created by line parser otherwise null (when the <paramref name="lineWithoutType"/> is empty)</returns>
        private static GitPorcelainFileItemInfo GitPorcelainItemIgnoredParser(string lineWithoutType, GitPorcelainInfo gitInfo)
        {
            if (string.IsNullOrEmpty(lineWithoutType)) return null;
            var item = new GitPorcelainFileItemInfo
            {
                FileName = lineWithoutType,
                ChangeCode = "!",
                StatusIndex = GitChangeTypeEnum.Ignored,
                StatusWorkTree = GitChangeTypeEnum.Ignored,
                IsIgnored = true
            };
            return item;
        }

        /// <summary>
        /// Gets the <see cref="GetGitChangeType"/> from given change code character
        /// </summary>
        /// <param name="code">Change code character</param>
        /// <returns>Parsed <see cref="GetGitChangeType"/></returns>
        private static GitChangeTypeEnum GetGitChangeType(char code)
        {
            switch (code)
            {
                case 'M': return GitChangeTypeEnum.Modified;
                case 'A': return GitChangeTypeEnum.Added;
                case 'D': return GitChangeTypeEnum.Deleted;
                case 'R': return GitChangeTypeEnum.Renamed;
                case 'C': return GitChangeTypeEnum.Copied;
                case 'U': return GitChangeTypeEnum.UnMerged;
                case '?': return GitChangeTypeEnum.NotTracked;
                case '!': return GitChangeTypeEnum.Ignored;
                default: return GitChangeTypeEnum.Unmodified;
            }
        }
    }
}
