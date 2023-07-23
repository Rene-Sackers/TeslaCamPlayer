using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Client.Models
{
	public class EventFilterValues
	{
		public bool DashcamHonk { get; set; } = true;

		public bool DashcamSaved { get; set; } = true;

		public bool DashcamOther { get; set; } = true;

		public bool SentryObjectDetection { get; set; } = true;

		public bool SentryAccelerationDetection { get; set; } = true;

		public bool SentryOther { get; set; } = true;

		public bool Recent { get; set; } = true;

		public bool IsInFilter(Clip clip)
		{
			if (DashcamHonk && clip.Event.Reason == CamEvents.UserInteractionHonk)
				return true;

			if (DashcamSaved && (clip.Event.Reason == CamEvents.UserInteractionDashcamPanelSave || clip.Event.Reason == CamEvents.UserInteractionDashcamIconTapped))
				return true;

			if (DashcamOther && clip.Type == ClipType.Saved)
				return true;

			if (SentryObjectDetection && clip.Event.Reason == CamEvents.SentryAwareObjectDetection)
				return true;

			if (SentryAccelerationDetection && clip.Event.Reason.StartsWith(CamEvents.SentryAwareAccelerationPrefix))
				return true;

			if (SentryOther && clip.Type == ClipType.Sentry)
				return true;

			return Recent && clip.Type == ClipType.Recent;
		}
	}
}
