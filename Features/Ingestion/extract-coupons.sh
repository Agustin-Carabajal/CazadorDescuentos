#!/usr/bin/env bash
# Extractor automatizado de cupones por marca (Historia 1 - CazadorDescuentos)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Cargar .env si existe
if [ -f "$ROOT_DIR/.env" ]; then
    set -a
    source "$ROOT_DIR/.env"
    set +a
fi

ADB_PATH="${ADB_PATH:-adb}"
OUTPUT_DIR="${OUTPUT_DIRECTORY:-capturas}"
TIMEOUT_SEC="${TIMEOUT_SECONDS:-30}"
SCROLL_COUNT="${SCROLL_COUNT:-3}"
LOG_FILE="${LOG_FILE:-logs/ingestion.log}"
SIMULATE="${SIMULATE_MODE:-false}"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --simulate|-s) SIMULATE="true" ;;
        --adb-path) ADB_PATH="$2"; shift ;;
        --output-dir) OUTPUT_DIR="$2"; shift ;;
        --timeout) TIMEOUT_SEC="$2"; shift ;;
        --scroll-count) SCROLL_COUNT="$2"; shift ;;
        --log-file) LOG_FILE="$2"; shift ;;
        *) echo "Parámetro desconocido: $1"; exit 1 ;;
    esac
    shift
done

mkdir -p "$OUTPUT_DIR"
mkdir -p "$(dirname "$LOG_FILE")"

log_msg() {
    local level="$1"
    local msg="$2"
    local ts
    ts="$(date '+%Y-%m-%d %H:%M:%S')"
    local line="[$ts] [$level] $msg"
    echo "$line"
    echo "$line" >> "$LOG_FILE"
}

log_msg "INFO" "Iniciando ciclo de extracción programada (Historia 1) para marcas: mcdonalds, burgerking, mostaza."

# Verificar ADB si no está en modo simulación
if [ "$SIMULATE" != "true" ]; then
    if ! command -v "$ADB_PATH" &> /dev/null || ! "$ADB_PATH" devices | grep -q "device$"; then
        log_msg "WARN" "ADB o dispositivo no detectado. Activando modo simulación automático."
        SIMULATE="true"
    fi
fi

TIMESTAMP="$(date '+%Y%m%d_%H%M%S')"
BRANDS=("mcdonalds" "burgerking" "mostaza")

# Definición de paquetes
declare -A PACKAGES=(
    ["mcdonalds"]="${MCDONALDS_PACKAGE:-com.mcdonalds.app}"
    ["burgerking"]="${BURGERKING_PACKAGE:-ar.com.burgerking}"
    ["mostaza"]="${MOSTAZA_PACKAGE:-com.mostaza.mostaza}"
)

declare -A NAMES=(
    ["mcdonalds"]="McDonald's"
    ["burgerking"]="Burger King"
    ["mostaza"]="Mostaza"
)

# Minimal 1x1 valid PNG in base64
MINIMAL_PNG_B64="iVBORw0KGgoAAAANSUhEUgAAAGQAAABkBAMAAACC3t6pAAAADAMAUAAAAAAAAP/iIbwzAAAADElEQVR4nGPcwQAAAgEBAeIhPLwAAAAASUVORK5CYII="

create_simulated_png() {
    local target="$1"
    echo "$MINIMAL_PNG_B64" | base64 -d > "$target" 2>/dev/null || true
}

successes=0
failures=0
total_captures=0

for brand in "${BRANDS[@]}"; do
    name="${NAMES[$brand]}"
    pkg="${PACKAGES[$brand]}"
    log_msg "INFO" "[$name] Iniciando extracción autónoma. Timeout: ${TIMEOUT_SEC}s."

    brand_start=$(date +%s)
    brand_failed=0

    if [ "$SIMULATE" = "true" ]; then
        for ((i=1; i<=SCROLL_COUNT; i++)); do
            filename="${brand}_${TIMESTAMP}_${i}.png"
            dest="$OUTPUT_DIR/$filename"
            create_simulated_png "$dest"
            total_captures=$((total_captures + 1))
            log_msg "INFO" "[$name] Captura guardada: $filename ($i/$SCROLL_COUNT)"
            sleep 0.2
        done
        successes=$((successes + 1))
        log_msg "INFO" "[$name] Extracción completada exitosamente."
    else
        # Ejecución ADB con timeout
        if timeout "$TIMEOUT_SEC" bash -c "
            '$ADB_PATH' shell monkey -p '$pkg' -c android.intent.category.LAUNCHER 1 > /dev/null 2>&1
            sleep 3
            for ((idx=1; idx<=$SCROLL_COUNT; idx++)); do
                file='${brand}_${TIMESTAMP}_\${idx}.png'
                '$ADB_PATH' exec-out screencap -p > '$OUTPUT_DIR/\$file'
                if [ \$idx -lt $SCROLL_COUNT ]; then
                    '$ADB_PATH' shell input swipe 500 1500 500 500 400
                    sleep 1
                fi
            done
            '$ADB_PATH' shell am force-stop '$pkg' > /dev/null 2>&1
        "; then
            count=$(ls -1 "$OUTPUT_DIR/${brand}_${TIMESTAMP}_"*.png 2>/dev/null | wc -l)
            total_captures=$((total_captures + count))
            successes=$((successes + 1))
            log_msg "INFO" "[$name] Proceso completado. Capturas generadas: $count"
        else
            failures=$((failures + 1))
            log_msg "ERROR" "[TIMEOUT] [$name] La aplicación no respondió dentro del límite de ${TIMEOUT_SEC} segundos. Continuando con la siguiente marca (Aislamiento de lote)."
            "$ADB_PATH" shell am force-stop "$pkg" > /dev/null 2>&1 || true
        fi
    fi
done

log_msg "INFO" "Ciclo de extracción finalizado. Marcas exitosas: $successes, Errores: $failures, Total de capturas generadas: $total_captures."
