#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["pnp-basic/pnp-basic.csproj", "pnp-basic/"]
RUN dotnet restore "pnp-basic/pnp-basic.csproj"
COPY . .
WORKDIR "/src/pnp-basic"
RUN dotnet build "pnp-basic.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "pnp-basic.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pnp-basic.dll"]