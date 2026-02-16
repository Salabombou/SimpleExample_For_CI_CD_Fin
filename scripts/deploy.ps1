# ====================================
# Azure Deployment Script
# ====================================

param(
    [Parameter(Mandatory=$true)]
    [string]$resourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$appServiceName
)

Write-Host "Building application..." -ForegroundColor Cyan
dotnet publish SimpleExample.API\SimpleExample.API.csproj --configuration Release --output .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Creating ZIP package..." -ForegroundColor Cyan
if (Test-Path .\app.zip) { Remove-Item .\app.zip }
Compress-Archive -Path .\publish\* -DestinationPath .\app.zip -Force

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deployment source config-zip `
  --name $appServiceName `
  --resource-group $resourceGroup `
  --src app.zip

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Restarting app..." -ForegroundColor Cyan
az webapp restart --name $appServiceName --resource-group $resourceGroup

Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "URL: https://$appServiceName.azurewebsites.net/swagger" -ForegroundColor Green

# Cleanup
Remove-Item .\publish -Recurse -Force
Remove-Item .\app.zip
