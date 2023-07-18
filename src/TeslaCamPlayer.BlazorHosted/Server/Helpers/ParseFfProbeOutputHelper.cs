using System.Text.RegularExpressions;

namespace TeslaCamPlayer.BlazorHosted.Server.Helpers;

public static partial class ParseFfProbeOutputHelper
{
	[GeneratedRegex("Duration: (?<h>\\d{2}):(?<m>\\d{2}):(?<s>\\d{2})\\.(?<ms>\\d*)", RegexOptions.Compiled)]
	private static partial Regex DurationRegex();
	
	public static TimeSpan? GetDuration(string output)
	{
		using var reader = new StringReader(output);
		while (reader.ReadLine() is { } line)
		{
			if (!line.TrimStart().StartsWith("Duration: "))
				continue;
			
			var matches = DurationRegex().Match(line);
			if (!matches.Success)
				return null;
		
			return new TimeSpan(
				0,
				int.Parse(matches.Groups["h"].Value),
				int.Parse(matches.Groups["m"].Value),
				int.Parse(matches.Groups["s"].Value),
				int.Parse(matches.Groups["ms"].Value));
		}

		return null;
	}
}