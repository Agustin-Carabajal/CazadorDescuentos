<#
.SYNOPSIS
    Pruebas automatizadas para verificar los criterios de aceptacion de Historia 1.
#>

$testOutput = Join-Path $PSScriptRoot "..\..\capturas_test"
$testLog = Join-Path $PSScriptRoot "..\..\logs\test_ingestion.log"

if (Test-Path $testOutput) { Remove-Item $testOutput -Recurse -Force }
if (Test-Path $testLog) { Remove-Item $testLog -Force }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  EJECUTANDO PRUEBAS DE ACEPTACION - HISTORIA 1" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$scriptPath = Join-Path $PSScriptRoot "extract-coupons.ps1"

# Prueba 1: Ejecucion completa en simulacion (McDonald's, Burger King, Mostaza)
Write-Host "`n[Prueba 1] Verificando apertura, navegacion y capturas secuenciales..." -ForegroundColor Yellow
& $scriptPath -OutputDirectory $testOutput -LogFile $testLog -Simulate -ScrollCount 2
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host "FALLO: El script termino con codigo de error $exitCode" -ForegroundColor Red
    exit 1
}

# Verificacion de Capturas por Marca
$brands = @("mcdonalds", "burgerking", "mostaza")
$allFiles = Get-ChildItem -Path $testOutput -Filter "*.png"

Write-Host "Archivos generados: $($allFiles.Count)" -ForegroundColor Green

foreach ($b in $brands) {
    $brandFiles = $allFiles | Where-Object { $_.Name -like "${b}_*.png" }
    if ($brandFiles.Count -ge 2) {
        Write-Host " [PASS] Marca '$b': $($brandFiles.Count) capturas generadas correctamente." -ForegroundColor Green
    } else {
        Write-Host " [FAIL] Marca '$b': No se encontraron suficientes capturas." -ForegroundColor Red
        exit 1
    }
}

# Verificacion de Nomenclatura {marca}_{timestamp}_{indice}.png
Write-Host "`n[Prueba 2] Verificando nomenclatura estandarizada: {marca}_{timestamp}_{indice}.png..." -ForegroundColor Yellow
$regex = "^(mcdonalds|burgerking|mostaza)_[0-9]{8}_[0-9]{6}_[0-9]+\.png$"
$invalid = $allFiles | Where-Object { $_.Name -notmatch $regex }

if ($invalid.Count -eq 0) {
    Write-Host " [PASS] 100% de los archivos cumplen con el patron {marca}_{timestamp}_{indice}.png" -ForegroundColor Green
} else {
    Write-Host " [FAIL] Archivos con formato invalido encontrados: $($invalid.Name -join ', ')" -ForegroundColor Red
    exit 1
}

# Verificacion de Log generado
Write-Host "`n[Prueba 3] Verificando registro en log..." -ForegroundColor Yellow
if (Test-Path $testLog) {
    $logContent = Get-Content $testLog
    if ($logContent.Count -gt 5) {
        Write-Host " [PASS] Archivo de log generado con $($logContent.Count) lineas." -ForegroundColor Green
    } else {
        Write-Host " [FAIL] El archivo de log esta vacio o incompleto." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host " [FAIL] Archivo de log no encontrado en $testLog" -ForegroundColor Red
    exit 1
}

# Prueba 4: Verificacion de Timeout y Aislamiento de Lote (Batch Isolation)
Write-Host "`n[Prueba 4] Verificando manejo de Timeout y Aislamiento de Lote (Timeout 1s)..." -ForegroundColor Yellow
$timeoutLog = Join-Path $PSScriptRoot "..\..\logs\timeout_test.log"
& $scriptPath -OutputDirectory $testOutput -LogFile $timeoutLog -Simulate -TimeoutSeconds 0

$timeoutLogContent = Get-Content $timeoutLog -Raw
if ($timeoutLogContent -match "TIMEOUT" -and $timeoutLogContent -match "Aislamiento de lote") {
    Write-Host " [PASS] El script detecto el timeout, registro el error y continuo con la siguiente marca (Batch Isolation comprobado)." -ForegroundColor Green
} else {
    Write-Host " [FAIL] No se encontro el registro esperado de timeout en el log." -ForegroundColor Red
    exit 1
}

# Limpieza de carpetas de prueba temporales
if (Test-Path $testOutput) { Remove-Item $testOutput -Recurse -Force }
if (Test-Path $testLog) { Remove-Item $testLog -Force }
if (Test-Path $timeoutLog) { Remove-Item $timeoutLog -Force }

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  TODAS LAS PRUEBAS DE HISTORIA 1 PASARON CON EXITO!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
