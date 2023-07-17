using Newtonsoft.Json;

namespace TeslaCamPlayer.BlazorHosted.Shared.Models;

/// <summary>
/// According to a random post on the internet
/// 0 = front camera
/// 1 = fisheye
/// 2 = narrow
/// 3 = left repeater
/// 4 = right repeater
/// 5 = left B pillar
/// 6 = right B pillar
/// 7 = rear
/// 8 = cabin
/// </summary>
public class Event
{
	[JsonProperty("timestamp")]
	public DateTime Timestamp;
	[JsonProperty("city")]
	public string City;
	[JsonProperty("est_lat")]
	public string EstLat;
	[JsonProperty("est_lon")]
	public string EstLon;
	[JsonProperty("reason")]
	public string Reason;
	[JsonProperty("camera")]
	public Cameras Camera;
}
