namespace TeslaCamPlayer.BlazorHosted.Shared.Models;

public class ClipVideoSegment
{
	public DateTime StartDate { get; init; }
	public DateTime EndDate { get; init; }
	public VideoFile CameraFront { get; init; }
	public VideoFile CameraLeftRepeater { get; init; }
	public VideoFile CameraRightRepeater { get; init; }
	public VideoFile CameraBack { get; init; }
	
}