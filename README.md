# TeslaCamPlayer

[![Release](https://github.com/Rene-Sackers/TeslaCamPlayer/actions/workflows/release.yml/badge.svg)](https://github.com/Rene-Sackers/TeslaCamPlayer/actions/workflows/release.yml)

GitHub: https://github.com/Rene-Sackers/TeslaCamPlayer

A Blazor WASM application for easily viewing locally stored Tesla sentry & dashcam videos.

Works great in combination with [TeslaUSB](https://github.com/marcone/teslausb).

First release, still needs some work, but functional and might be just what you needed :)

![Screenshot](readme/screenshot.png)

## Features

### Implemented

- Infinite scrolling list of events (virtualized)
- Icons to easily identify events (sentry/dashcam/honk/movement detected/manual save)
- Calendar to easily go to a certain date
- Auto scaling viewer
- Supports MCU-1 (3 camera, missing back cam) and/or missing/corrupt angles
- Event time marker on timeline
- Filtering events
- RecentClips viewing

### TODO/missing

- Mobile viewport support
- Progress bar for loading. Initial load checks the length of each video file, this may take a moment.
- Map for event location
- Exporting clips
- General small issues

## Windows

The Windows build is a self-contained .exe, you do not need to install .NET, as it's compiled into the executable.

Download the latest version from [releases](https://github.com/Rene-Sackers/TeslaCamPlayer/releases/tag/v2023.7.23.1431) (teslacamplayer-win-x64-\*.zip) and extract the zip.

Modify `appsettings.json`, change the value for `ClipsRootPath`, set it to the path that contains your TeslaCam videos, and escape the \\ directory character with another \\ For example:

```json
{
	...
	"AllowedHosts": "*",
	"ClipsRootPath": "D:\\Some\\Folder\\TeslaCam"
}

```

Run `TeslaCamPlayer.BlazorHosted.Server.exe` and navigate to `http://localhost:5000` in your browser.

## Docker

```
docker run \
	-e ClipsRootPath=/TeslaCam \
	-v D:\\TeslaCam\\:/TeslaCam \
	-p 80:80 \
	teslacam
```

### Environment variables

| Variable      | Example   | Description                                                                                                                                          |
| ------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| ClipsRootPath | /TeslaCam | The path to the root of the clips mount in the container. This is set by default, you do not need to change it if you mount the volume to this path. |

### Volumes

| Volume    | Description                                                                                                                                          |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| /TeslaCam | Should contain the event folders with the camera files (eg. RecentClips, SavedClips, SentryClips) Mounts to the `ClipsRootPath` environment variable |

### Ports

| Port | Description                 |
| ---- | --------------------------- |
| 80   | The HTTP web interface port |

# Legal

pls no sue kthx

## Tesla

This software is in no way, shape or form affiliated with Tesla, Inc. (https://www.tesla.com/), or its products.
This software is not:

- An official Tesla product
- Licensed by Tesla
- Built by or in conjunction with Tesla
- Commisioned by Tesla

It does not directly impact Tesla products. It is an aftermarket piece of software that processes data produced by Tesla vehicles.

## FFmpeg

This software uses libraries from the FFmpeg project under the LGPLv2.1  
The Windows build of this software includes a copy of ffprobe.exe, compiled by: https://github.com/BtbN/FFmpeg-Builds/releases  
More info and sources for FFmpeg can be found on: https://ffmpeg.org/

[def]: releases
