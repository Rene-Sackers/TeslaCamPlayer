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
	private string _frontVideoSrc;
	private string _leftRepeaterVideoSrc;
	private string _rightRepeaterVideoSrc;
	private string _backVideoSrc;
	private VideoPlayer _videoPlayerFront;
	private VideoPlayer _videoPlayerLeftRepeater;
	private VideoPlayer _videoPlayerRightRepeater;
	private VideoPlayer _videoPlayerBack;
	private bool _isPlaying;
	private ClipVideoSegment _currentSegment;
	private MudSlider<double> _timelineSlider;
	private double _timelineMaxSeconds;
	private double _ignoreTimelineValue;
	private bool _wasPlayingBeforeScrub;
	private bool _isScrubbing;
	private double _timelineValue;
	private System.Timers.Timer _setVideoTimeDebounceTimer;

	protected override void OnInitialized()
	{
		_setVideoTimeDebounceTimer = new(100);
		_setVideoTimeDebounceTimer.Elapsed += ScrubVideoDebounceTick;
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (!firstRender)
			return;
	}

	public async Task SetClipAsync(Clip clip)
	{
		_clip = clip;
		TimelineValue = 0;
		_timelineMaxSeconds = (clip.EndDate - clip.StartDate).TotalSeconds;

		_currentSegment = _clip.Segments.First();
		SetCurrentSegmentVideos();

		if (_isPlaying)
		{
			// Let elements update
			await Task.Delay(100);
			await ToggleSetPlayingAsync(true);
		}
	}

	private void SetCurrentSegmentVideos()
	{
		if (_currentSegment == null)
			return;
		
		_frontVideoSrc = _currentSegment.CameraFront?.Url;
		_leftRepeaterVideoSrc = _currentSegment.CameraLeftRepeater?.Url;
		_rightRepeaterVideoSrc = _currentSegment.CameraRightRepeater?.Url;
		_backVideoSrc = _currentSegment.CameraBack?.Url;
		
		StateHasChanged();
	}

	private async Task ExecuteOnPlayers(Func<VideoPlayer, Task> player)
	{
		await player(_videoPlayerFront);
		await player(_videoPlayerLeftRepeater);
		await player(_videoPlayerRightRepeater);
		await player(_videoPlayerBack);
	}

	private async Task ToggleSetPlayingAsync(bool? play = null)
	{
		play ??= !_isPlaying;
		_isPlaying = play.Value;
		await ExecuteOnPlayers(async p => await (play.Value ? p.PlayAsync() : p.PauseAsync()));
	}

	private Task PlayPauseClicked()
		=> ToggleSetPlayingAsync();

	private async Task FrontVideoEnded()
	{
		if (_currentSegment == _clip.Segments.Last())
		{
			_isPlaying = false;
			return;
		}

		_currentSegment = _clip.Segments
			.SkipWhile(s => s != _currentSegment)
			.Skip(1)
			.First();
		SetCurrentSegmentVideos();
		await Task.Delay(10);
		await ToggleSetPlayingAsync(true);
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

	private async Task TimelineSliderMouseDown()
	{
		_isScrubbing = true;
		_wasPlayingBeforeScrub = _isPlaying;
		await ToggleSetPlayingAsync(false);
		
		// Allow value change event to trigger, then scrub before user releases mouse click
		await Task.Delay(10);
		await ScrubToSliderTime();
	}

	private async Task TimelineSliderMouseUp()
	{
		_isScrubbing = false;
		if (_wasPlayingBeforeScrub)
			await ToggleSetPlayingAsync(true);
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
			var segment = _clip.SegmentAtDate(scrubToDate);
			if (segment != _currentSegment)
			{
				_currentSegment = segment;
				await InvokeAsync(SetCurrentSegmentVideos);
			}

			var secondsIntoSegment = (scrubToDate - segment.StartDate).TotalSeconds;
			await InvokeAsync(async () => await ExecuteOnPlayers(async p => await p.SetTimeAsync(secondsIntoSegment)));
		}
		catch
		{
			// ignore, happens sometimes
		}
		
	}
}