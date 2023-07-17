using TeslaCamPlayer.BlazorHosted.Server.Models;

namespace TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;

public interface ISettingsProvider
{
	Settings Settings { get; }
}