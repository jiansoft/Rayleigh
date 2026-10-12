# Compare the actual committed library with the current library, using extern alias.
[CmdletBinding()]
param(
    [switch]$PrepareOnly,
    [ValidateRange(1, 20)][int]$LaunchCount = 2,
    [ValidateRange(1, 100)][int]$WarmupCount = 6,
    [ValidateRange(3, 100)][int]$IterationCount = 12,
    [ValidateRange(100, 10000)][int]$IterationTime = 200
)

$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskRevision = '6dd8b1660f74ae6f57bbf7f4d5445dffa2be303e'
$taskBaseline = Join-Path $taskRepo 'artifacts/performance-baseline'
$taskRun = Join-Path $taskRepo ('artifacts/prior-changes-audit/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$taskSettings = @{ DOTNET_CLI_USE_MSBUILD_SERVER = '0'; MSBUILDDISABLENODEREUSE = '1'; UseSharedCompilation = 'false' }
$taskEnvironment = @{}
foreach ($taskName in $taskSettings.Keys) {
    $taskEnvironment[$taskName] = [Environment]::GetEnvironmentVariable($taskName, 'Process')
    [Environment]::SetEnvironmentVariable($taskName, $taskSettings[$taskName], 'Process')
}
Push-Location $taskRepo
try {
    New-Item -ItemType Directory -Path $taskRun -Force | Out-Null
    if (!(Test-Path -LiteralPath (Join-Path $taskBaseline 'source'))) {
        New-Item -ItemType Directory -Path $taskBaseline -Force | Out-Null
        $taskArchive = Join-Path $taskBaseline 'source.zip'
        git archive --format=zip --output $taskArchive $taskRevision src/jIAnSoft.Rayleigh
        if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
        Expand-Archive -LiteralPath (Join-Path $taskBaseline 'source.zip') -DestinationPath (Join-Path $taskBaseline 'source')
        Set-Content -LiteralPath (Join-Path $taskBaseline 'revision.txt') -Value $taskRevision
    }
    if ((Get-Content -Raw -LiteralPath (Join-Path $taskBaseline 'revision.txt')).Trim() -ne $taskRevision) {
        throw 'Baseline revision does not match; use a separate workspace.'
    }

    # Only C# and the original project are archived. Both builds inherit the same
    # current SDK/package policy; the baseline has isolated intermediate/output paths.
    foreach ($taskFramework in @('net8.0', 'net10.0')) {
        dotnet build (Join-Path $taskBaseline 'source/src/jIAnSoft.Rayleigh/jIAnSoft.Rayleigh.csproj') `
            -c Release -f $taskFramework --disable-build-servers -p:UseSharedCompilation=false -nodeReuse:false `
            -p:AssemblyName=Rayleigh.Baseline -p:GeneratePackageOnBuild=false `
            "-p:ArtifactsPath=$(Join-Path $taskBaseline 'build')" -o (Join-Path $taskBaseline "lib/$taskFramework")
        if ($LASTEXITCODE -ne 0) { throw "Baseline build failed: $taskFramework" }
    }

    Set-Location (Join-Path $PSScriptRoot 'jIAnSoft.Rayleigh.Comparison')
    dotnet build jIAnSoft.Rayleigh.Comparison.csproj -c Release --disable-build-servers -p:UseSharedCompilation=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw 'Comparison build failed.' }
    foreach ($taskFramework in @('net8.0', 'net10.0')) {
        dotnet "../../artifacts/bin/jIAnSoft.Rayleigh.Comparison/release_$taskFramework/jIAnSoft.Rayleigh.Comparison.dll" --verify-bugs |
            Tee-Object -FilePath (Join-Path $taskRun "bugs-$taskFramework.txt")
        if ($LASTEXITCODE -ne 0) { throw "Bug reproduction failed: $taskFramework" }
    }
    if ($PrepareOnly) { Write-Output "Prepared; reproduction logs: $taskRun"; return }

    dotnet ../../artifacts/bin/jIAnSoft.Rayleigh.Comparison/release_net10.0/jIAnSoft.Rayleigh.Comparison.dll `
        --filter '*' --launchCount $LaunchCount --warmupCount $WarmupCount `
        --iterationCount $IterationCount --iterationTime $IterationTime --exporters json `
        --artifacts $taskRun --stopOnFirstError | Tee-Object -FilePath (Join-Path $taskRun 'console.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark process failed.' }
    $taskReports = @(Get-ChildItem -LiteralPath (Join-Path $taskRun 'results') -Filter '*-report-full-compressed.json')
    if ($taskReports.Count -ne 7) { throw "Expected 7 reports; got $($taskReports.Count)." }
    $taskCount = 0
    foreach ($taskReport in $taskReports) {
        foreach ($taskCase in (Get-Content -Raw -LiteralPath $taskReport.FullName | ConvertFrom-Json).Benchmarks) {
            if ($null -eq $taskCase.Statistics -or $taskCase.Statistics.N -lt 1) {
                throw "Missing statistics: $($taskCase.FullName)"
            }
            $taskCount++
        }
    }
    if ($taskCount -ne 192) { throw "Expected 192 cases; got $taskCount." }
    Write-Output "Validated $taskCount cases. Evidence: $taskRun"
}
finally {
    Pop-Location
    foreach ($taskName in $taskSettings.Keys) {
        [Environment]::SetEnvironmentVariable($taskName, $taskEnvironment[$taskName], 'Process')
    }
}
