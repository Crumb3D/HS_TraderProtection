param(
    [string]$Dll = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "HS_TraderProtection.dll"),
    [string[]]$Managed = @(
        "C:\7DTD-Server\ServerFiles\7DaysToDieServer_Data\Managed",
        "C:\7DTD-Server-3.3\ServerFiles\7DaysToDieServer_Data\Managed"),
    [string]$Cecil = "C:\7DTD-Server-3.3\ServerFiles\Mods\0_TFP_Harmony\Mono.Cecil.dll"
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Cecil)) { Write-Host "skip  no Mono.Cecil at $Cecil"; exit 0 }
Add-Type -Path $Cecil

$gameAsms = @("Assembly-CSharp", "Assembly-CSharp-firstpass")
$versionGuarded = @(
    "EntityTrader::CanLockOnServer",
    "EntityTrader::OnLockRequestServer",
    "NetPackage::GetLength",
    "ItemInventoryData::holdingEntity")
$bad = 0
foreach ($dir in $Managed) {
    if (-not (Test-Path (Join-Path $dir "Assembly-CSharp.dll"))) { Write-Host "skip  $dir"; continue }
    $resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
    foreach ($d in $resolver.GetSearchDirectories()) { $resolver.RemoveSearchDirectory($d) }
    $resolver.AddSearchDirectory($dir)
    $rp = New-Object Mono.Cecil.ReaderParameters
    $rp.AssemblyResolver = $resolver
    $bytes = [IO.File]::ReadAllBytes($Dll)
    $mod = [Mono.Cecil.ModuleDefinition]::ReadModule((New-Object IO.MemoryStream(, $bytes)), $rp)
    $missing = New-Object System.Collections.Generic.SortedSet[string]
    foreach ($r in $mod.GetMemberReferences()) {
        $scope = $r.DeclaringType.Scope.Name
        if ($gameAsms -notcontains $scope) { continue }
        $ok = $false
        try { $ok = $null -ne $r.Resolve() } catch { $ok = $false }
        if (-not $ok) { [void]$missing.Add($r.FullName) }
    }
    foreach ($t in $mod.GetTypeReferences()) {
        if ($gameAsms -notcontains $t.Scope.Name) { continue }
        $ok = $false
        try { $ok = $null -ne $t.Resolve() } catch { $ok = $false }
        if (-not $ok) { [void]$missing.Add("type " + $t.FullName) }
    }
    $mod.Dispose()
    $real = @($missing | Where-Object { $m = $_; -not ($versionGuarded | Where-Object { $m.Contains($_) }) })
    if ($real.Count -eq 0) { Write-Host "ok    every game reference resolves in $dir ($($missing.Count) version-guarded)" -ForegroundColor Green }
    foreach ($m in $real) { Write-Host "MISS  [$dir] $m" -ForegroundColor Red; $bad++ }
}
if ($bad -gt 0) { exit 1 }
exit 0
