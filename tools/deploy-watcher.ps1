param([switch]$Once)
$ErrorActionPreference = "Stop"
$mutexName = "Local\PerkFixesAndRebalance.DeployWatcher"
$created = $false
$mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$created)
if (-not $mutex.WaitOne(0)) {
    Write-Output "FAILED another watcher holds mutex"
    exit 1
}
try {
    $root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    if (-not (Test-Path (Join-Path $root "PerkFixesAndRebalance.csproj"))) {
        $root = "D:\Games\Bannerlord\Mod-dev\PerkFixesAndRebalance"
    }
    $srcDll = Join-Path $root "bin\Release\PerkFixesAndRebalance.dll"
    $srcPdb = Join-Path $root "bin\Release\PerkFixesAndRebalance.pdb"
    $srcXml = Join-Path $root "SubModule.xml"
    $srcModuleData = Join-Path $root "ModuleData"
    $mod = "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\PerkFixesAndRebalance"
    $destDir = Join-Path $mod "bin\Win64_Shipping_Client"
    $destDll = Join-Path $destDir "PerkFixesAndRebalance.dll"
    $destModuleData = Join-Path $mod "ModuleData"
    Write-Output "PENDING waiting for DLL unlock"
    while ($true) {
        $locked = $false
        if (Test-Path $destDll) {
            try {
                $fs = [IO.File]::Open($destDll, "Open", "ReadWrite", "None")
                $fs.Close()
            } catch {
                $locked = $true
            }
        }
        if (-not $locked) {
            try {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                Copy-Item $srcDll $destDir -Force
                if (Test-Path $srcPdb) { Copy-Item $srcPdb $destDir -Force }
                Copy-Item $srcXml $mod -Force
                if (Test-Path $srcModuleData) {
                    if (Test-Path $destModuleData) {
                        Remove-Item $destModuleData -Recurse -Force
                    }
                    Copy-Item $srcModuleData $destModuleData -Recurse -Force
                }
                $srcHash = (Get-FileHash $srcDll -Algorithm SHA256).Hash
                $destHash = (Get-FileHash $destDll -Algorithm SHA256).Hash
                if ($srcHash -ne $destHash) {
                    Write-Output "MISMATCH"
                    exit 1
                }
                Write-Output "DEPLOYED $($destHash.Substring(0,12))"
                break
            } catch {
                if (-not $Once) { }
            }
        }
        Start-Sleep -Seconds 2
    }
} finally {
    $mutex.ReleaseMutex()
    $mutex.Dispose()
}
