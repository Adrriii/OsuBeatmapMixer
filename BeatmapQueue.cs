using OsuBeatmapMixer.Osu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapMixer {

	public class BeatmapQueue {

		public int Order { get; set; }

        public int Offset { get; set; }

        public int Start { get; set; }

        public int End { get; set; }

        public string Artist => Beatmap.Artist;

		public string Title => Beatmap.Title;

		public string Creator => Beatmap.Creater;

		public string DiffName => Beatmap.Difficulty;

		public string Path {  get; set; }

		private string DirPath { get; }

		public Beatmap Beatmap { get; set; }

		public BeatmapQueue(string Path) {
			this.Path = Path;
			Beatmap = Parser.ParseBeatmap(Path, Start, End);
            Beatmap.Offset = Offset;
            DirPath = System.IO.Path.GetDirectoryName(Path);
		}

		internal string GetAudioPath() =>
			$@"{DirPath}\{Beatmap.AudioFilename}";
	}
}
