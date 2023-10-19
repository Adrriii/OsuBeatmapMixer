using OsuBeatmapMixer.Osu;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapMixer {

	class MixData {

		internal Beatmap MixedBeatmap => mixedBeatmap;

		Beatmap mixedBeatmap;

		readonly Mixer beatmapMixer;

		readonly Audio.Mixer audioMixer;

		readonly string audioPath;

		readonly string osuPath;

		internal MixData(IEnumerable<BeatmapQueue> BeatmapQueues, string AudioPath, string OsuPath) {
			beatmapMixer = new Mixer(BeatmapQueues);
			audioMixer = new Audio.Mixer(BeatmapQueues);

			audioPath = AudioPath;
			osuPath = OsuPath;

			mixedBeatmap = new Beatmap();
		}

		internal void Mix(Action<int> ReportProgress) {

			beatmapMixer.MixBeatmap(ref mixedBeatmap, ReportProgress);

			audioMixer.Mix(audioPath, ReportProgress);

			mixedBeatmap.SaveFile(osuPath);
		}
	}
}
