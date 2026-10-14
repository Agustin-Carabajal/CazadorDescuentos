# MVP: Centralizador de Ofertas de Comida Rápida
**Proyecto:** CazadorDescuentos  
**Alcance inicial:** McDonald’s, Burger King y Mostaza  
**Enfoque de validación:** Ultra-Lean (Extremadamente pequeño)  
**Criterio rector:** *«¿Podemos demostrar que la idea funciona sin esta funcionalidad?»*

---

## 1. Contexto y Problemas Abordados

El producto busca consolidar promociones dispersas de tres cadenas de comida rápida bajo un modelo automatizado, resolviendo dos problemas fundamentales:

1. **Silos cerrados (*App-First / App-Only*):** Las ofertas reales no están en la web abierta; viven dentro de aplicaciones móviles nativas y requieren mecanismos de canje específicos (QR, tótems, apps oficiales).
2. **Condicionalidad compleja y datos no estructurados:** Las promociones se publican como banners gráficos o imágenes con textos legales ilegibles (*letra chica*, topes de reintegro, bancos y días específicos).

---

## 2. Arquitectura Conceptual del Flujo

```
┌─────────────────┐       ┌───────────────────────────┐       ┌────────────────────────┐       ┌───────────────────────┐
│  APPS CERRADAS  │  ──►  │ INGESTA Y PROCESAMIENTO   │  ──►  │      WEB MÓVIL         │  ──►  │ REDIRECCIÓN Y CANJE   │
│  • McDonald's   │       │  • Emulador Headless      │       │  • Acceso anónimo      │       │  • Deep Link a la app │
│  • Burger King  │       │  • OCR de banners/textos  │       │  • Filtro: Día + Banco │       │  • Aviso de canje     │
│  • Mostaza      │       │  • IA (Interpreta/Resume) │       │  • Ficha estructurada  │       │  • Botón de reporte   │
│                 │       │  • Normalizador de datos  │       │  • Depuración vencidas │       │    de errores         │
└─────────────────┘       └───────────────────────────┘       └────────────────────────┘       └───────────────────────┘
```

---

## 3. Listado de Características Indispensables

### Módulo A: Ingesta y Extracción Automatizada (Silos Cerrados)

1. **Captura automática en apps (Emulador Móvil Headless):**
   * *Descripción:* Tarea programada que ejecuta instancias de las aplicaciones nativas en un emulador en la nube para navegar y extraer capturas de pantalla de las secciones de cupones.
   * *Justificación:* Al exigir extracción 100% automática y no existir las ofertas en la web abierta, el emulador es el mecanismo mínimo viable para superar el silo cerrado sin recurrir a ingeniería inversa compleja.

2. **Enrutador de enlaces con *Smart Deep-Linking* y Fallback:**
   * *Descripción:* Al hacer clic en la oferta, el sistema detecta el dispositivo: intenta abrir directamente la app instalada o deriva a la tienda oficial (Google Play / App Store).
   * *Justificación:* Si la redirección falla o lleva a una web rota, el usuario no puede acceder al cupón ni canjearlo.

3. **Indicador explícito de entorno de canje:**
   * *Descripción:* Etiqueta clara en la tarjeta: *"Canje exclusivo en App Oficial / Tótem"*.
   * *Justificación:* Alinea las expectativas del usuario antes de salir de la plataforma, evitando el abandono por frustración al descubrir que no se canjea en la web.

---

### Módulo B: Procesamiento, IA y Estructuración (Letra Chica)

4. **OCR para promociones en imágenes y documentos legales:**
   * *Descripción:* Motor de reconocimiento óptico de caracteres que extrae texto incrustado en banners publicitarios, piezas gráficas y documentos de términos y condiciones.
   * *Justificación:* Las promociones de comida rápida se publican casi exclusivamente como piezas gráficas; sin OCR, los datos son invisibles para cualquier pipeline automatizado.

5. **IA para clasificar, interpretar condiciones y redactar resúmenes:**
   * *Descripción:* Modelo de lenguaje (LLM) que toma el texto del OCR, comprende las restricciones legales (topes de reintegro, exclusiones) y genera un resumen directo de una o dos líneas en lenguaje cotidiano.
   * *Justificación:* Reemplaza la redacción manual humana y resuelve el dolor principal: traducir textos legales confusos a información clara y accionable.

6. **Extracción y estructuración de todos los tipos de beneficio:**
   * *Descripción:* Categorización bajo una taxonomía homogénea: *2x1*, *Porcentaje de descuento (% off)*, *Monto fijo promocional (Combo a \$X)* o *Descuento en 2da unidad*.
   * *Justificación:* Permite procesar ofertas heterogéneas de distintas marcas bajo un esquema estructurado y legible.

7. **Normalización de formatos (Fechas, Precios y Nombres):**
   * *Descripción:* Estandarización de fechas de vencimiento (`YYYY-MM-DD`), montos numéricos enteros y nombres unificados de entidades financieras (ej. "Banco Galicia", "Galicia", "Bco Galicia" $\rightarrow$ `Galicia`).
   * *Justificación:* Imprescindible para que los filtros funcionen de forma consistente y para que el sistema reconozca cuándo expira una oferta.

---

### Módulo C: Consulta, Filtros y Control de Calidad

8. **Acceso directo y anónimo (Web Móvil):**
   * *Descripción:* Experiencia web optimizada para smartphones, sin registro, sin login y sin contraseñas.
   * *Justificación:* Elimina toda fricción inicial. El usuario que busca comer rápido no tolerará un proceso de alta de cuenta.

9. **Filtro básico inmediato por Día y Medio de Pago:**
   * *Descripción:* Dos selectores esenciales: **Día de la semana** (por defecto: *"Hoy"*) y **Medio de pago** (banco o billetera virtual).
   * *Justificación:* Responde a la pregunta clave del usuario en segundos: *"¿Qué tengo disponible hoy con mis medios de pago?"*.

10. **Depuración automática de promociones vencidas:**
    * *Descripción:* Regla en base de datos que oculta de inmediato las ofertas cuya fecha límite normalizada haya sido superada.
    * *Justificación:* La presencia de ofertas vencidas destruye la credibilidad del servicio en sus primeros días.

11. **Botón de reporte de usuarios (Oferta vencida / Datos incorrectos):**
    * *Descripción:* Acción en un clic dentro de cada oferta para reportar inconsistencias.
    * *Justificación:* Actúa como red de seguridad comunitaria en tiempo real frente a posibles errores del OCR, cambios repentinos en las apps o promociones agotadas antes de su fecha límite.

---

## 4. Restricciones y Exclusiones para el MVP

Para garantizar que el MVP permanezca en su tamaño mínimo viable, se establecen los siguientes límites estrictos:
* **Sin carga manual:** Ingesta y normalización 100% automatizadas.
* **Sin canal de delivery:** Se enfoca únicamente en compra presencial / take-away / auto-mac.
* **Sin comparador cruzado:** No se implementan tablas comparativas entre marcas, productos, precios ni códigos.
* **Sin perfiles ni autenticación:** Se pospone la función de "Mi Billetera" guardada; los filtros se seleccionan en cada visita.
