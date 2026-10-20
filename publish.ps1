# Produces a single self-contained exe (no .NET install needed) in .\publish
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
Write-Host "Done: .\publish\FloatingKeys.exe"
