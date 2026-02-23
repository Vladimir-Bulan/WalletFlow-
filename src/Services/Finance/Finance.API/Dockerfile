FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Finance/Finance.API/Finance.API.csproj", "src/Services/Finance/Finance.API/"]
COPY ["src/Services/Finance/Finance.Application/Finance.Application.csproj", "src/Services/Finance/Finance.Application/"]
COPY ["src/Services/Finance/Finance.Domain/Finance.Domain.csproj", "src/Services/Finance/Finance.Domain/"]
COPY ["src/Services/Finance/Finance.Infrastructure/Finance.Infrastructure.csproj", "src/Services/Finance/Finance.Infrastructure/"]
COPY ["src/Shared/WalletFlow.Contracts/WalletFlow.Contracts.csproj", "src/Shared/WalletFlow.Contracts/"]
RUN dotnet restore "src/Services/Finance/Finance.API/Finance.API.csproj"
COPY . .
RUN dotnet publish "src/Services/Finance/Finance.API/Finance.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Finance.API.dll"]
