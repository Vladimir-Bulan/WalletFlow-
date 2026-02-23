FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Gateway/WalletFlow.Gateway/WalletFlow.Gateway.csproj", "src/Gateway/WalletFlow.Gateway/"]
RUN dotnet restore "src/Gateway/WalletFlow.Gateway/WalletFlow.Gateway.csproj"
COPY . .
RUN dotnet publish "src/Gateway/WalletFlow.Gateway/WalletFlow.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WalletFlow.Gateway.dll"]
