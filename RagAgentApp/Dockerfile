# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["RagAgentApp.csproj", "./"]
RUN dotnet restore "RagAgentApp.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
RUN dotnet build "RagAgentApp.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "RagAgentApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy the published application
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "RagAgentApp.dll"]
