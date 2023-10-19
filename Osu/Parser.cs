using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OsuBeatmapMixer.Osu.Utils;

namespace OsuBeatmapMixer.Osu {

	class Parser {

		enum ParseMode {
			General,
			Editor,
			Metadata,
			Difficulty,
			Events,
			TimingPoints,
			Colours,
			HitObjects,
			Unknown
		}

		internal static Beatmap ParseBeatmap(string Path, int startOffset, int endOffset) {
			Beatmap Res = new Beatmap();
            TimingPoint latest = null;
			TimingPoint current = null;

            using (StreamReader FileStream = new StreamReader(Path, Encoding.GetEncoding("UTF-8"))) {
				ParseMode parseMode = ParseMode.Unknown;

                string Text;
				while ((Text = FileStream.ReadLine()) != null) {
					if (string.IsNullOrWhiteSpace(Text)) continue;

					if (parseMode == ParseMode.Unknown)
						parseMode = GetParseMode(Text);
					else if (GetParseMode(Text) == ParseMode.Unknown) {
						switch (parseMode) {
							case ParseMode.General:
								GeneralParse(ref Res, Text);
								break;
							case ParseMode.Editor:
								break;
							case ParseMode.Metadata:
								MetadataParse(ref Res, Text);
								break;
							case ParseMode.Difficulty:
								DifficultyParse(ref Res, Text);
								break;
							case ParseMode.Events:
								break;
							case ParseMode.TimingPoints:
								current = TimingPointsParse(ref Res, Text, startOffset, endOffset, latest);
								if(current != null && current.Uninherited == 1)
								{
									latest = current;
                                }
								break;
							case ParseMode.Colours:
								break;
							case ParseMode.HitObjects:
								HitObjectsParse(ref Res, Text, startOffset, endOffset);
								break;
							default:
								throw new ParseException("Unknown Mode");
						}
					}
					else parseMode = GetParseMode(Text);
				}
			}

			if(latest != null)
            {
                latest.Offset = 1;
                Console.WriteLine("added : " + latest.Offset);
                Res.TimingPoints = Res.TimingPoints.Prepend(latest).ToList();
            }

			Res.TimingPoints.Sort((a, b) => a.Offset - b.Offset);
			Res.HitObjects.Sort((a, b) => a.StartOffset - b.StartOffset);

			return Res;
		}

		static ParseMode GetParseMode(string TextLine) {
			switch (TextLine) {
				case "[General]":
					return ParseMode.General;
				case "[Editor]":
					return ParseMode.Editor;
				case "[Metadata]":
					return ParseMode.Metadata;
				case "[Difficulty]":
					return ParseMode.Difficulty;
				case "[Events]":
					return ParseMode.Events;
				case "[TimingPoints]":
					return ParseMode.TimingPoints;
				case "[Colours]":
					return ParseMode.Colours;
				case "[HitObjects]":
					return ParseMode.HitObjects;
				default:
					return ParseMode.Unknown;
			}
		}

		static void GeneralParse(ref Beatmap beatmap, string Text) {
			if (Text.StartsWith("AudioFilename:")) {
				beatmap.AudioFilename = Text.Replace("AudioFilename:", "").Trim();
			}
			else if (Text.StartsWith("Mode:")) {
				beatmap.Mode = (GameMode) ParseToInt(Text.Replace("Mode:", "").Trim());
			}
		}

		static void MetadataParse(ref Beatmap beatmap, string Text) {
			if (Text.StartsWith("Title:")) {
				beatmap.Title = Text.Substring(6);
			}
			else if (Text.StartsWith("Artist:")) {
				beatmap.Artist = Text.Substring(7);
			}
			else if (Text.StartsWith("Creator:")) {
				beatmap.Creater = Text.Substring(8);
			}
			else if (Text.StartsWith("Version:")) {
				beatmap.Difficulty = Text.Substring(8);
			}
			else if (Text.StartsWith("Source:")) {
				beatmap.Source = Text.Substring(7);
			}
			else if (Text.StartsWith("Tags:")) {
				beatmap.Tags = Text.Substring(5);
			}
		}

		static void DifficultyParse(ref Beatmap beatmap, string Text) {
			if (Text.StartsWith("HPDrainRate:")) {
				beatmap.HPDrainRate = DoubleParse(Text.Replace("HPDrainRate:", "").Trim());
			}
			else if (Text.StartsWith("CircleSize:")) {
				beatmap.CircleSize = DoubleParse(Text.Replace("CircleSize:", "").Trim());
			}
			else if (Text.StartsWith("OverallDifficulty:")) {
				beatmap.OverallDifficulty = DoubleParse(Text.Replace("OverallDifficulty:", "").Trim());
			}
			else if (Text.StartsWith("ApproachRate:")) {
				beatmap.ApproachRate = DoubleParse(Text.Replace("ApproachRate:", "").Trim());
			}
			else if (Text.StartsWith("SliderMultiplier:")) {
				beatmap.SliderMultiplier = DoubleParse(Text.Replace("SliderMultiplier:", "").Trim());
			}
			else if (Text.StartsWith("SliderTickRate:")) {
				beatmap.SliderTickRate = DoubleParse(Text.Replace("SliderTickRate:", "").Trim());
			}
		}

		static TimingPoint TimingPointsParse(ref Beatmap beatmap, string Text, int startOffset, int endOffset, TimingPoint latest) {
			#if DEBUG
			Console.WriteLine(Text);
			#endif

			string[] TextSplit = Text.Split(',');

			if (TextSplit.Length != 8)
				throw new ParseException("TextSplit length is not 8");

			int UnInherited = ParseToInt(TextSplit[6]);
			double BeatLength = DoubleParse(TextSplit[1]);
			if (UnInherited == 0) BeatLength = Math.Abs(BeatLength);

			var newTimingPoint = new TimingPoint(
				ParseToInt(TextSplit[0]),
				BeatLength,
				ParseToInt(TextSplit[2]),
				ParseToInt(TextSplit[3]),
				ParseToInt(TextSplit[4]),
				ParseToInt(TextSplit[5]),
				UnInherited,
				ParseToInt(TextSplit[7])
			);

            if (newTimingPoint.Offset > endOffset) return null;
			if (newTimingPoint.Offset < startOffset) return newTimingPoint;

			if(latest != null)
            {
				latest.Offset = 0;
                Console.WriteLine("added : "+latest.Offset);
                beatmap.TimingPoints.Add(latest);
            }

			newTimingPoint.Offset -= startOffset;

            Console.WriteLine("added : " + newTimingPoint.Offset);
            beatmap.TimingPoints.Add(newTimingPoint);

			return null;
		}

		static void HitObjectsParse(ref Beatmap beatmap, string Text, int startOffset, int endOffset) {

			string[] TextSplit = Text.Split(',');

			if (TextSplit.Length < 5)
				throw new ParseException("TextSplit length is not 8");

			var offsetStart = ParseToInt(TextSplit[2]);

            if (offsetStart < startOffset) return;
            if (offsetStart > endOffset) return;

			offsetStart -= startOffset;

            if (TextSplit.Length == 5) { // ShortNormal
				beatmap.HitObjects.Add(new HitObject(
					ParseToInt(TextSplit[0]),
					ParseToInt(TextSplit[1]),
					offsetStart,
					ParseToInt(TextSplit[3]),
					ParseToInt(TextSplit[4]),
					null
				));
			}
			else if (TextSplit.Length == 6) { // Normal or Mania LN or ShortSpinner
				int Type = ParseToInt(TextSplit[3]);
				if ((Type & 0b1000_0000) == 128) {
					List<string> EndIndexSplit = new List<string>(TextSplit[5].Split(':'));
					int EndOffset = ParseToInt(EndIndexSplit[0]);

					if (EndOffset > endOffset) return;

					EndOffset -= startOffset;

                    EndIndexSplit.RemoveAt(0);

					beatmap.HitObjects.Add(new HitObject(
						HitObject.ObjectType.ManiaLN,
						ParseToInt(TextSplit[0]),
						ParseToInt(TextSplit[1]),
						offsetStart,
						Type,
						ParseToInt(TextSplit[4]),
						EndOffset,
						null,
						string.Join(":", EndIndexSplit)
					));
				}
				else if ((Type & 0b0000_1000) == 8)
                {
                    int EndOffset = ParseToInt(TextSplit[5]);

                    if (EndOffset > endOffset) return;
                    EndOffset -= startOffset;

                    beatmap.HitObjects.Add(new HitObject(
						HitObject.ObjectType.ShortSpinner,
						ParseToInt(TextSplit[0]),
						ParseToInt(TextSplit[1]),
						offsetStart,
						ParseToInt(TextSplit[3]),
						ParseToInt(TextSplit[4]),
                        EndOffset,
						null,
						null
				));
				}
				else {
					beatmap.HitObjects.Add(new HitObject(
						ParseToInt(TextSplit[0]),
						ParseToInt(TextSplit[1]),
						offsetStart,
						Type,
						ParseToInt(TextSplit[4]),
						TextSplit[5]
					));
				}
			}
			else if (TextSplit.Length == 7)
            { // Spinner
                int EndOffset = ParseToInt(TextSplit[5]);

                if (EndOffset > endOffset) return;
                EndOffset -= startOffset;

                beatmap.HitObjects.Add(new HitObject(
						HitObject.ObjectType.Spinner,
						ParseToInt(TextSplit[0]),
						ParseToInt(TextSplit[1]),
						offsetStart,
						ParseToInt(TextSplit[3]),
						ParseToInt(TextSplit[4]),
                        EndOffset,
						null,
						TextSplit[6]
				));
			}
			else if (TextSplit.Length == 8) { // ShortSlider
				int StartOffset = offsetStart;

				TimingPoint AppliedTimingPoint = GetAppliedTimingPoint(beatmap, StartOffset, true);

				TimingPoint AppliedScale = GetAppliedTimingPoint(beatmap, StartOffset, false);

				double SliderLength = DoubleParse(TextSplit[7]);

				int EndOffset = GetSliderEndOffset(StartOffset, SliderLength, beatmap.SliderMultiplier, AppliedTimingPoint.BeatLength, AppliedScale is null ? default : AppliedScale.BeatLength);

                if (EndOffset > endOffset) return;
                EndOffset -= startOffset;

                beatmap.HitObjects.Add(new HitObject(
					HitObject.ObjectType.ShortSlider,
					ParseToInt(TextSplit[0]),
					ParseToInt(TextSplit[1]),
					StartOffset,
					ParseToInt(TextSplit[3]),
					ParseToInt(TextSplit[4]),
					EndOffset,
					$"{TextSplit[5]},{TextSplit[6]},{TextSplit[7]}",
					null
				));
			}
			else if (TextSplit.Length == 11) { // Slider
				int StartOffset = offsetStart;

				TimingPoint AppliedTimingPoint = GetAppliedTimingPoint(beatmap, StartOffset, true);

				TimingPoint AppliedScale = GetAppliedTimingPoint(beatmap, StartOffset, false);

				double SliderLength = DoubleParse(TextSplit[7]);

				int EndOffset = GetSliderEndOffset(StartOffset, SliderLength, beatmap.SliderMultiplier, AppliedTimingPoint.BeatLength, AppliedScale is null ? default : AppliedScale.BeatLength);

                if (EndOffset > endOffset) return;
                EndOffset -= startOffset;

                beatmap.HitObjects.Add(new HitObject(
					HitObject.ObjectType.Slider,
					ParseToInt(TextSplit[0]),
					ParseToInt(TextSplit[1]),
					StartOffset,
					ParseToInt(TextSplit[3]),
					ParseToInt(TextSplit[4]),
					EndOffset,
					$"{TextSplit[5]},{TextSplit[6]},{TextSplit[7]},{TextSplit[8]},{TextSplit[9]}",
					TextSplit[10]
				));
			}
		}

		static int GetSliderEndOffset(int StartOffset, double SliderLength, double SliderMultiplier, double BeatLength, double ScaleBeatLength = 100)
			=> (int) Math.Round((SliderLength / (SliderMultiplier * 100) * (BeatLength / (100 / ScaleBeatLength))) + StartOffset);

		static TimingPoint GetAppliedTimingPoint(Beatmap beatmap, int StartOffset, bool Uninherited) {
			List<TimingPoint> TimingPoints = (
					from t in beatmap.TimingPoints
					where t.Uninherited == (Uninherited ? 1 : 0)
					where t.Offset <= StartOffset
					select t
				).ToList();
			if (TimingPoints.Count == 0) return null;

			TimingPoints.Sort((a, b) => b.Offset - a.Offset);

			return TimingPoints[0];
		}
	}
}
