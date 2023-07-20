using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Timers;
using Microsoft.AspNetCore.Components.Web;
using TeslaCamPlayer.BlazorHosted.Client.Components;
using TeslaCamPlayer.BlazorHosted.Client.Helpers;
using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Client.Pages;

public partial class Index : ComponentBase
{
	private const int EventItemHeight = 60;

	[Inject]
	private HttpClient HttpClient { get; set; }

	[Inject]
	private IJSRuntime JsRuntime { get; set; }

	private Clip[] _clips;
	private HashSet<DateTime> _eventDates;
	private MudDatePicker _datePicker;
	private bool _setDatePickerInitialDate;
	private ElementReference _eventsList;
	private System.Timers.Timer _scrollDebounceTimer;
	private DateTime _ignoreDatePicked;
	private Clip _activeClip;
	private ClipViewer _clipViewer;

	protected override async Task OnInitializedAsync()
	{
		_scrollDebounceTimer = new(100);
		_scrollDebounceTimer.Elapsed += ScrollDebounceTimerTick;
		_clips = await HttpClient.GetFromNewtonsoftJsonAsync<Clip[]>("Api/GetClips");

		_eventDates = _clips
			.Select(c => c.StartDate.Date)
			.Concat(_clips.Select(c => c.EndDate.Date))
			.Distinct()
			.ToHashSet();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_setDatePickerInitialDate && _clips?.Any() == true && _datePicker != null)
		{
			_setDatePickerInitialDate = true;
			var latestClip = _clips.MaxBy(c => c.EndDate)!;
			await _datePicker.GoToDate(latestClip.EndDate);
			await SetActiveClip(latestClip);
		}
	}
	
	private bool IsDateDisabledFunc(DateTime date)
		=> !_eventDates.Contains(date);

	private static string[] GetClipIcons(Clip clip)
	{
		// sentry_aware_object_detection
		// user_interaction_honk
		// user_interaction_dashcam_panel_save
		// user_interaction_dashcam_icon_tapped
		// sentry_aware_accel_0.532005

		var baseIcon = clip.Type switch {
			ClipType.Recent => Icons.Material.Filled.History,
			ClipType.Saved => Icons.Material.Filled.CameraAlt,
			ClipType.Sentry => Icons.Material.Filled.RadioButtonChecked,
			_ => Icons.Material.Filled.QuestionMark
		};

		if (clip.Type == ClipType.Recent || clip.Type == ClipType.Unknown || clip.Event == null)
			return new[] { baseIcon };

		var secondIcon = clip.Event.Reason switch
		{
			"sentry_aware_object_detection" => Icons.Material.Filled.Animation,
			"user_interaction_honk" => Icons.Material.Filled.Campaign,
			"user_interaction_dashcam_panel_save" => Icons.Material.Filled.Archive,
			_ => null
		};

		if (clip.Event.Reason.StartsWith("sentry_aware_accel_"))
			secondIcon = Icons.Material.Filled.OpenWith;

		return secondIcon == null ? new [] { baseIcon } : new[] { baseIcon, secondIcon };
	}

	private class ScrollToOptions
	{
		public int? Left { get; set; }

		public int? Top { get; set; }

		public string Behavior { get; set; }
	}

	private async Task DatePicked(DateTime? pickedDate)
	{
		if (!pickedDate.HasValue || _ignoreDatePicked == pickedDate)
			return;

		var firstClipAtDate = _clips.FirstOrDefault(c => c.StartDate.Date == pickedDate);
		if (firstClipAtDate == null)
			return;

		await SetActiveClip(firstClipAtDate);
		await ScrollListToActiveClip();
		await Task.Delay(500);
	}

	private async Task ScrollListToActiveClip()
	{
		var listBoundingRect = await _eventsList.MudGetBoundingClientRectAsync();
		var index = Array.IndexOf(_clips, _activeClip);
		var top = (int)(index * EventItemHeight - listBoundingRect.Height / 2 + EventItemHeight / 2);

		await JsRuntime.InvokeVoidAsync("HTMLElement.prototype.scrollTo.call", _eventsList, new ScrollToOptions
		{
			Behavior = "smooth",
			Top = top
		});
	}

	private async Task SetActiveClip(Clip clip)
	{
		_activeClip = clip;
		await _clipViewer.SetClipAsync(_activeClip);
		_ignoreDatePicked = clip.StartDate.Date;
		_datePicker.Date = clip.StartDate.Date;
	}

	private void EventListScrolled()
	{
		if (!_scrollDebounceTimer.Enabled)
			_scrollDebounceTimer.Enabled = true;
	}

	private async void ScrollDebounceTimerTick(object _, ElapsedEventArgs __)
	{
		var scrollTop = await JsRuntime.InvokeAsync<double>("getProperty", _eventsList, "scrollTop");
		var listBoundingRect = await _eventsList.MudGetBoundingClientRectAsync();
		var centerScrollPosition = scrollTop + listBoundingRect.Height / 2 + EventItemHeight / 2;
		var itemIndex = (int)centerScrollPosition / EventItemHeight;
		var atClip = _clips.ElementAt(Math.Min(_clips.Length - 1, itemIndex));

		_ignoreDatePicked = atClip.StartDate.Date;
		await _datePicker.GoToDate(atClip.StartDate.Date);

		_scrollDebounceTimer.Enabled = false;
	}

	private async Task PreviousButtonClicked()
	{
		// Go to an OLDER clip, so start date should be GREATER than current
		var previous = _clips
			.OrderByDescending(c => c.StartDate)
			.FirstOrDefault(c => c.StartDate < _activeClip.StartDate);

		if (previous != null)
		{
			await SetActiveClip(previous);
			await ScrollListToActiveClip();
		}
	}

	private async Task NextButtonClicked()
	{
		// Go to a NEWER clip, so start date should be LESS than current
		var next = _clips
			.OrderBy(c => c.StartDate)
			.FirstOrDefault(c => c.StartDate > _activeClip.StartDate);

		if (next != null)
		{
			await SetActiveClip(next);
			await ScrollListToActiveClip();
		}
	}

	private async Task DatePickerOnMouseWheel(WheelEventArgs e)
	{
		if (e.DeltaY == 0 && e.DeltaX == 0 || !_datePicker.PickerMonth.HasValue)
			return;

		var goToNextMonth = e.DeltaY + e.DeltaX * -1 < 0;
		var targetDate = _datePicker.PickerMonth.Value.AddMonths(goToNextMonth ? 1 : -1);
		var endOfMonth = targetDate.AddMonths(1);

		var clipsInOrAfterTargetMonth = _clips.Any(c => c.StartDate >= targetDate);
		var clipsInOrBeforeTargetMonth = _clips.Any(c => c.StartDate <= endOfMonth);
		
		if (goToNextMonth && !clipsInOrAfterTargetMonth)
			return;
		
		if (!goToNextMonth && !clipsInOrBeforeTargetMonth)
			return;
		
		_ignoreDatePicked = targetDate;
		await _datePicker.GoToDate(targetDate);
	}
}