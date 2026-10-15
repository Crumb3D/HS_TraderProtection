# Pre-boot check. Exit 1 if anything is wrong.
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$fail = 0

function Fail($msg) {
    Write-Host "FAIL  $msg" -ForegroundColor Red
    $script:fail++
}
function Ok($msg) { Write-Host "ok    $msg" -ForegroundColor Green }

$settings = Join-Path $Root "HSTraderProtectionSettings.json"
if (-not (Test-Path $settings)) { Fail "HSTraderProtectionSettings.json missing" }
else {
    try {
        $j = Get-Content $settings -Raw | ConvertFrom-Json
        if ($j.PrepDays -lt 1) { Fail "PrepDays must be >= 1" } else { Ok "PrepDays $($j.PrepDays)" }
        if ($j.ObjectiveHours -lt 1) { Fail "ObjectiveHours must be >= 1" } else { Ok "ObjectiveHours $($j.ObjectiveHours)" }
        if (-not $j.Bands -or $j.Bands.Count -lt 1) { Fail "Bands missing" } else { Ok "Bands $($j.Bands.Count)" }
    } catch { Fail "HSTraderProtectionSettings.json: $_" }
}

$loc = Join-Path $Root "Config\Localization.csv"
if (-not (Test-Path $loc)) { Fail "Config\Localization.csv missing" }
else {
    $locText = Get-Content $loc -Raw
    foreach ($k in @("hsTpTitle", "hsTpProtected", "hsTpUnderAttack", "hsTpSuccess", "hsTpFailure")) {
        if ($locText -notmatch [regex]::Escape($k)) { Fail "localization missing $k" } else { Ok "loc $k" }
    }
}

$windows = Join-Path $Root "Config\XUi_InGame\windows.xml"
$xui = Join-Path $Root "Config\XUi_InGame\xui.xml"
$groups = Join-Path $Root "Config\entitygroups.xml"
foreach ($f in @($windows, $xui, $groups)) {
    if (-not (Test-Path $f)) { Fail "$f missing" }
    else {
        try { [xml](Get-Content $f -Raw) | Out-Null; Ok (Split-Path $f -Leaf) }
        catch { Fail "$f : $_" }
    }
}

$winText = Get-Content $windows -Raw
if ($winText -notmatch 'HSTraderHud, HS_TraderProtection') { Fail "HUD controller must be 'HSTraderHud, HS_TraderProtection'" } else { Ok "HUD controller" }
$xuiText = Get-Content $xui -Raw
if ($xuiText -notmatch 'HUDHSTrader') { Fail "xui.xml must append HUDHSTrader to toolbelt" } else { Ok "toolbelt HUD" }
if ($xuiText -notmatch 'hsTraderResults') { Fail "xui.xml missing hsTraderResults group" } else { Ok "results group" }

$grpText = Get-Content $groups -Raw
foreach ($g in @("HS_TP_Normal", "HS_TP_Feral", "HS_TP_Radiated", "HS_TP_Charged", "HS_TP_Infernal", "HS_TP_Demolisher")) {
    if ($grpText -notmatch [regex]::Escape($g)) { Fail "entitygroups missing $g" } else { Ok "group $g" }
}
$entNames = [regex]::Matches($grpText, '<e n="([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$entityXml = @(
    "C:\7DTD-Server\ServerFiles\Data\Config\entityclasses.xml",
    "C:\7DTD-Server-3.3\ServerFiles\Data\Config\entityclasses.xml"
)
foreach ($ecPath in $entityXml) {
    if (-not (Test-Path $ecPath)) { Write-Host "skip  entityclasses $($ecPath)"; continue }
    $live = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($line in Get-Content $ecPath) {
        if ($line -match '<!--') { continue }
        if ($line -match '<entity_class name="([^"]+)"') { [void]$live.Add($Matches[1]) }
    }
    foreach ($n in $entNames) {
        if (-not $live.Contains($n)) { Fail "entity '$n' missing in $ecPath" } else { Ok "entity $n" }
    }
}

if (-not (Test-Path (Join-Path $Root "Handbook.html"))) { Fail "Handbook.html missing" } else { Ok "player handbook html" }
if (-not (Test-Path (Join-Path $Root "PLAYER_HANDBOOK.md"))) { Fail "PLAYER_HANDBOOK.md missing" } else { Ok "player handbook md" }
$hb = Get-Content (Join-Path $Root "Handbook.html") -Raw
if ($hb -notmatch 'Defend the trader' -or $hb -notmatch 'RAID IN N DAYS' -or $hb -notmatch 'wall the trader in with steel') { Fail "handbook html is missing the trader-defence guide" } else { Ok "handbook html trader-defence" }
$md = Get-Content (Join-Path $Root "PLAYER_HANDBOOK.md") -Raw
if ($md -notmatch 'Defend the trader' -or $md -notmatch 'RAID IN N DAYS' -or $md -notmatch 'wall the trader in with steel') { Fail "handbook md is missing the trader-defence guide" } else { Ok "handbook md trader-defence" }
foreach ($doc in @("CHANGELOG.md", "QUICKSTART.md", "GAME_API_CALLS.md", "README.md")) {
    if (-not (Test-Path (Join-Path $Root $doc))) { Fail "$doc missing" } else { Ok $doc }
}

$dll = Join-Path $Root "HS_TraderProtection.dll"
if (-not (Test-Path $dll)) { Fail "HS_TraderProtection.dll missing" }
else {
    $ascii = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll))
    foreach ($t in @("HSTraderProtectionMod", "HSTraderConfig", "HSTraderMission", "XUiC_HSTraderHud", "XUiC_HSTraderResults", "HSTraderSettingsData")) {
        if ($ascii.IndexOf($t) -lt 0) { Fail "DLL missing '$t'" } else { Ok "dll $t" }
    }
}

$refs = Join-Path $Root "check_refs.ps1"
if (Test-Path $refs) {
    & $refs
    if ($LASTEXITCODE -ne 0) { Fail "check_refs.ps1" } else { Ok "check_refs" }
}

if ($fail -gt 0) {
    Write-Host ("{0} check(s) failed." -f $fail) -ForegroundColor Red
    exit 1
}
Write-Host "All checks passed." -ForegroundColor Green
exit 0
