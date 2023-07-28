FROM mcr.microsoft.com/dotnet/sdk:7.0 AS publish
COPY . /src
WORKDIR "/src/TeslaCamPlayer.BlazorHosted/Server"
RUN dotnet restore .
RUN dotnet publish *.csproj -c Release -o /app/publish /p:DefineConstants=DOCKER
# Remove ffprobe.exe from docker builds. It relies on an apt package instead.
RUN rm -r /app/publish/lib/

FROM node:17 AS gulp
WORKDIR /src
COPY TeslaCamPlayer.BlazorHosted/Client/package.json .
COPY TeslaCamPlayer.BlazorHosted/Client/gulpfile.js .
COPY TeslaCamPlayer.BlazorHosted/Client/wwwroot/scss/ ./wwwroot/scss/
RUN npm install
RUN npm install -g gulp
RUN gulp default

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
RUN apt-get update && apt-get install -y ffmpeg --no-install-recommends
WORKDIR /app
ENV ClipsRootPath=/TeslaCam
EXPOSE 80/tcp
COPY --from=publish /app/publish .
COPY --from=gulp /src/wwwroot/css/ ./wwwroot/css/

ENTRYPOINT ["dotnet", "TeslaCamPlayer.BlazorHosted.Server.dll"]
