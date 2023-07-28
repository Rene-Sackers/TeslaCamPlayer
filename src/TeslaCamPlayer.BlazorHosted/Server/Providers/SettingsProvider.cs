using TeslaCamPlayer.BlazorHosted.Server.Models;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;

namespace TeslaCamPlayer.BlazorHosted.Server.Providers;

public class SettingsProvider : ISettingsProvider
{
	public Settings Settings => _settings.Value;

	private readonly Lazy<Settings> _settings = new(SettingsValueFactory);

	private static Settings SettingsValueFactory() =>
		new ConfigurationBuilder()
			.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
			.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"), optional: true)
			.AddEnvironmentVariables()
			.Build()
			.Get<Settings>();
}