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

$repoRoot = (Resolve-Path "$PSScriptRoot/../..").Path

# Pushing needs registry auth and a source-traceable image; a local -SkipPush build needs neither.
if (-not $SkipPush) {
    # Prerequisite for `doctl registry login` -- fail fast with a clear message rather than letting
    # the docker push fail later with an opaque auth error.
    doctl account get | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "doctl is not authenticated. Run 'doctl auth init' and 'doctl registry login' first." }

    # docker builds the working tree, so a dirty tree would tag an image with a commit sha that does
    # not match its contents. Refuse it, so a pushed tag always identifies the exact source.
    $dirty = git -C $repoRoot status --porcelain
    if ($LASTEXITCODE -ne 0) { throw "git status failed -- cannot confirm a clean tree for a production push." }
    if ($dirty) { throw "Working tree is dirty -- refusing to push an image whose tag would not match its source. Commit or stash changes, or use -SkipPush for a local build." }
}

if (-not $Tag) {
    # Immutable and unique: UTC timestamp + short commit sha. The timestamp keeps a rebuild of the
    # same commit from silently overwriting an existing tag.
    $sha = (git -C $repoRoot rev-parse --short HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or -not $sha) { throw "Not a git repository or git unavailable -- cannot derive image tag." }
    $Tag = "{0}-{1}" -f (Get-Date -AsUTC -Format "yyyyMMdd'T'HHmmss'Z'"), $sha
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
