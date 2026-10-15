# CazadorDescuentos

Plataforma ultra-lean centralizadora de promociones y cupones de comida rápida (McDonald's, Burger King y Mostaza).

## Módulo de Ingesta (Historia 1: Extractor automatizado por marca)

Extractor autónomo que ejecuta rutinas secuenciales mediante comandos ADB sobre emulador o dispositivo Android para capturar las pantallas de cupones de McDonald's, Burger King y Mostaza.

### Características implementadas
- **Apertura y navegación autónoma:** Ejecuta de forma secuencial la apertura de McDonald's, Burger King y Mostaza sin requerir intervención humana.
- **Captura y scroll:** Captura las promociones visibles y realiza scroll vertical para almacenar cada banner en formato `.png`.
- **Aislamiento de lote (Batch Isolation) y Timeout de 30s:** Si una aplicación se cuelga o no responde en 30 segundos, el script registra el error en `logs/ingestion.log` y continúa automáticamente con la siguiente marca.
- **Nomenclatura estandarizada:** Almacena los archivos siguiendo el formato `{marca}_{timestamp}_{indice}.png` (con identificadores canónicos en minúsculas: `mcdonalds`, `burgerking`, `mostaza`).
- **Modo simulación:** Permite verificar y probar el pipeline completo sin necesidad de un emulador físico conectado.

### Requisitos previos
- .NET 8 SDK (o superior) / PowerShell 5.1+ / Bash
- Android SDK Platform-Tools (`adb`) con emulador o dispositivo configurado (opcional si se utiliza `--simulate`).

### Configuración
Copiar el archivo de plantilla `.env.example` a `.env`:
```bash
cp .env.example .env
```

Variables disponibles en `.env`:
- `ADB_PATH`: Ruta al binario `adb` (por defecto: `adb`).
- `ANDROID_SERIAL`: Identificador del dispositivo o emulador Android (opcional).
- `OUTPUT_DIRECTORY`: Carpeta destino de las capturas (por defecto: `capturas`).
- `TIMEOUT_SECONDS`: Tiempo límite por aplicación en segundos (por defecto: `30`).
- `SCROLL_COUNT`: Cantidad de scrolls y capturas por marca (por defecto: `3`).
- `LOG_FILE`: Ruta del archivo de logs (por defecto: `logs/ingestion.log`).
- `SIMULATE_MODE`: Activa el modo simulación si es `true`.

### Ejecución

#### Opción 1: Script de PowerShell (Windows)
```powershell
# Ejecución en emulador Android
.\Features\Ingestion\extract-coupons.ps1

# Ejecución en modo simulación (pruebas locales o CI)
.\Features\Ingestion\extract-coupons.ps1 -Simulate
```

#### Opción 2: Script Bash (Linux / macOS / Docker)
```bash
# Dar permisos de ejecución
chmod +x Features/Ingestion/extract-coupons.sh

# Ejecutar
./Features/Ingestion/extract-coupons.sh --simulate
```

### Pruebas automatizadas de criterios de aceptación
Para validar automáticamente el 100% de los criterios de aceptación de la Historia 1:
```powershell
.\Features\Ingestion\test-extraction.ps1
```
