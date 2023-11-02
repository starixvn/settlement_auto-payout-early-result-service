FROM hub.nexdev.net/base/dotnet/core/sdk:7.0-alpine AS builder
WORKDIR /src

COPY ./Sources/Fanex.AutoPayoutSettlement.Core/*.csproj ./Sources/Fanex.AutoPayoutSettlement.Core/
COPY ./Sources/AutoPayoutSettlement/*.csproj ./Sources/AutoPayoutSettlement/
COPY ./Sources/Fanex.Library/*.csproj ./Sources/Fanex.Library/
COPY ./Sources/Fanex.AutoPayoutSettlement.Service/Fanex.AutoPayoutSettlement.Service.csproj  ./Sources/Fanex.AutoPayoutSettlement.Service/

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2
RUN --mount=type=cache,target=/root/.nuget/packages \ 
dotnet restore -r linux-x64 Sources/Fanex.AutoPayoutSettlement.Service/Fanex.AutoPayoutSettlement.Service.csproj 
  
COPY ./Sources/.  ./Sources

RUN ls /src -l
WORKDIR /src/Sources/Fanex.AutoPayoutSettlement.Service

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


ENTRYPOINT ["dotnet", "Fanex.AutoPayoutSettlement.Service.dll"]
