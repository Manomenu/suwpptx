# Build context is the repo root: docker build -f Suwpptx.Server/Dockerfile -t suwpptx-server:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.23 AS build
WORKDIR /src
COPY .editorconfig global.json nuget.config Directory.Build.props Directory.Build.rsp Directory.Packages.props BannedSymbols.txt ./
COPY Suwpptx.Server/Suwpptx.Server.csproj Suwpptx.Server/packages.lock.json Suwpptx.Server/
RUN dotnet restore Suwpptx.Server/Suwpptx.Server.csproj --locked-mode
COPY Suwpptx.Server/ Suwpptx.Server/
RUN dotnet publish Suwpptx.Server/Suwpptx.Server.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble
# The previews: LibreOffice renders slides to PDF (the app turns the pages into images itself), the fonts keep the layout
# (Carlito and Caladea are metric-compatible with Calibri and Cambria). The -nogui package runs without any display.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libreoffice-impress-nogui wget fonts-dejavu-core fonts-liberation2 fonts-crosextra-carlito fonts-crosextra-caladea fonts-noto-core \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:6310
# compose runs the container as the host user, who has no home here; soffice needs one that exists and is writable.
ENV HOME=/tmp
USER app
EXPOSE 6310
ENTRYPOINT ["dotnet", "Suwpptx.Server.dll"]
