FROM mcr.microsoft.com/dotnet/aspnet:3.1-focal AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN apt-get update
RUN apt -y install software-properties-common
RUN apt-get install -y ffmpeg

RUN add-apt-repository ppa:nilarimogard/webupd8
RUN apt-get update
RUN apt install streamlink 

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:3.1-focal AS build
WORKDIR /src
COPY ["FfmpegPOC/FfmpegPOC.csproj", "FfmpegPOC/"]
RUN dotnet restore "FfmpegPOC/FfmpegPOC.csproj"
COPY . .
WORKDIR "/src/FfmpegPOC"
RUN dotnet build "FfmpegPOC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FfmpegPOC.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FfmpegPOC.dll"]
