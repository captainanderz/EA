#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM node:15.4-alpine3.12 AS client 
WORKDIR /app 
COPY ProjectHorizon.WebAPI/ClientApp . 
RUN npm install
RUN npm run-script build

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["ProjectHorizon.WebAPI/ProjectHorizon.WebAPI.csproj", "ProjectHorizon.WebAPI/"]
RUN dotnet restore "ProjectHorizon.WebAPI/ProjectHorizon.WebAPI.csproj"
COPY . .
WORKDIR "/src/ProjectHorizon.WebAPI"
RUN dotnet build "ProjectHorizon.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectHorizon.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=client /app/dist ClientApp/dist
ENTRYPOINT ["dotnet", "ProjectHorizon.WebAPI.dll"]