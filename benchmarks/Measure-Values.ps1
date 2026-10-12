# Run from any directory. Every case compares the frozen pre-change implementation
# with the library on the SAME runtime and input. Expect this to take several minutes.
[CmdletBinding()]
param(
    [string]$ArtifactsPath = (Join-Path $PSScriptRoot '../artifacts/values-benchmark'),
    [ValidateRange(1, 20)][int]$LaunchCount = 2,
    [ValidateRange(1, 100)][int]$WarmupCount = 6,
    [ValidateRange(3, 100)][int]$IterationCount = 12,
    [ValidateRange(100, 10000)][int]$IterationTime = 250
)

$ErrorActionPreference = 'Stop'
$taskOutput = Join-Path ([IO.Path]::GetFullPath($ArtifactsPath)) (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null

# Preserve the caller's settings. Disable persistent build/compiler processes.
$taskEnvironment = @{}
$taskSettings = @{
    DOTNET_CLI_USE_MSBUILD_SERVER = '0'
    MSBUILDDISABLENODEREUSE = '1'
    UseSharedCompilation = 'false'
}

foreach ($taskName in $taskSettings.Keys) {
    $taskEnvironment[$taskName] = [Environment]::GetEnvironmentVariable($taskName, 'Process')
    [Environment]::SetEnvironmentVariable($taskName, $taskSettings[$taskName], 'Process')
}

# The local solution bounds BenchmarkDotNet's project search, even when the repo
# contains archived copies of projects underneath artifacts/.
Push-Location (Join-Path $PSScriptRoot 'jIAnSoft.Rayleigh.Benchmarks')
try {
    dotnet build jIAnSoft.Rayleigh.Benchmarks.csproj -c Release -f net10.0 `
        --disable-build-servers -p:UseSharedCompilation=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark host build failed.' }

    dotnet ../../artifacts/bin/jIAnSoft.Rayleigh.Benchmarks/release_net10.0/jIAnSoft.Rayleigh.Benchmarks.dll `
        --filter '*ValuesIteratorBenchmarks*' --launchCount $LaunchCount `
        --warmupCount $WarmupCount --iterationCount $IterationCount --iterationTime $IterationTime `
        --exporters json --artifacts $taskOutput --stopOnFirstError
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark process failed.' }

    # BenchmarkDotNet may return exit code 0 even when generated-project builds fail.
    # Reject missing / failed measurements instead of treating exit code alone as proof.
    $taskReports = @(Get-ChildItem -LiteralPath (Join-Path $taskOutput 'results') -Filter '*-report-full-compressed.json')
    if ($taskReports.Count -ne 3) { throw 'Expected reports for int, Guid and string.' }
    $taskCaseCount = 0
    foreach ($taskReport in $taskReports) {
        $taskData = Get-Content -Raw -LiteralPath $taskReport.FullName | ConvertFrom-Json
        foreach ($taskCase in $taskData.Benchmarks) {
            if ($null -eq $taskCase.Statistics -or $taskCase.Statistics.N -lt 1) {
                throw "Missing measurement: $($taskCase.FullName)"
            }
            $taskCaseCount++
        }
    }
    if ($taskCaseCount -ne 108) { throw "Expected 108 cases; found $taskCaseCount. Check benchmark parameters and reports." }
    Write-Output "Validated $taskCaseCount measurements. Reports: $taskOutput"
}
finally {
    Pop-Location
    foreach ($taskName in $taskSettings.Keys) {
        [Environment]::SetEnvironmentVariable($taskName, $taskEnvironment[$taskName], 'Process')
    }
}
