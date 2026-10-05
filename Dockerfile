FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# restore first so the layer is cached until a project file changes
COPY PersonalFinancialManagement.slnx ./
COPY src/PersonalFinancialManagement.Domain/*.csproj src/PersonalFinancialManagement.Domain/
COPY src/PersonalFinancialManagement.Application/*.csproj src/PersonalFinancialManagement.Application/
COPY src/PersonalFinancialManagement.Infrastructure/*.csproj src/PersonalFinancialManagement.Infrastructure/
COPY src/PersonalFinancialManagement.Api/*.csproj src/PersonalFinancialManagement.Api/
RUN dotnet restore src/PersonalFinancialManagement.Api/PersonalFinancialManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/PersonalFinancialManagement.Api/PersonalFinancialManagement.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Hosts such as Render pass the port to listen on in $PORT.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet PersonalFinancialManagement.Api.dll"]
