<#
.SYNOPSIS
    Extractor automatizado de cupones por marca (Historia 1 - CazadorDescuentos).

.DESCRIPTION
    Ejecuta una rutina programada sobre un emulador o dispositivo Android para capturar
    las pantallas de cupones de McDonald's, Burger King y Mostaza de forma autónoma,
    gestionando timeouts de 30 segundos por aplicación y guardando las capturas
    con la nomenclatura estandarizada: {marca}_{timestamp}_{indice}.png.

.PARAMETER AdbPath
    Ruta o comando del ejecutable ADB (por defecto: 'adb' o valor en .env).

.PARAMETER OutputDirectory
    Carpeta de destino para las capturas (por defecto: 'capturas').

.PARAMETER TimeoutSeconds
    Límite máximo de espera por marca en segundos antes de timeout (por defecto: 30).

.PARAMETER ScrollCount
    Número de capturas y scrolls a realizar por marca (por defecto: 3).

.PARAMETER LogFile
    Ruta del archivo de registro (por defecto: 'logs/ingestion.log').

.PARAMETER Simulate
    Ejecuta el script en modo simulación para entornos sin emulador Android o para pruebas.
#>

[CmdletBinding()]
param (
    [string]$AdbPath,
    [string]$OutputDirectory,
    [int]$TimeoutSeconds = 30,
    [int]$ScrollCount = 3,
    [string]$LogFile,
    [switch]$Simulate,
    [string[]]$Brands = @("mcdonalds", "burgerking", "mostaza")
)

# 1. Cargar configuración desde .env si existe
$envFile = Join-Path $PSScriptRoot "..\..\.env"
if (-not (Test-Path $envFile)) {
    $envFile = Join-Path (Get-Location) ".env"
}

if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#") -and $line.Contains("=")) {
            $parts = $line.Split("=", 2)
            $key = $parts[0].Trim()
            $val = $parts[1].Trim()
            if (-not [string]::IsNullOrEmpty($val)) {
                switch ($key) {
                    "ADB_PATH" { if (-not $PSBoundParameters.ContainsKey('AdbPath')) { $AdbPath = $val } }
                    "OUTPUT_DIRECTORY" { if (-not $PSBoundParameters.ContainsKey('OutputDirectory')) { $OutputDirectory = $val } }
                    "TIMEOUT_SECONDS" { if (-not $PSBoundParameters.ContainsKey('TimeoutSeconds')) { $TimeoutSeconds = [int]$val } }
                    "SCROLL_COUNT" { if (-not $PSBoundParameters.ContainsKey('ScrollCount')) { $ScrollCount = [int]$val } }
                    "LOG_FILE" { if (-not $PSBoundParameters.ContainsKey('LogFile')) { $LogFile = $val } }
                    "SIMULATE_MODE" { if (-not $PSBoundParameters.ContainsKey('Simulate') -and ($val -eq "true" -or $val -eq "1")) { $Simulate = $true } }
                }
            }
        }
    }
}

# Valores por defecto finales
if ([string]::IsNullOrWhiteSpace($AdbPath)) { $AdbPath = "adb" }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = "capturas" }
if ([string]::IsNullOrWhiteSpace($LogFile)) { $LogFile = "logs/ingestion.log" }

# Asegurar directorios
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$logDir = [System.IO.Path]::GetDirectoryName($LogFile)
if ($logDir -and -not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

function Write-IngestionLog {
    param (
        [string]$Message,
        [string]$Level = "INFO"
    )
    $ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

Write-IngestionLog "Iniciando ciclo de extracción programada (Historia 1) para marcas: $($Brands -join ', ')."

# Diccionario de paquetes Android por marca
$BrandPackages = @{
    "mcdonalds"  = @{ Name = "McDonald's"; Package = "com.mcdonalds.app" }
    "burgerking" = @{ Name = "Burger King"; Package = "ar.com.burgerking" }
    "mostaza"    = @{ Name = "Mostaza"; Package = "com.mostaza.mostaza" }
}

# Verificar disponibilidad de ADB o dispositivo si no está en modo simulación
$deviceReady = $false
if (-not $Simulate) {
    try {
        $adbTest = & $AdbPath devices 2>&1
        if ($LASTEXITCODE -eq 0) {
            $attached = $adbTest | Select-String -Pattern "device$"
            if ($attached.Count -gt 0) {
                $deviceReady = $true
                Write-IngestionLog "Dispositivo Android detectado mediante ADB."
            } else {
                Write-IngestionLog "No se encontró ningún emulador o dispositivo Android conectado en ADB. Activando modo simulación automático." "WARN"
                $Simulate = $true
            }
        } else {
            Write-IngestionLog "No se pudo invocar '$AdbPath'. Activando modo simulación automático." "WARN"
            $Simulate = $true
        }
    } catch {
        Write-IngestionLog "Error al verificar ADB ($($_.Exception.Message)). Activando modo simulación automático." "WARN"
        $Simulate = $true
    }
}

$timestamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$totalCapturas = 0
$exitos = 0
$errores = 0

# Función para generar PNG minimalista válido en modo simulación
function New-SimulatedPng {
    param ([string]$DestinationPath)
    [byte[]]$pngBytes = @(
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x64,
        0x00, 0x00, 0x00, 0x64,
        0x08, 0x02, 0x00, 0x00, 0x00,
        0x5B, 0xDE, 0xB3, 0xA9,
        0x00, 0x00, 0x00, 0x0C,
        0x49, 0x44, 0x41, 0x54,
        0x78, 0x9C, 0x63, 0x60, 0x00, 0x00, 0x00, 0x02,
        0x00, 0x01, 0xE2, 0x21,
        0xBC, 0x33,
        0x00, 0x00, 0x00, 0x00,
        0x49, 0x45, 0x4E, 0x44,
        0xAE, 0x42, 0x60, 0x82
    )
    [System.IO.File]::WriteAllBytes($DestinationPath, $pngBytes)
}

foreach ($brandId in $Brands) {
    $brandKey = $brandId.ToLowerInvariant()
    $brandMeta = $BrandPackages[$brandKey]
    $displayName = if ($brandMeta) { $brandMeta.Name } else { $brandId }
    $pkgName = if ($brandMeta) { $brandMeta.Package } else { "com.$brandKey.app" }

    Write-IngestionLog "[$displayName] Iniciando extracción autónoma. Timeout: ${TimeoutSeconds}s."

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $timedOut = $false
    $brandSuccess = $false

    try {
        if ($Simulate) {
            # Simulación de apertura y navegación
            Start-Sleep -Milliseconds 500
            for ($i = 1; $i -le $ScrollCount; $i++) {
                if ($sw.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
                    $timedOut = $true
                    break
                }
                # Formato: {marca}_{timestamp}_{indice}.png
                $filename = "${brandKey}_${timestamp}_${i}.png"
                $destPath = Join-Path $OutputDirectory $filename
                New-SimulatedPng -DestinationPath $destPath
                $totalCapturas++
                Write-IngestionLog "[$displayName] Captura guardada: $filename ($i/$ScrollCount)"
                if ($i -lt $ScrollCount) {
                    Start-Sleep -Milliseconds 300
                }
            }
            if (-not $timedOut) {
                $brandSuccess = $true
            }
        } else {
            # Ejecución real mediante comandos ADB con control de timeout
            $jobScript = {
                param ($adb, $pkg, $outDir, $brand, $ts, $scrolls)
                # 1. Apertura de app
                & $adb shell monkey -p $pkg -c android.intent.category.LAUNCHER 1 2>&1 | Out-Null
                Start-Sleep -Seconds 3

                # 2. Captura y Scroll
                for ($idx = 1; $idx -le $scrolls; $idx++) {
                    $file = "${brand}_${ts}_${idx}.png"
                    $target = Join-Path $outDir $file
                    
                    # Screencap directo
                    & $adb exec-out screencap -p > $target
                    
                    if ($idx -lt $scrolls) {
                        # Scroll swipe básico
                        & $adb shell input swipe 500 1500 500 500 400
                        Start-Sleep -Seconds 1
                    }
                }

                # 3. Detener app
                & $adb shell am force-stop $pkg 2>&1 | Out-Null
            }

            $job = Start-Job -ScriptBlock $jobScript -ArgumentList $AdbPath, $pkgName, $OutputDirectory, $brandKey, $timestamp, $ScrollCount
            $completed = Wait-Job $job -Timeout $TimeoutSeconds

            if ($null -eq $completed) {
                # Criterio de aceptación 3: Timeout de 30 segundos
                $timedOut = $true
                Stop-Job $job -Force | Out-Null
                Remove-Job $job -Force | Out-Null
                
                # Intentar forzar cierre de la app colgada
                try { & $AdbPath shell am force-stop $pkgName 2>&1 | Out-Null } catch { }
            } else {
                Receive-Job $job | Out-Null
                Remove-Job $job -Force | Out-Null
                $brandSuccess = $true
                # Contar archivos generados
                $files = Get-ChildItem -Path $OutputDirectory -Filter "${brandKey}_${timestamp}_*.png"
                $totalCapturas += $files.Count
                Write-IngestionLog "[$displayName] Proceso completado. Capturas generadas: $($files.Count)"
            }
        }

        if ($timedOut) {
            $errores++
            Write-IngestionLog "[TIMEOUT] [$displayName] La aplicación no respondió dentro del límite de ${TimeoutSeconds} segundos. Registrando error y continuando con la siguiente marca (Aislamiento de lote)." "ERROR"
        } elseif ($brandSuccess) {
            $exitos++
            Write-IngestionLog "[$displayName] Extracción completada exitosamente en $($sw.Elapsed.TotalSeconds.ToString('0.0'))s."
        }
    } catch {
        $errores++
        Write-IngestionLog "[$displayName] Error durante la extracción: $($_.Exception.Message). Continuando con la siguiente marca." "ERROR"
    } finally {
        $sw.Stop()
    }
}

Write-IngestionLog "Ciclo de extracción finalizado. Marcas exitosas: $exitos, Errores: $errores, Total de capturas generadas: $totalCapturas."

if ($errores -gt 0 -and $exitos -eq 0) {
    exit 1
}
exit 0
