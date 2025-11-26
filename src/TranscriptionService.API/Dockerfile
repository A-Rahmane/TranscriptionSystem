FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/TranscriptionService.API/TranscriptionService.API.csproj", "TranscriptionService.API/"]
COPY ["src/TranscriptionService.Application/TranscriptionService.Application.csproj", "TranscriptionService.Application/"]
COPY ["src/TranscriptionService.Domain/TranscriptionService.Domain.csproj", "TranscriptionService.Domain/"]
COPY ["src/TranscriptionService.Infrastructure/TranscriptionService.Infrastructure.csproj", "TranscriptionService.Infrastructure/"]
COPY ["src/TranscriptionService.Contracts/TranscriptionService.Contracts.csproj", "TranscriptionService.Contracts/"]

# Restore dependencies
RUN dotnet restore "TranscriptionService.API/TranscriptionService.API.csproj"

# Copy all source files
COPY src/ .

# Build
WORKDIR "/src/TranscriptionService.API"
RUN dotnet build "TranscriptionService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TranscriptionService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create necessary directories
RUN mkdir -p /app/transcription-files /app/models

ENTRYPOINT ["dotnet", "TranscriptionService.API.dll"]
