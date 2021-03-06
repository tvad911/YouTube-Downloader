﻿using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace YouTube_Downloader_DLL.Classes
{
    /// <summary>
    /// Used to quickly list all videos in a playlist.
    /// </summary>
    public class QuickPlaylist
    {
        public bool IgnoreDuplicates { get; set; } = true;
        public string Title { get; private set; }
        public string Url { get; private set; }
        public List<QuickVideoInfo> Videos { get; private set; }

        public QuickPlaylist(string playlistUrl)
        {
            this.Url = playlistUrl;
            this.Videos = new List<QuickVideoInfo>();
        }

        public QuickPlaylist Load()
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            int videoIndex = 0;
            string source = wc.DownloadString(this.Url);
            Match m = null;

            // Find playlist title
            var playlistname = new Regex(
                @"pl-header-title[\s|\""].*?>(.*?)(?=<)",
                RegexOptions.Singleline);

            this.Title = playlistname.Match(source).Groups[1].Value.Trim();

            // Find the load more button
            var loadmore = new Regex(
                @"data-uix-load-more-href=""([^ ""]*)",
                RegexOptions.Compiled);

            // Find video id and title in any order
            var titleId = new Regex(
                @"<tr(?=.*?data-video-id=""(.*?)"")(?=.*?data-title=""(.*?)"").*?pl-video-edit-options",
                RegexOptions.Compiled | RegexOptions.Singleline);

            // Find duration. Private/deleted e.g. videos does not have duration
            var duration = new Regex(
                @"timestamp"">.*?>(.*?)<",
                RegexOptions.Compiled | RegexOptions.Singleline);

            // Leaving this as null allow duplicates
            List<string> ids = this.IgnoreDuplicates ? new List<string>() : null;

            do
            {
                if (m != null)
                {
                    source = wc.DownloadString(@"https://www.youtube.com" + m.Groups[1].Value);

                    source = Regex.Unescape(source);
                    source = HttpUtility.HtmlDecode(source);
                }

                foreach (Match match in titleId.Matches(source))
                {
                    string fullMatch = match.Groups[0].Value;
                    string resultId = match.Groups[1].Value;
                    string resultTitle = match.Groups[2].Value;
                    string resultDuration = string.Empty;

                    if (ids?.Contains(resultId) == true)
                        continue;

                    Match mDuration;
                    if ((mDuration = duration.Match(fullMatch)).Success)
                        resultDuration = mDuration.Groups[1].Value;

                    ids?.Add(resultId);
                    this.Videos.Add(new QuickVideoInfo(videoIndex + 1, // Not zero-based
                                                       resultId,
                                                       resultTitle,
                                                       resultDuration));

                    videoIndex++;
                }
            }
            while ((m = loadmore.Match(source)).Success);

            return this;
        }

        public static QuickVideoInfo[] GetAll(string playlistUrl)
        {
            return new QuickPlaylist(playlistUrl).Load().Videos.ToArray();
        }
    }
}
