FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY . /src
WORKDIR "/src/TeslaCamPlayer.BlazorHosted/Server"
RUN dotnet restore .
RUN dotnet build *.csproj -c Release -o /app/build /p:DefineConstants=DOCKER

FROM build AS publish
RUN dotnet publish *.csproj -c Release -o /app/publish /p:DefineConstants=DOCKER

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TeslaCamPlayer.BlazorHosted.Server.dll"]
