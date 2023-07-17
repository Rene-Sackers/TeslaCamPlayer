using TeslaCamPlayer.BlazorHosted.Server.Models;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;

namespace TeslaCamPlayer.BlazorHosted.Server.Providers;

public class SettingsProvider : ISettingsProvider
{
	public Settings Settings => _settings.Value;

	private readonly Lazy<Settings> _settings = new(SettingsValueFactory);

	private static Settings SettingsValueFactory() =>
		new ConfigurationBuilder()
			.AddEnvironmentVariables()
			.Build()
			.Get<Settings>();
}