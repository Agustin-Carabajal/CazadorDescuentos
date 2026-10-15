# User Stories - MVP CazadorDescuentos

**Proyecto:** CazadorDescuentos  
**Alcance inicial:** McDonald’s, Burger King y Mostaza  
**Hipótesis central del MVP:**  
> *«El sistema debe poder leer/extraer ofertas válidas de los sitios de comida y mostrárselas al usuario correctamente»*

---

## Núcleo de Validación del MVP

Las siguientes 8 historias de usuario representan el alcance funcional definitivo del MVP. Las historias 1, 2, 3, 5 y 6 conforman la **ruta crítica** indispensable para validar la hipótesis.

---

### Historia 1 (Ruta Crítica)
**Historia:** Un extractor automatizado por marca que ejecuta una rutina programada sobre el emulador para capturar las pantallas de cupones de McDonald's, Burger King y Mostaza.  
**Versión mínima aceptable:** Un script secuencial (vía Appium o comandos ADB) que se ejecuta una vez al día en un emulador Android local o servidor, abre cada una de las 3 aplicaciones de forma consecutiva, realiza un scroll básico en la pestaña de cupones y almacena las capturas en formato `.png` en una carpeta local.  
**Listado de criterios de aceptación:**
* El script ejecuta la apertura y navegación de McDonald's, Burger King y Mostaza sin requerir intervención humana.
* Genera al menos una captura de pantalla legible por cada cupón o banner presente en la vista principal de promociones.
* Si una aplicación no responde o se cuelga (timeout de 30 segundos), el script registra el error en un log y continúa automáticamente con la siguiente marca.
* Las capturas se guardan con una nomenclatura estandarizada: `{marca}_{timestamp}_{indice}.png`.

---

### Historia 2 (Ruta Crítica)
**Historia:** Un digitalizador OCR que procesa las imágenes capturadas y extrae la totalidad del texto visible (títulos, montos y términos legales).  
**Versión mínima aceptable:** Una función que procesa cada imagen almacenada mediante un motor estándar de OCR (como Tesseract o una API básica de visión) y vuelca todo el texto reconocido en un único campo de texto plano asociado al archivo.  
**Listado de criterios de aceptación:**
* Procesa el 100% de las imágenes generadas por el extractor en el lote diario.
* Extrae el texto legible tanto de títulos prominentes como de la letra chica ubicada al pie del banner.
* Descarta automáticamente imágenes corruptas o aquellas donde el texto extraído contenga menos de 10 caracteres legibles.
* Guarda el texto crudo en la base de datos vinculado al identificador de la imagen original.

---

### Historia 3 (Ruta Crítica)
**Historia:** Un analizador de promociones con IA que procesa el texto OCR mediante un único prompt estructurado para clasificar el beneficio, interpretar restricciones y redactar un resumen en dos líneas.  
**Versión mínima aceptable:** Una llamada a la API de un modelo de lenguaje (LLM) que envía el texto OCR crudo con un prompt de sistema rígido y recibe obligatoriamente un objeto JSON con: tipo de beneficio, banco/billetera, tope de reintegro, fecha límite y un resumen breve.  
**Listado de criterios de aceptación:**
* El modelo devuelve una respuesta que cumple estrictamente con el esquema JSON predefinido (`tipo_beneficio`, `medio_pago`, `tope`, `fecha_vencimiento`, `resumen`).
* El resumen redactado tiene una longitud máxima de 140 caracteres y explica la oferta en lenguaje cotidiano.
* Si el texto legal no especifica tope o medio de pago, el campo se completa con valores estándar (`"sin_tope"`, `"cualquiera"`).
* Si el modelo responde con un formato inválido o se produce un error de API, la oferta se marca como `"pendiente_revision"` y no se publica en el catálogo.

---

### Historia 4
**Historia:** Un normalizador de datos que estandariza las fechas a formato `YYYY-MM-DD`, los precios a números enteros y los nombres de bancos a identificadores únicos.  
**Versión mínima aceptable:** Un conjunto de funciones deterministas de limpieza de datos que transforman los campos del JSON antes de insertarlos en la base de datos definitiva.  
**Listado de criterios de aceptación:**
* Las fechas extraídas se transforman a formato estándar `YYYY-MM-DD` (si no incluye año, asume automáticamente el año en curso).
* Los montos monetarios y porcentajes se limpian de símbolos (`$`, `%`, `.`) y se almacenan como tipos de datos numéricos enteros.
* Los nombres de bancos y billeteras se convierten a un identificador canónico único mediante una tabla de equivalencias (ej. *"Banco Santander"*, *"Santander Río"* $\rightarrow$ `santander`).
* Si la fecha límite normalizada es anterior al día en curso, la oferta es descartada y no se almacena en la tabla de vigentes.

---

### Historia 5 (Ruta Crítica)
**Historia:** Una vista web móvil pública que renderiza el catálogo de promociones activas en tarjetas limpias sin requerir inicio de sesión ni registro.  
**Versión mínima aceptable:** Una página web estática o SPA responsiva optimizada para navegadores móviles que consulta la base de datos y lista las ofertas activas sin barreras de acceso.  
**Listado de criterios de aceptación:**
* La página es de acceso libre e inmediato, sin pantallas de login, registro ni formularios de bienvenida.
* El tiempo de carga inicial del catálogo es menor a 2.5 segundos sobre una conexión móvil 4G estándar.
* El diseño se adapta y visualiza correctamente en pantallas móviles estándar (anchos entre 360px y 430px).
* Si la base de datos no contiene ofertas activas, muestra un mensaje informativo claro: *"No hay promociones disponibles en este momento"*.

---

### Historia 6 (Ruta Crítica)
**Historia:** Un filtro contextual de catálogo que permite al usuario filtrar las promociones mostradas según el día de la semana y su medio de pago.  
**Versión mínima aceptable:** Dos selectores en la cabecera de la vista móvil: uno para el día de la semana (con el día actual preseleccionado) y otro desplegable con los medios de pago que tienen ofertas activas.  
**Listado de criterios de aceptación:**
* Al ingresar a la página, el filtro de día se posiciona automáticamente en el día actual del usuario.
* El selector de medios de pago muestra únicamente las entidades canónicas que tienen al menos una oferta vigente.
* Al seleccionar un banco o cambiar de día, el catálogo se actualiza instantáneamente en el navegador sin recargar la página completa.
* Incluye una opción clara *"Ver todos"* para remover el filtro de medios de pago.

---

### Historia 7
**Historia:** Una tarjeta de oferta estructurada que expone el resumen, el beneficio neto, la fecha de vigencia y un aviso visible indicando que el canje es exclusivo en la app oficial de la marca.  
**Versión mínima aceptable:** Un componente visual tipo tarjeta que contiene el logo de la cadena, el resumen generado por IA, una etiqueta destacada con el beneficio principal, la fecha de vencimiento y una leyenda fija de canje.  
**Listado de criterios de aceptación:**
* Presenta de manera legible el logo o nombre de la marca (McDonald's, Burger King o Mostaza) y el resumen en un máximo de 2 líneas.
* Muestra de forma destacada el beneficio principal (ej. *"30% OFF"* o *"2x1"*).
* Exhibe de forma obligatoria y visible la advertencia: *"Canje exclusivo en App Oficial o Tótem"*.
* Muestra la fecha de expiración en formato legible (ej. *"Vence: 28/10"*).

---

### Historia 8
**Historia:** Un derivador básico a la app oficial que al tocar la oferta intenta abrir la app correspondiente en el teléfono móvil o deriva a la tienda oficial mediante un enlace universal.  
**Versión mínima aceptable:** Un botón de acción principal en la tarjeta configurado con el enlace universal o esquema URI de la cadena correspondiente, con redirección alternativa a la tienda de aplicaciones.  
**Listado de criterios de aceptación:**
* Al presionar el botón en un dispositivo móvil que tiene la aplicación de la cadena instalada, el sistema intenta abrir la app oficial.
* Si el dispositivo no cuenta con la aplicación instalada, el enlace redirige a la página de la aplicación oficial en Google Play Store o Apple App Store según el sistema operativo detectado.
* La acción de redirección se ejecuta en una pestaña nueva o abre la app externa sin cerrar la página web del centralizador.
