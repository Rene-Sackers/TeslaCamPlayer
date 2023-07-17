using Microsoft.AspNetCore.Mvc;
using BlazorHostedExample.Shared;

namespace BlazorHostedExample.Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class CamApiController : ControllerBase
{
	private static readonly string[] Summaries = new[]
	{
		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	};


	[HttpGet]
	public IEnumerable<WeatherForecast> GetEvents()
	{
		return Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToArray();
	}
}