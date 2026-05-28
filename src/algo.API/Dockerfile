FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/algo.API/algo.API.csproj", "src/algo.API/"]
COPY ["src/algo.Application/algo.Application.csproj", "src/algo.Application/"]
COPY ["src/algo.Domain/algo.Domain.csproj", "src/algo.Domain/"]
COPY ["src/algo.Infrastructure/algo.Infrastructure.csproj", "src/algo.Infrastructure/"]
COPY ["src/algo.Persistence/algo.Persistence.csproj", "src/algo.Persistence/"]
COPY ["src/algo.RealTime/algo.RealTime.csproj", "src/algo.RealTime/"]
COPY ["src/algo.SharedKernel/algo.SharedKernel.csproj", "src/algo.SharedKernel/"]

RUN dotnet restore "src/algo.API/algo.API.csproj"

COPY . .
WORKDIR /src/src/algo.API
RUN dotnet publish "algo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "algo.API.dll"]
