using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Client.Components;

public partial class ClipViewer : ComponentBase
{
	private static readonly TimeSpan TimelineScrubTimeout = TimeSpan.FromSeconds(2);
	
	[Inject]
	public IJSRuntime JsRuntime { get; set; }
	
	[Parameter]
	public EventCallback PreviousButtonClicked { get; set; }
	
	[Parameter]
	public EventCallback NextButtonClicked { get; set; }

	private double TimelineValue
	{
		get => _timelineValue;
		set
		{
			_timelineValue = value;
			if (_isScrubbing)
				_setVideoTimeDebounceTimer.Enabled = true;
		}
	}

	private Clip _clip;
	private VideoPlayer _videoPlayerFront;
	private VideoPlayer _videoPlayerLeftRepeater;
	private VideoPlayer _videoPlayerRightRepeater;
	private VideoPlayer _videoPlayerBack;
	private int _videoLoadedEventCount = 0;
	private bool _isPlaying;
	private ClipVideoSegment _currentSegment;
	private MudSlider<double> _timelineSlider;
	private double _timelineMaxSeconds;
	private double _ignoreTimelineValue;
	private bool _wasPlayingBeforeScrub;
	private bool _isScrubbing;
	private double _timelineValue;
	private System.Timers.Timer _setVideoTimeDebounceTimer;
	private CancellationTokenSource _loadSegmentCts = new();

	protected override void OnInitialized()
	{
		_setVideoTimeDebounceTimer = new(500);
		_setVideoTimeDebounceTimer.Elapsed += ScrubVideoDebounceTick;
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (!firstRender)
			return;

		_videoPlayerFront.Loaded += () =>
		{
			Console.WriteLine("Loaded: Front");
			_videoLoadedEventCount++;
		};
		_videoPlayerLeftRepeater.Loaded += () =>
		{
			Console.WriteLine("Loaded: Left");
			_videoLoadedEventCount++;
		};
		_videoPlayerRightRepeater.Loaded += () =>
		{
			Console.WriteLine("Loaded: Right");
			_videoLoadedEventCount++;
		};
		_videoPlayerBack.Loaded += () =>
		{
			Console.WriteLine("Loaded: Back");
			_videoLoadedEventCount++;
		};
	}

	private static Task AwaitUiUpdate()
		=> Task.Delay(100);

	public async Task SetClipAsync(Clip clip)
	{
		_clip = clip;
		TimelineValue = 0;
		_timelineMaxSeconds = (clip.EndDate - clip.StartDate).TotalSeconds;

		_currentSegment = _clip.Segments.First();
		await SetCurrentSegmentVideosAsync();
	}

	private async Task<bool> SetCurrentSegmentVideosAsync()
	{
		if (_currentSegment == null)
			return false;

		await _loadSegmentCts.CancelAsync();
		_loadSegmentCts = new();
		
		_videoLoadedEventCount = 0;
		var cameraCount = _currentSegment.CameraAnglesCount();

		var wasPlaying = _isPlaying;
		if (wasPlaying)
			await TogglePlayingAsync(false);
		
		_videoPlayerFront.Src = _currentSegment.CameraFront?.Url;
		_videoPlayerLeftRepeater.Src = _currentSegment.CameraLeftRepeater?.Url;
		_videoPlayerRightRepeater.Src = _currentSegment.CameraRightRepeater?.Url;
		_videoPlayerBack.Src = _currentSegment.CameraBack?.Url;

		if (_loadSegmentCts.IsCancellationRequested)
			return false;
		
		await InvokeAsync(StateHasChanged);

		var timeout = Task.Delay(10000);
		var completedTask = await Task.WhenAny(Task.Run(async () =>
		{
			while (_videoLoadedEventCount < cameraCount && !_loadSegmentCts.IsCancellationRequested)
				await Task.Delay(10, _loadSegmentCts.Token);
			
			Console.WriteLine("Loading done");
		}, _loadSegmentCts.Token), timeout);

		if (completedTask == timeout)
		{
			Console.WriteLine("Loading timed out");
			return false;
		}

		if (wasPlaying)
			await TogglePlayingAsync(true);

		return !_loadSegmentCts.IsCancellationRequested;
	}

	private async Task ExecuteOnPlayers(Func<VideoPlayer, Task> action)
	{
		try
		{
			await action(_videoPlayerFront);
			await action(_videoPlayerLeftRepeater);
			await action(_videoPlayerRightRepeater);
			await action(_videoPlayerBack);
		}
		catch
		{
			// ignore
		}
	}

	private Task TogglePlayingAsync(bool? play = null)
	{
		play ??= !_isPlaying;
		_isPlaying = play.Value;
		return ExecuteOnPlayers(async p => await (play.Value ? p.PlayAsync() : p.PauseAsync()));
	}

	private Task PlayPauseClicked()
		=> TogglePlayingAsync();

	private async Task VideoEnded()
	{
		if (_currentSegment == _clip.Segments.Last())
			return;

		await TogglePlayingAsync(false);

		var nextSegment = _clip.Segments
			.OrderBy(s => s.StartDate)
			.SkipWhile(s => s != _currentSegment)
			.Skip(1)
			.FirstOrDefault()
			?? _clip.Segments.FirstOrDefault();

		if (nextSegment == null)
		{
			await TogglePlayingAsync(false);
			return;
		}

		_currentSegment = nextSegment;
		await SetCurrentSegmentVideosAsync();
		await AwaitUiUpdate();
		await TogglePlayingAsync(true);
	}

	private async Task FrontVideoTimeUpdate()
	{
		if (_currentSegment == null)
			return;

		if (_isScrubbing)
			return;
		
		var seconds = await _videoPlayerFront.GetTimeAsync();
		var currentTime = _currentSegment.StartDate.AddSeconds(seconds);
		var secondsSinceClipStart = (currentTime - _clip.StartDate).TotalSeconds;
		
		_ignoreTimelineValue = secondsSinceClipStart;
		TimelineValue = secondsSinceClipStart;
	}

	private async Task TimelineSliderPointerDown()
	{
		_isScrubbing = true;
		_wasPlayingBeforeScrub = _isPlaying;
		await TogglePlayingAsync(false);
		
		// Allow value change event to trigger, then scrub before user releases mouse click
		await AwaitUiUpdate();
		await ScrubToSliderTime();
	}

	private async Task TimelineSliderPointerUp()
	{
		Console.WriteLine("Pointer up");
		await ScrubToSliderTime();
		_isScrubbing = false;
			
		if (!_isPlaying && _wasPlayingBeforeScrub)
			await TogglePlayingAsync(true);
	}

	private async void ScrubVideoDebounceTick(object _, ElapsedEventArgs __)
		=> await ScrubToSliderTime();

	private async Task ScrubToSliderTime()
	{
		_setVideoTimeDebounceTimer.Enabled = false;
		
		if (!_isScrubbing)
			return;

		try
		{
			var scrubToDate = _clip.StartDate.AddSeconds(TimelineValue);
			var segment = _clip.SegmentAtDate(scrubToDate)
				?? _clip.Segments.Where(s => s.StartDate > scrubToDate).MinBy(s => s.StartDate);

			if (segment == null)
				return;

			if (segment != _currentSegment)
			{
				_currentSegment = segment;
				if (!await SetCurrentSegmentVideosAsync())
					return;
			}

			var secondsIntoSegment = (scrubToDate - segment.StartDate).TotalSeconds;
			await ExecuteOnPlayers(async p => await p.SetTimeAsync(secondsIntoSegment));
		}
		catch
		{
			// ignore, happens sometimes
		}
	}

	private double DateTimeToTimelinePercentage(DateTime dateTime)
	{
		var percentage = Math.Round(dateTime.Subtract(_clip.StartDate).TotalSeconds / _clip.TotalSeconds * 100, 2);
		return Math.Clamp(percentage, 0, 100);
	}

	private string SegmentStartMargerStyle(ClipVideoSegment segment)
	{
		var percentage = DateTimeToTimelinePercentage(segment.StartDate);
		return $"left: {percentage}%";
	}

	private string EventMarkerStyle()
	{
		if (_clip?.Event?.Timestamp == null)
			return "display: none";

		var percentage = DateTimeToTimelinePercentage(_clip.Event.Timestamp);
		return $"left: {percentage}%";
	}
}