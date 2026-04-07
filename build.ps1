$ErrorActionPreference = "Stop"

$currentDir = $PSScriptRoot
$uiProject = Join-Path $currentDir "src\GitBuddy.UI\GitBuddy.UI.csproj"
$uiPublishDir = Join-Path $currentDir "src\GitBuddy.UI\bin\Release\net9.0\publish\wwwroot"
$extensionBlazorDir = Join-Path $currentDir "src\extension\blazor-app"

Write-Host "Building Blazor WebAssembly Project..." -ForegroundColor Cyan
dotnet publish $uiProject -c Release

Write-Host "Recreating extension blazor-app directory..." -ForegroundColor Cyan
if (Test-Path $extensionBlazorDir) {
    Remove-Item -Recurse -Force $extensionBlazorDir
}
New-Item -ItemType Directory -Path $extensionBlazorDir | Out-Null

Write-Host "Copying Blazor app to VS Code Extension folder..." -ForegroundColor Cyan
Copy-Item "$uiPublishDir\*" "$extensionBlazorDir\" -Recurse -Force

Write-Host "Build and Copy Complete!" -ForegroundColor Green
