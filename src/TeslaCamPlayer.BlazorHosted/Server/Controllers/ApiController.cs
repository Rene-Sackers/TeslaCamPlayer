using System.Web;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;
using TeslaCamPlayer.BlazorHosted.Server.Services.Interfaces;
using TeslaCamPlayer.BlazorHosted.Shared.Models;

namespace TeslaCamPlayer.BlazorHosted.Server.Controllers;

[ApiController]
[Route("Api/[action]")]
public class ApiController : ControllerBase
{
	private readonly IClipsService _clipsService;
	private readonly string _rootFullPath;

	public ApiController(ISettingsProvider settingsProvider, IClipsService clipsService)
	{
		_rootFullPath = Path.GetFullPath(settingsProvider.Settings.ClipsRootPath);
		_clipsService = clipsService;
	}

	[HttpGet]
	public async Task<Clip[]> GetClips()
		=> await _clipsService.GetClipsAsync();

	private bool IsUnderRootPath(string path)
		=> path.StartsWith(_rootFullPath);

	[HttpGet("{path}.mp4")]
	public IActionResult Video(string path)
		=> ServeFile(path, ".mp4", "video/mp4", true);

	[HttpGet("{path}.png")]
	public IActionResult Thumbnail(string path)
		=> ServeFile(path, ".png", "image/png");

	private IActionResult ServeFile(string path, string extension, string contentType, bool enableRangeProcessing = false)
	{
		path = HttpUtility.UrlDecode(path);
		path += extension;

		path = Path.GetFullPath(path);
		if (!IsUnderRootPath(path))
			return BadRequest($"File must be in subdirectory under \"{_rootFullPath}\", but was \"{path}\"");

		if (!System.IO.File.Exists(path))
			return NotFound();

		return PhysicalFile(path, contentType, enableRangeProcessing);
	}
}