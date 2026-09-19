#requires -Version 7
<#
.SYNOPSIS
Build the CloudApi image on this dev box and push it to DO Container Registry.
Run `doctl registry login` first. Prints the CLOUDAPI_IMAGE line to paste into .env.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Registry,                              # DO registry name
    [string] $Tag,                                                          # default: yyyyMMdd-<short sha>
    [switch] $SkipPush                                                      # build+tag only (local check)
)
$ErrorActionPreference = 'Stop'

$repoRoot  = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $Tag) {
    # Immutable, traceable to source: date + short commit sha.
    $sha = (git -C $repoRoot rev-parse --short HEAD).Trim()
    $Tag = "{0}-{1}" -f (Get-Date -AsUTC -Format 'yyyyMMdd'), $sha
}
$imageBase = "registry.digitalocean.com/$Registry/indypos-cloudapi"
$ref       = "${imageBase}:$Tag"

docker build -f "$repoRoot/src/IndyPOS.CloudApi/Dockerfile" -t $ref -t "${imageBase}:latest" $repoRoot
if ($LASTEXITCODE -ne 0) { throw "docker build failed (exit $LASTEXITCODE)" }
if (-not $SkipPush) {
    docker push $ref
    if ($LASTEXITCODE -ne 0) { throw "docker push failed (exit $LASTEXITCODE)" }
    docker push "${imageBase}:latest"
    if ($LASTEXITCODE -ne 0) { throw "docker push failed (exit $LASTEXITCODE)" }
}

Write-Host ""
Write-Host "CLOUDAPI_IMAGE=$ref"
