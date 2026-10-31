# Build context is the repo root: docker build -f Suwpptx.Server/Dockerfile -t suwpptx-server:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.23 AS build
WORKDIR /src
COPY .editorconfig global.json nuget.config Directory.Build.props Directory.Build.rsp Directory.Packages.props BannedSymbols.txt ./
COPY Suwpptx.Server/Suwpptx.Server.csproj Suwpptx.Server/packages.lock.json Suwpptx.Server/
RUN dotnet restore Suwpptx.Server/Suwpptx.Server.csproj --locked-mode
COPY Suwpptx.Server/ Suwpptx.Server/
RUN dotnet publish Suwpptx.Server/Suwpptx.Server.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-alpine3.23
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:6310
USER app
EXPOSE 6310
ENTRYPOINT ["dotnet", "Suwpptx.Server.dll"]
