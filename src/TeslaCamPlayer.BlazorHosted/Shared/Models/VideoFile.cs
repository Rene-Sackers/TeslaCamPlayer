namespace TeslaCamPlayer.BlazorHosted.Shared.Models;

public class VideoFile
{
	public string FilePath { get; init; }
	public string Url { get; init; }
	public string EventFolderName { get; init; }
	public ClipType ClipType { get; init; }
	public DateTime StartDate { get; init; }
	public Cameras Camera { get; init; }
	public TimeSpan Duration { get; init; }
}