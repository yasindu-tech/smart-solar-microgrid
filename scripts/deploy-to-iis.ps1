# ---------------------------------------------------------------------------
#  deploy-to-iis.ps1
#
#  Run this ON THE AZURE VM, in an elevated PowerShell prompt, after you have
#  already copied a fresh `dotnet publish` output into a staging folder there.
#  See docs/architecture/deployment-guide.md sections 6 and 12 for the full
#  loop (publish locally -> copy up -> run this).
#
#  It stops the app pool (so files aren't locked), mirrors the staging folder
#  into the live site folder, then starts the app pool again. It does NOT
#  touch appsettings.Production.json if that file already exists in the live
#  folder and isn't present in the staging folder - your secrets survive a
#  redeploy without being re-typed.
#
#  Usage (defaults match the paths used in the deployment guide):
#      ./scripts/deploy-to-iis.ps1
#      ./scripts/deploy-to-iis.ps1 -StagingPath "C:\deploy-staging" -SitePath "C:\inetpub\smartsolar-api" -AppPool "SmartSolarApi"
# ---------------------------------------------------------------------------

param(
    [string]$StagingPath = "C:\deploy-staging",
    [string]$SitePath    = "C:\inetpub\smartsolar-api",
    [string]$AppPool     = "SmartSolarApi"
)

$ErrorActionPreference = "Stop"

function Say($msg)  { Write-Host "==> $msg" -ForegroundColor Cyan }
function Warn($msg) { Write-Host "    $msg" -ForegroundColor Yellow }

# IIS's WAS service occasionally can't accept a control message right after the
# previous one - "The service cannot accept control messages at this time"
# (HRESULT 0x80070425 / Win32 1061). It's transient, most often seen seconds
# after the OTHER app pool's own Stop/Start just ran, or right after the VM has
# just been started and WAS is still coming up - not a real failure, so retry
# with a short backoff instead of failing the whole deploy over it.
function Invoke-WithRetry([scriptblock]$Action, [string]$Description, [int]$MaxAttempts = 5) {
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            & $Action
            return
        } catch {
            if ($attempt -eq $MaxAttempts) { throw }
            $delaySeconds = 3 * $attempt
            Warn "$Description failed on attempt $attempt of $MaxAttempts ($($_.Exception.Message)) - retrying in ${delaySeconds}s"
            Start-Sleep -Seconds $delaySeconds
        }
    }
}

Import-Module WebAdministration

if (-not (Test-Path $StagingPath)) {
    throw "Staging path '$StagingPath' does not exist. Copy your `dotnet publish` output there first - see deployment-guide.md section 6."
}

if (-not (Get-Item "IIS:\AppPools\$AppPool" -ErrorAction SilentlyContinue)) {
    throw "App pool '$AppPool' does not exist. Run the IIS setup in deployment-guide.md section 5 first."
}

Say "Stopping app pool '$AppPool' so files aren't locked"
Invoke-WithRetry -Description "Stop-WebAppPool" -Action { Stop-WebAppPool -Name $AppPool }
Start-Sleep -Seconds 2

Say "Preserving appsettings.Production.json if it exists and isn't in the new build"
$liveSecrets = Join-Path $SitePath "appsettings.Production.json"
$stagingSecrets = Join-Path $StagingPath "appsettings.Production.json"
$secretsBackup = $null

if ((Test-Path $liveSecrets) -and -not (Test-Path $stagingSecrets)) {
    $secretsBackup = Join-Path $env:TEMP "appsettings.Production.json.bak"
    Copy-Item $liveSecrets $secretsBackup -Force
    Warn "Backed up existing appsettings.Production.json - it will be restored after the copy"
}

Say "Mirroring '$StagingPath' into '$SitePath'"
# /MIR mirrors the tree exactly (deletes files in the destination that aren't in the source);
# that's what we want for a clean redeploy, but it's why the secrets backup above matters.
robocopy $StagingPath $SitePath /MIR /R:2 /W:2 /NFL /NDL /NJH | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy reported a real error (exit code $LASTEXITCODE). Check the paths and permissions."
}

if ($secretsBackup) {
    Copy-Item $secretsBackup $liveSecrets -Force
    Remove-Item $secretsBackup -Force
    Warn "Restored appsettings.Production.json"
}

Say "Starting app pool '$AppPool'"
Invoke-WithRetry -Description "Start-WebAppPool" -Action { Start-WebAppPool -Name $AppPool }
Start-Sleep -Seconds 2

$state = (Get-WebAppPoolState -Name $AppPool).Value
if ($state -ne "Started") {
    throw "App pool did not start (state: $state). Check Event Viewer > Windows Logs > Application for the ASP.NET Core startup error."
}

Say "Deployed. Smoke-test with: curl http://localhost/api/health"

# robocopy uses exit codes 0-7 to mean success (1 = "files were copied"), and it
# leaves $LASTEXITCODE set. A CI shell that appends `exit $LASTEXITCODE` would
# then fail a deploy that actually worked, so end on an explicit success.
exit 0
