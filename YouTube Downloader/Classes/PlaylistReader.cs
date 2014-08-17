﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace YouTube_Downloader.Classes
{
    public class PlaylistReader
    {
        private StreamWriter log;
        private StreamReader reader;
        private Process youtubeDl;

        public Playlist Playlist { get; set; }

        public PlaylistReader(string url)
        {
            /* ToDo:
             * 
             * Start process
             * Start log writer
             * Store playlist info (name, id, count)
             */

            /* Playlist properties. */
            string name = string.Empty;
            string playlist_id = Helper.GetPlaylistId(url);
            int onlineCount = 0;

            string arguments = string.Format(YouTubeDLHelper.Cmd_JSON_Info_Playlist, playlist_id, url);

            youtubeDl = YouTubeDLHelper.StartProcess(arguments);

            /* Write output to log. */
            log = YouTubeDLHelper.CreateLogWriter();

            YouTubeDLHelper.WriteLogHeader(log, arguments, url);

            reader = youtubeDl.StandardOutput;

            while ((line = reader.ReadLine()) != null)
            {
                Match m;

                /* Get the playlist count. */
                if ((m = Regex.Match(line, @"^\[youtube:playlist\] playlist (.*):.*downloading\s+(\d+)\s+.*$")).Success)
                {
                    name = m.Groups[1].Value;
                    onlineCount = int.Parse(m.Groups[2].Value);
                    break;
                }
            }

            this.Playlist = new Playlist(playlist_id, name, onlineCount, new List<VideoInfo>());
        }

        private string line = string.Empty;

        public VideoInfo Next()
        {
            string json_path = string.Empty;

            while ((line = reader.ReadLine()) != null)
            {
                log.WriteLine(line);

                Match m;

                /* New json found, break & create a VideoInfo instance. */
                if ((m = Regex.Match(line, @"^\[info\].*JSON.*:\s*(.*)$")).Success)
                {
                    string file = m.Groups[1].Value;

                    json_path = Path.Combine(Application.StartupPath, file);
                    /* Read another line to release json file from youtube-dl (hopefully...). */
                    break;
                }
            }

            /* If it's the end of the stream finish up the process. */
            if (line == null)
            {
                /* End of stream. */
                YouTubeDLHelper.WriteLogEnd(log);

                youtubeDl.WaitForExit();

                if (!youtubeDl.HasExited)
                    youtubeDl.Kill();

                return null;
            }

            VideoInfo video = VideoInfo.DeserializeJson(json_path);

            this.Playlist.Videos.Add(video);

            return video;
        }
    }
}