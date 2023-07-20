FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY . /src
WORKDIR "/src/TeslaCamPlayer.BlazorHosted/Server"
RUN dotnet restore .
RUN dotnet build *.csproj -c Release -o /app/build /p:DefineConstants=DOCKER

FROM build AS publish
RUN dotnet publish *.csproj -c Release -o /app/publish /p:DefineConstants=DOCKER

FROM node:17 AS gulp
WORKDIR /src
COPY TeslaCamPlayer.BlazorHosted/Client/package.json .
COPY TeslaCamPlayer.BlazorHosted/Client/gulpfile.js .
COPY TeslaCamPlayer.BlazorHosted/Client/wwwroot/scss/ ./wwwroot/scss/
RUN npm install
RUN npm install -g gulp
RUN gulp default

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
RUN apt-get update && apt-get install -y ffmpeg
WORKDIR /app
ENV ClipsRootPath=/TeslaCam
COPY --from=publish /app/publish .
COPY --from=gulp /src/wwwroot/css/ ./wwwroot/css/

ENTRYPOINT ["dotnet", "TeslaCamPlayer.BlazorHosted.Server.dll"]
