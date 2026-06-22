FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN find . -name "Dockerfile" -type f -delete

COPY ["Presentation/Presentation.csproj", "Presentation/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Domain/Domain.csproj", "Domain/"]

RUN dotnet restore "Presentation/Presentation.csproj"

COPY . .
RUN find . -name "Dockerfile" -type f -delete

WORKDIR /src/RealtyAnalizator
RUN dotnet build "Presentation.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080
ENTRYPOINT ["dotnet", "OzonProductService.dll"]