FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/AutoSettlement/*.csproj ./Sources/AutoSettlement/
COPY ./Sources/Fanex.Repository/*.csproj ./Sources/Fanex.Repository/
COPY ./Sources/Fanex.AutoSettlement.Service.csproj  ./Fanex.AutoSettlement.Service/

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \
  dotnet restore -r linux-x64 Fanex.AutoSettlement.Service/Fanex.AutoSettlement.Service.csproj 
  
COPY ./Sources/AutoSettlement ./Sources/AutoSettlement
COPY ./Sources/Fanex.Repository ./Sources/Fanex.Repository
COPY ./Sources  ./Fanex.AutoSettlement.Service


WORKDIR /src/Fanex.AutoInstant.Service
RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN dotnet restore -v n

COPY ./Sources/Fanex.AutoSettlement.Service .

RUN dotnet publish -r linux-x64 --self-contained false -c release -o /app 

FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENTRYPOINT ["dotnet", "Fanex.AutoSettlement.Service.dll"]
