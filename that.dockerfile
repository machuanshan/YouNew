FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY YouNewThat ./YouNewThat
COPY YouNewAll ./YouNewAll
WORKDIR /src/YouNewThat
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish YouNewThat.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YouNewThat.dll"]