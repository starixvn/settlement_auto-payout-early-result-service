FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/AutoPayout/*.csproj ./Sources/AutoPayout/
COPY ./Sources/AutoPayout.EarlyResult/*.csproj ./Sources/AutoPayout.EarlyResult/
COPY ./Sources/AutoPayout.Conditions/*.csproj ./Sources/AutoPayout.Conditions/
COPY ./Sources/AutoPayout.EarlyResult.Service/AutoPayout.EarlyResult.Service.csproj  ./Sources/AutoPayout.EarlyResult.Service/

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \ 
dotnet restore -r linux-x64 Sources/AutoPayout.EarlyResult.Service/AutoPayout.EarlyResult.Service.csproj 
  
COPY ./Sources/.  ./Sources

RUN ls /src -l
WORKDIR /src/Sources/AutoPayout.EarlyResult.Service

RUN --mount=type=cache,target=/root/.nuget/packages \
dotnet publish -r linux-x64 --self-contained false -c release -o /app 

FROM hub.nexdev.net/base/dotnet/core/aspnet:7.0-alpine AS base

WORKDIR /app
COPY --from=builder /app .

ENV TZ="America/La_Paz"
ENV COMPlus_EnableDiagnostics=0


# Don�t run as root
RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app && chown -R dotnetuser /tmp
USER dotnetuser
#


ENTRYPOINT ["dotnet", "AutoPayout.EarlyResult.Service.dll"]
