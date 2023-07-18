using Microsoft.AspNetCore.Mvc;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;
using TeslaCamPlayer.BlazorHosted.Server.Services.Interfaces;
using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Server.Controllers;

[ApiController]
[Route("Api/[action]")]
public class ApiController : ControllerBase
{
	private readonly ISettingsProvider _settingsProvider;
	private readonly IClipsService _clipsService;

	public ApiController(ISettingsProvider settingsProvider, IClipsService clipsService)
	{
		_settingsProvider = settingsProvider;
		_clipsService = clipsService;
	}

	[HttpGet]
	public async Task<Clip[]> GetClips()
		=> await _clipsService.GetClipsAsync();

	[HttpGet("{path}.mp4")]
	public IActionResult Video(string path)
	{
		path += ".mp4";

		path = Path.GetFullPath(path);
		if (!path.StartsWith(_settingsProvider.Settings.ClipsRootPath))
			return BadRequest($"Video must be in subdirectory under \"{_settingsProvider.Settings.ClipsRootPath}\"");

		if (!System.IO.File.Exists(path))
			return NotFound();
		
		return PhysicalFile(path, "video/mp4", true);
	}

	[HttpGet("{path}.png")]
	public IActionResult Thumbnail(string path)
	{
		path += ".png";

		path = Path.GetFullPath(path);
		if (!path.StartsWith(_settingsProvider.Settings.ClipsRootPath))
			return BadRequest($"Thumbnail must be in subdirectory under \"{_settingsProvider.Settings.ClipsRootPath}\"");

		if (!System.IO.File.Exists(path))
			return NotFound();

		return PhysicalFile(path, "image/png");
	}
}