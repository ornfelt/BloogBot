# bloogbot_diff.ps1
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

# Modes:
#   "DownloadedVsPreFork"
#       Compare:
#           pre-fork commit from RepoA
#               ->
#           latest COMMIT in RepoB
#
#   "CurrentVsPreFork"
#       Compare:
#           pre-fork commit from RepoA
#               ->
#           current working tree in RepoA
#
#$Mode = "DownloadedVsPreFork"
$Mode = "CurrentVsPreFork"

# true:
#   Find the FIRST commit whose author name/email contains "ornfelt" or "jonas"
#   (case-insensitive), then use its parent as the pre-fork commit.
#
# false:
#   Compare the history against DrewKestell/BloogBot and find the first commit
#   in our history that does not exist upstream. Its parent becomes the
#   pre-fork commit.
$UseAuthorHeuristic = $true

$RepoA = Join-Path $env:code_root_dir "Code2/C#/BloogBot"
$RepoB = Join-Path $env:USERPROFILE "Downloads/BloogBot"

$OriginalRepoUrl = "https://github.com/DrewKestell/BloogBot.git"
$OriginalBranch  = "main"

$AuthorPattern = "(?i)(ornfelt|jonas)"

# Generated in the current directory.
$OutputDir = (Get-Location).Path


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repo,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$GitArgs
    )

    $output = & git -C $Repo @GitArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed in '$Repo': git $($GitArgs -join ' ')"
    }

    return $output
}


function Test-GitRepo {
    param([string]$Repo)

    if (-not (Test-Path $Repo)) {
        throw "Repository path does not exist: $Repo"
    }

    & git -C $Repo rev-parse --git-dir *> $null

    if ($LASTEXITCODE -ne 0) {
        throw "Not a Git repository: $Repo"
    }
}


function Get-PreForkCommitByAuthor {
    param([string]$Repo)

    Write-Host "Finding first commit by author matching '$AuthorPattern'..."

    $commits = Invoke-Git $Repo log `
        --reverse `
        "--format=%H%x09%an%x09%ae" `
        HEAD

    $firstMatchingCommit = $null
    $matchingAuthor = $null

    foreach ($line in $commits) {
        $parts = $line -split "`t", 3

        if ($parts.Count -lt 3) {
            continue
        }

        $hash   = $parts[0]
        $name   = $parts[1]
        $email  = $parts[2]
        $author = "$name <$email>"

        if ($author -match $AuthorPattern) {
            $firstMatchingCommit = $hash
            $matchingAuthor = $author
            break
        }
    }

    if (-not $firstMatchingCommit) {
        throw "Could not find a commit whose author matches '$AuthorPattern'."
    }

    Write-Host "First matching commit:"
    Write-Host "  $firstMatchingCommit"
    Write-Host "  $matchingAuthor"

    $parent = Invoke-Git $Repo rev-parse "$firstMatchingCommit^1"

    Write-Host "Using its parent as pre-fork commit:"
    Write-Host "  $parent"

    return $parent.Trim()
}


function Get-PreForkCommitFromUpstream {
    param(
        [string]$Repo,
        [string]$TemporaryUpstreamRef
    )

    Write-Host "Fetching original upstream history..."

    Invoke-Git $Repo fetch `
        --quiet `
        $OriginalRepoUrl `
        "+refs/heads/${OriginalBranch}:${TemporaryUpstreamRef}"

    # Find commits reachable from our HEAD which are NOT in the original
    # repository. The oldest one should be the first fork-specific commit.
    $customCommits = Invoke-Git $Repo rev-list `
        --reverse `
        --topo-order `
        HEAD `
        --not $TemporaryUpstreamRef

    $firstCustomCommit = $customCommits | Select-Object -First 1

    if (-not $firstCustomCommit) {
        throw "No fork-specific commits were found compared with upstream."
    }

    Write-Host "First commit not present upstream:"
    Write-Host "  $firstCustomCommit"

    $parent = Invoke-Git $Repo rev-parse "$firstCustomCommit^1"

    Write-Host "Using its parent as pre-fork commit:"
    Write-Host "  $parent"

    return $parent.Trim()
}


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

Test-GitRepo $RepoA

if ($Mode -eq "DownloadedVsPreFork") {
    Test-GitRepo $RepoB
}

$TempUpstreamRef = "refs/bloogbot-diff/$PID/upstream"
$TempRepoBRef    = "refs/bloogbot-diff/$PID/downloaded"

try {
    # -----------------------------------------------------------------------
    # Find baseline
    # -----------------------------------------------------------------------

    if ($UseAuthorHeuristic) {
        $PreForkCommit = Get-PreForkCommitByAuthor $RepoA
    }
    else {
        $PreForkCommit = Get-PreForkCommitFromUpstream `
            $RepoA `
            $TempUpstreamRef
    }

    Write-Host ""
    Write-Host "Pre-fork baseline:"
    Invoke-Git $RepoA show `
        --no-patch `
        "--format=%H%nAuthor: %an <%ae>%nDate:   %ad%nSubject: %s" `
        $PreForkCommit

    Write-Host ""

    # -----------------------------------------------------------------------
    # Generate diff
    # -----------------------------------------------------------------------

    switch ($Mode) {
        "DownloadedVsPreFork" {
            Write-Host "Getting latest commit from:"
            Write-Host "  $RepoB"

            $RepoBHead = (Invoke-Git $RepoB rev-parse HEAD).Trim()

            Write-Host "RepoB HEAD:"
            Write-Host "  $RepoBHead"

            # Import RepoB's HEAD into RepoA temporarily so Git can compare
            # the two commit trees directly.
            Invoke-Git $RepoA fetch `
                --quiet `
                $RepoB `
                "+HEAD:${TempRepoBRef}"

            $OutputFile = Join-Path `
                $OutputDir `
                "bloogbot_downloaded_vs_prefork.diff"

            Invoke-Git $RepoA diff `
                --binary `
                --find-renames `
                "--output=$OutputFile" `
                $PreForkCommit `
                $TempRepoBRef

            Write-Host ""
            Write-Host "Generated:"
            Write-Host "  $OutputFile"
        }

        "CurrentVsPreFork" {
            $OutputFile = Join-Path `
                $OutputDir `
                "bloogbot_current_vs_prefork.diff"

            # <commit> vs working tree.
            #
            # This includes staged + unstaged changes to TRACKED files.
            # Untracked files are not included by normal `git diff`.
            Invoke-Git $RepoA diff `
                --binary `
                --find-renames `
                "--output=$OutputFile" `
                $PreForkCommit `
                -- .

            Write-Host ""
            Write-Host "Generated:"
            Write-Host "  $OutputFile"
        }

        default {
            throw "Unknown mode: $Mode"
        }
    }
}
finally {
    # Remove temporary refs if they were created.
    & git -C $RepoA update-ref -d $TempUpstreamRef 2>$null
    & git -C $RepoA update-ref -d $TempRepoBRef 2>$null
}
