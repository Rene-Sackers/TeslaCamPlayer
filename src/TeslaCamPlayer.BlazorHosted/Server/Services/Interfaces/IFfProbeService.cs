namespace TeslaCamPlayer.BlazorHosted.Server.Services.Interfaces;

public interface IFfProbeService
{
	public Task<TimeSpan?> GetVideoFileDurationAsync(string videoFilePath);
}