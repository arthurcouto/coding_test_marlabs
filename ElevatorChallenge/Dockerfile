# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution file and project files first to cache restoring dependencies
COPY *.slnx .
COPY Elevator.Domain/*.csproj ./Elevator.Domain/
COPY Elevator.Core/*.csproj ./Elevator.Core/
COPY Elevator.App/*.csproj ./Elevator.App/
COPY Elevator.Tests/*.csproj ./Elevator.Tests/

# Restore all NuGet packages
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and publish the console application to out/ folder
WORKDIR /app/Elevator.App
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy the published artifacts from the build stage
COPY --from=build /app/publish .

# Run the interactive console application
ENTRYPOINT ["dotnet", "Elevator.App.dll"]
