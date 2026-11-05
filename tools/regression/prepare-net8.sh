#!/bin/bash
# Makes a runnable .NET 8 copy of the BeDiagram project for click-testing where the .NET 10 Blazor packages can't be
# downloaded (Claude's cloud workspace). App code is copied unchanged; only the hosting bits that .NET 8 lacks change.
# Usage: prepare-net8.sh <BeDiagram project folder> <destination folder>
set -euo pipefail
src=$(realpath "$1"); dst=$2
rm -rf "$dst"; mkdir -p "$dst"
cp -r "$src"/. "$dst"/
cd "$dst"
rm -rf bin obj Components/Layout/ReconnectModal.razor*

# net10 -> net8, no native app host (needs a NuGet package)
sed -i 's#net10.0#net8.0#; /BlazorDisableThrowNavigationException/d; s#<Nullable>#<UseAppHost>false</UseAppHost><Nullable>#' BeDiagram.csproj

# Plain .NET 8 host page: no @Assets, ImportMap, ResourcePreloader or ReconnectModal
cat > Components/App.razor <<'HTML'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <base href="/" />
    <link rel="stylesheet" href="app.css" />
    <link rel="stylesheet" href="BeDiagram.styles.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
HTML
sed -i 's# NotFoundPage="typeof(Pages.NotFound)"##' Components/Routes.razor
sed -i 's#app.MapStaticAssets();#app.UseStaticFiles();#; /app.UseStatusCodePagesWithReExecute/d' Program.cs

mkdir -p /tmp/empty-nuget-source
dotnet build --source /tmp/empty-nuget-source 2>&1 | grep -E " error |Build succeeded" | sort -u
