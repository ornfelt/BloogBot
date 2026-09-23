<#
.SYNOPSIS
    Links (or copies) the movemap tiles into this repository's build output folders.

.DESCRIPTION
    Navigation.dll builds its movemap path from its own module directory - see
    Navigation/Navigation.cpp, which takes GetModuleFileNameW, strips the file name and appends
    "mmaps\". So pathfinding needs <output folder>\mmaps\, which means Bot\mmaps for Debug and
    Bot\Release\mmaps for Release.

    The tiles are large - a full set is over 2 GB - so by default this creates a directory junction
    pointing at an existing set instead of copying. A junction costs no disk space and Windows
    redirects through it transparently, so Navigation.dll cannot tell the difference.

    Bot\ is in .gitignore, so nothing this script does shows up in git.

.PARAMETER Source
    The folder holding the movemap tiles. Defaults to the .NET Framework tree's Bot\mmaps beside
    this repository, which is where they usually already are.

.PARAMETER Copy
    Copy the tiles instead of linking. Needs as much free space as the source, per configuration.
    Use this if you want the port to stand on its own rather than depend on the source folder.

.PARAMETER Remove
    Remove the links this script created. Only ever deletes the link itself, never the tiles it
    points at. Refuses to touch a real directory.

.PARAMETER Force
    Replace an existing link that points somewhere else.

.EXAMPLE
    .\Link-Mmaps.ps1
    Links Bot\mmaps and Bot\Release\mmaps to the .NET Framework tree's tiles.

.EXAMPLE
    .\Link-Mmaps.ps1 -Source D:\wow\mmaps
    Links to a set of tiles somewhere else.

.EXAMPLE
    .\Link-Mmaps.ps1 -Remove
    Removes the links, leaving the tiles alone.
#>
[CmdletBinding()]
param(
    [string] $Source = (Join-Path (Split-Path $PSScriptRoot -Parent) 'BloogBot\Bot\mmaps'),
    [switch] $Copy,
    [switch] $Remove,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

# The two output folders, matching Directory.Build.props: Debug writes Bot\, Release Bot\Release\.
$targets = @(
    (Join-Path $PSScriptRoot 'Bot\mmaps'),
    (Join-Path $PSScriptRoot 'Bot\Release\mmaps')
)

function Test-IsLink {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Get-Item -LiteralPath $Path -Force
    return [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)
}

function Remove-Link {
    param([string] $Path)
    # Directory.Delete with recursive:$false removes the reparse point only. Remove-Item and
    # rm -rf can follow a junction and take the real tiles with them.
    [System.IO.Directory]::Delete($Path, $false)
}

if ($Remove) {
    foreach ($target in $targets) {
        if (-not (Test-Path -LiteralPath $target)) {
            Write-Host "not present:  $target"
        }
        elseif (Test-IsLink $target) {
            Remove-Link $target
            Write-Host "removed link: $target"
        }
        else {
            Write-Warning "skipped, this is a real directory, not a link: $target"
        }
    }
    return
}

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Movemap source not found: $Source`nPass -Source with the folder holding your .mmap and .mmtile files."
}

$tileCount = @(Get-ChildItem -LiteralPath $Source -Filter *.mmtile -ErrorAction SilentlyContinue).Count
$mapCount = @(Get-ChildItem -LiteralPath $Source -Filter *.mmap -ErrorAction SilentlyContinue).Count
if ($tileCount -eq 0 -and $mapCount -eq 0) {
    throw "No .mmap or .mmtile files in: $Source`nThat is probably not a movemap folder."
}
Write-Host "source: $Source  ($mapCount .mmap, $tileCount .mmtile)"

foreach ($target in $targets) {
    $parent = Split-Path $target -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
        Write-Host "created:      $parent"
    }

    if (Test-Path -LiteralPath $target) {
        if (Test-IsLink $target) {
            $existing = (Get-Item -LiteralPath $target -Force).Target
            if (-not $Force) {
                Write-Host "already linked: $target -> $existing"
                continue
            }
            Remove-Link $target
            Write-Host "replaced link: $target (was -> $existing)"
        }
        else {
            Write-Warning "skipped, a real directory is already there: $target"
            continue
        }
    }

    if ($Copy) {
        Write-Host "copying to:   $target  (this takes a while)"
        Copy-Item -LiteralPath $Source -Destination $target -Recurse
        Write-Host "copied:       $target"
    }
    else {
        New-Item -ItemType Junction -Path $target -Target $Source | Out-Null
        Write-Host "linked:       $target -> $Source"
    }
}

Write-Host ''
Write-Host 'Done. Verify with:'
Write-Host '    Get-ChildItem Bot\mmaps | Measure-Object | Select-Object -ExpandProperty Count'
