# Stage 1: base (uses target platform architecture for ASP.NET runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Stage 2: build/publish (SDK runs natively on host platform for speed)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG TARGETOS

WORKDIR /src

# Copy project files and source
COPY . .

# Drop the Windows-only local NuGet source from nuget.config (not available in the container)
RUN dotnet nuget remove source local --configfile nuget.config

# Use bash to map amd64 -> x64 and build for the target RID cleanly
RUN /bin/bash -c '\
    RID_ARCH="${TARGETARCH/amd64/x64}" && \
    dotnet publish "PleiadesApi/PleiadesApi.csproj" \
    -c Release \
    -r "${TARGETOS:-linux}-${RID_ARCH}" \
    --no-self-contained \
    -o /app/publish'

# Stage 3: final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PleiadesApi.dll"]
