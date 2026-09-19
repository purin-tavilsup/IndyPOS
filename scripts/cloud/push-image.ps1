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
if (-not $SkipPush) {
    docker push $ref
    docker push "${imageBase}:latest"
}

Write-Host ""
Write-Host "CLOUDAPI_IMAGE=$ref"
