# ── Build stage ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so package restore is cached
# separately from source changes.
COPY NayanCareWeb.csproj ./
RUN dotnet restore "NayanCareWeb.csproj"

# Now copy the rest of the source and publish.
COPY . .
RUN dotnet publish "NayanCareWeb.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as a non-root user.
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

# The app listens on 8080 inside the container (see ASPNETCORE_URLS below).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "NayanCareWeb.dll"]
