using Microsoft.JSInterop;

namespace TeslaCamPlayer.BlazorHosted.Client.Factories;

public class VideoElementReferenceFactory
{
	private readonly IJSRuntime _jsRuntime;

	public VideoElementReferenceFactory(IJSRuntime jsRuntime)
	{
		_jsRuntime = jsRuntime;
	}
	
	
}