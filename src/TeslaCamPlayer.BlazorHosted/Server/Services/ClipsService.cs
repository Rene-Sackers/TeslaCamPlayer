using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;
using TeslaCamPlayer.BlazorHosted.Server.Services.Interfaces;
using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Server.Services;

public partial class ClipsService : IClipsService
{
	private const string NoThumbnailImageUrl = "/img/no-thumbnail.png";
	
	private static readonly string CacheFilePath = Path.Combine(AppContext.BaseDirectory, "clips.json");
	private static readonly Regex FileNameRegex = FileNameRegexGenerated();
	private static Clip[] _cache;
	
	private readonly ISettingsProvider _settingsProvider;
	private readonly IFfProbeService _ffProbeService;

	public ClipsService(ISettingsProvider settingsProvider, IFfProbeService ffProbeService)
	{
		_settingsProvider = settingsProvider;
		_ffProbeService = ffProbeService;
	}

	private async Task<Clip[]> GetCachedAsync()
		=> File.Exists(CacheFilePath)
			? JsonConvert.DeserializeObject<Clip[]>(await File.ReadAllTextAsync(CacheFilePath))
			: null;

	public async Task<Clip[]> GetClipsAsync(bool refreshCache = false)
	{
		if (!refreshCache && (_cache ??= await GetCachedAsync()) != null)
			return _cache;

		var videoFiles = (await Task.WhenAll(Directory
			.GetFiles(_settingsProvider.Settings.ClipsRootPath, "*.mp4", SearchOption.AllDirectories)
			.AsParallel()
			.Select(path => new { Path = path, RegexMatch = FileNameRegex.Match(path) })
			.Where(f => f.RegexMatch.Success)
			.ToList()
			.Select(async f => await TryParseVideoFileAsync(f.Path, f.RegexMatch))))
			.AsParallel()
			.Where(vfi => vfi != null)
			.ToList();

		var recentClips = GetRecentClips(videoFiles
			.Where(vfi => vfi.ClipType == ClipType.Recent).ToList());
		
		var clips = videoFiles
			.Select(vfi => vfi.EventFolderName)
			.Distinct()
			.AsParallel()
			.Where(e => !string.IsNullOrWhiteSpace(e))
			.Select(e => ParseClip(e, videoFiles))
			.Concat(recentClips.AsParallel())
			.OrderByDescending(c => c.StartDate)
			.ToArray();

		_cache = clips;
		await File.WriteAllTextAsync(CacheFilePath, JsonConvert.SerializeObject(clips));
		return _cache;
	}

	private static IEnumerable<Clip> GetRecentClips(List<VideoFile> recentVideoFiles)
	{
		recentVideoFiles = recentVideoFiles.OrderByDescending(f => f.StartDate).ToList();

		var currentClipSegments = new List<ClipVideoSegment>();
		for (var i = 0; i < recentVideoFiles.Count;)
		{
			var currentVideoFile = recentVideoFiles[i];
			var segmentVideos = recentVideoFiles.Where(f => f.StartDate == currentVideoFile.StartDate).ToList();
			var segment = new ClipVideoSegment
			{
				StartDate = currentVideoFile.StartDate,
				EndDate = currentVideoFile.StartDate.Add(currentVideoFile.Duration),
				CameraFront = segmentVideos.FirstOrDefault(v => v.Camera == Cameras.Front),
				CameraLeftRepeater = segmentVideos.FirstOrDefault(v => v.Camera == Cameras.LeftRepeater),
				CameraRightRepeater = segmentVideos.FirstOrDefault(v => v.Camera == Cameras.RightRepeater),
				CameraBack = segmentVideos.FirstOrDefault(v => v.Camera == Cameras.Back)
			};
			
			currentClipSegments.Add(segment);

			// Set i to the video after the last video in this clip segment, ie: the first video of the next segment.
			i = i + segmentVideos.Count + 1;
			
			// No more recent video files
			if (i >= recentVideoFiles.Count)
			{
				yield return new Clip(ClipType.Recent, currentClipSegments.ToArray())
				{
					ThumbnailUrl = NoThumbnailImageUrl
				};
				currentClipSegments.Clear();
				yield break;
			}

			const int segmentVideoGapToleranceInSeconds = 5;
			var nextSegmentFirstVideo = recentVideoFiles[i];
			// Next video is within X seconds of last video of current segment, continue building clip segments
			if (nextSegmentFirstVideo.StartDate <= segment.EndDate.AddSeconds(segmentVideoGapToleranceInSeconds))
				continue;
			
			// Next video is more than X seconds, assume it's a new recent video clip
			yield return new Clip(ClipType.Recent, currentClipSegments.ToArray())
			{
				ThumbnailUrl = NoThumbnailImageUrl
			};
			currentClipSegments.Clear();
		}
	}

	private async Task<VideoFile> TryParseVideoFileAsync(string path, Match regexMatch)
	{
		try
		{
			return await ParseVideoFileAsync(path, regexMatch);
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to parse info for video file from path: {Path}", path);
			return null;
		}
	}

	private async Task<VideoFile> ParseVideoFileAsync(string path, Match regexMatch)
	{
		var clipType = regexMatch.Groups["type"].Value switch
		{
			"RecentClips" => ClipType.Recent,
			"SavedClips" => ClipType.Saved,
			"SentryClips" => ClipType.Sentry,
			_ => ClipType.Unknown
		};

		var camera = regexMatch.Groups["camera"].Value switch
		{
			"back" => Cameras.Back,
			"front" => Cameras.Front,
			"left_repeater" => Cameras.LeftRepeater,
			"right_repeater" => Cameras.RightRepeater,
			_ => Cameras.Unknown
		};

		var date = new DateTime(
			int.Parse(regexMatch.Groups["vyear"].Value),
			int.Parse(regexMatch.Groups["vmonth"].Value),
			int.Parse(regexMatch.Groups["vday"].Value),
			int.Parse(regexMatch.Groups["vhour"].Value),
			int.Parse(regexMatch.Groups["vminute"].Value),
			int.Parse(regexMatch.Groups["vsecond"].Value));

		var duration = await _ffProbeService.GetVideoFileDurationAsync(path);
		if (!duration.HasValue)
		{
			Log.Error("Failed to get duration for video file {Path}", path);
			return null;
		}

		var eventFolderName = clipType != ClipType.Recent
			? regexMatch.Groups["event"].Value
			: null;
		
		return new VideoFile
		{
			FilePath = path,
			Url = $"/Api/Video/{Uri.EscapeDataString(path)}",
			EventFolderName = eventFolderName,
			ClipType = clipType,
			StartDate = date,
			Camera = camera,
			Duration = duration.Value
		};
	}

	private static Clip ParseClip(string eventFolderName, IEnumerable<VideoFile> videoFiles)
	{
		var eventVideoFiles = videoFiles
			.AsParallel()
			.Where(v => v.EventFolderName == eventFolderName)
			.ToList();
		
		var segments = eventVideoFiles
			.GroupBy(v => v.StartDate)
			.AsParallel()
			.Select(g => new ClipVideoSegment
			{
				StartDate = g.Key,
				EndDate = g.Key.Add(g.First().Duration),
				CameraFront = g.FirstOrDefault(v => v.Camera == Cameras.Front),
				CameraLeftRepeater = g.FirstOrDefault(v => v.Camera == Cameras.LeftRepeater),
				CameraRightRepeater = g.FirstOrDefault(v => v.Camera == Cameras.RightRepeater),
				CameraBack = g.FirstOrDefault(v => v.Camera == Cameras.Back)
			})
			.ToArray();

		var eventFolderPath = Path.GetDirectoryName(eventVideoFiles.First().FilePath)!;
		var expectedEventJsonPath = Path.Combine(eventFolderPath, "event.json");
		var eventInfo = TryReadEvent(expectedEventJsonPath);

		var expectedEventThumbnailPath = Path.Combine(eventFolderPath, "thumb.png");
		var thumbnailUrl = File.Exists(expectedEventThumbnailPath)
			? $"/Api/Thumbnail/{Uri.EscapeDataString(expectedEventThumbnailPath)}"
			: NoThumbnailImageUrl;

		return new Clip(eventVideoFiles.First().ClipType, segments)
		{
			Event = eventInfo,
			ThumbnailUrl = thumbnailUrl
		};
	}

	private static Event TryReadEvent(string path)
	{
		try
		{
			if (!File.Exists(path))
				return null;

			var json = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<Event>(json);
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to read {EventJsonPath}", path);
			return null;
		}
	}

	/*
	 * \SavedClips\2023-06-16_17-18-06\2023-06-16_17-12-49-front.mp4"
	 * type = SavedClips
	 * event = 2023-06-16_17-18-06
	 * year = 2023
	 * month = 06
	 * day = 17
	 * hour = 18
	 * minute = 06
	 * vyear = 2023
	 * vmonth = 06
	 * vhour = 17
	 * vminute = 12
	 * vsecond = 49
	 * camera = front
	 */
	[GeneratedRegex(@"(?:[\\/]|^)(?<type>(?:Recent|Saved|Sentry)Clips)(?:[\\/](?<event>(?<year>20\d{2})\-(?<month>[0-1][0-9])\-(?<day>[0-3][0-9])_(?<hour>[0-2][0-9])\-(?<minute>[0-5][0-9])\-(?<second>[0-5][0-9])))?[\\/](?<vyear>20\d{2})\-(?<vmonth>[0-1][0-9])\-(?<vday>[0-3][0-9])_(?<vhour>[0-2][0-9])\-(?<vminute>[0-5][0-9])\-(?<vsecond>[0-5][0-9])\-(?<camera>back|front|left_repeater|right_repeater)\.mp4")]
	private static partial Regex FileNameRegexGenerated();
}