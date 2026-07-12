$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repositoryRoot "src/FundingMonitor.Api/FundingMonitor.Api.csproj"
$frontendRoot = Join-Path $repositoryRoot "frontend/FundingMonitor.Web"
$expectedSchema = Join-Path $repositoryRoot "openapi/funding-monitor-v1.json"
$expectedTypes = Join-Path $frontendRoot "src/types/generated/api.ts"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("funding-monitor-openapi-" + [Guid]::NewGuid().ToString("N"))
$actualSchema = Join-Path $temporaryRoot "funding-monitor-v1.json"
$actualTypes = Join-Path $temporaryRoot "api.ts"

function Assert-SameFile([string] $Expected, [string] $Actual, [string] $Label) {
    $expectedHash = (Get-FileHash -LiteralPath $Expected -Algorithm SHA256).Hash
    $actualHash = (Get-FileHash -LiteralPath $Actual -Algorithm SHA256).Hash
    if ($expectedHash -ne $actualHash) {
        throw "$Label устарел. Обновите OpenAPI schema и generated TypeScript types."
    }
}

try {
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

    & dotnet run --project $apiProject --no-restore -- --export-openapi $actualSchema
    if ($LASTEXITCODE -ne 0) { throw "Не удалось экспортировать OpenAPI schema." }

    $generator = Join-Path $frontendRoot "node_modules/.bin/openapi-typescript.cmd"
    & $generator $actualSchema -o $actualTypes
    if ($LASTEXITCODE -ne 0) { throw "Не удалось сгенерировать TypeScript types." }

    Assert-SameFile $expectedSchema $actualSchema "OpenAPI schema"
    Assert-SameFile $expectedTypes $actualTypes "Generated TypeScript contract"
    Write-Host "OpenAPI schema и frontend types актуальны."
}
finally {
    $resolvedTempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    $temporaryLeaf = Split-Path -Leaf $resolvedTemporaryRoot
    if ($resolvedTemporaryRoot.StartsWith($resolvedTempBase, [StringComparison]::OrdinalIgnoreCase) -and
        $temporaryLeaf.StartsWith("funding-monitor-openapi-", [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
