FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Install dependencies for Whisper.cpp
RUN apt-get update && apt-get install -y \
    wget \
    build-essential \
    git \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/TranscriptionService.Worker/TranscriptionService.Worker.csproj", "TranscriptionService.Worker/"]
COPY ["src/TranscriptionService.Application/TranscriptionService.Application.csproj", "TranscriptionService.Application/"]
COPY ["src/TranscriptionService.Domain/TranscriptionService.Domain.csproj", "TranscriptionService.Domain/"]
COPY ["src/TranscriptionService.Infrastructure/TranscriptionService.Infrastructure.csproj", "TranscriptionService.Infrastructure/"]
COPY ["src/TranscriptionService.Contracts/TranscriptionService.Contracts.csproj", "TranscriptionService.Contracts/"]

# Restore dependencies
RUN dotnet restore "TranscriptionService.Worker/TranscriptionService.Worker.csproj"

# Copy source code
COPY src/ .

# Build
WORKDIR "/src/TranscriptionService.Worker"
RUN dotnet build "TranscriptionService.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TranscriptionService.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for models and files
RUN mkdir -p /var/models/whisper /var/transcription-files

ENTRYPOINT ["dotnet", "TranscriptionService.Worker.dll"]
