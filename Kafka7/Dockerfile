FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Kafka7/Kafka7.csproj", "Kafka7/"]
RUN dotnet restore "Kafka7/Kafka7.csproj"
COPY . .
WORKDIR "/src/Kafka7"
RUN dotnet build "Kafka7.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kafka7.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kafka7.dll"]