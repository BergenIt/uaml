FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
RUN apt-get update
RUN apt-get install -y libfreetype6
RUN apt-get install -y libfontconfig1

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["yaml-moq/yaml-moq.csproj", "yaml-moq/"]
RUN dotnet restore "yaml-moq/yaml-moq.csproj"
COPY . .
WORKDIR "/src/yaml-moq"
RUN dotnet build "yaml-moq.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "yaml-moq.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/build/yaml-moq.xml .
ENTRYPOINT ["dotnet", "yaml-moq.dll"]
