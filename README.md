# Documento de Especificación del MVP

## Visión General del Producto
Un sistema centralizador y comparador de ofertas y descuentos automatizado para cadenas de comida rápida (**McDonald’s, Burger King y Mostaza**). El objetivo exclusivo del MVP es permitir al usuario comparar de forma rápida y confiable cuál es la mejor oferta disponible hoy para consumo en local o retiro en tienda (*take-away*).

---

## Flujo General del Sistema

```
[Webs: McDonald's / BK / Mostaza] 
               │ (Scraping automático de texto)
               ▼
[Servicio IA: Normaliza precios/fechas + Interpreta T&C + Estructura Beneficios]
               │ (Guarda datos en JSON estructurado)
               ▼
[Base de Datos] ──(Filtra expiradas)
               │
               ▼
[Frontend Mínimo: Buscador / Comparador de Precios y Beneficios]
               ├── [Reporte de error por el usuario]
               └── [Redirección directa a la marca + Código copiado]
```

---

## Módulo 1: Backend (Servicio de Extracción, IA y Procesamiento)

Lógica automatizada indispensable para alimentar la base de datos sin intervención humana.

### Funcionalidades Incluidas

1. **Scraper Adaptativo de Texto Plano (McDonald’s, Burger King, Mostaza):**
   * *Justificación:* Es la puerta de entrada técnica. Sin la lectura automática del contenido web de estas tres páginas, no existe el producto.
2. **Normalización de formatos de fechas, precios y nombres:**
   * *Justificación:* **Indispensable.** Cada marca redacta sus datos de forma distinta (`$15.000` vs `15000ARS`, *"Hamburguesa Doble"* vs *"Doble Cheeseburger"*, *"Válido 20/10"* vs *"Hasta el domingo"*). Se estandarizan fechas en formato ISO, precios en enteros numéricos y nombres para permitir el correcto ordenamiento y filtrado en el frontend.
3. **Uso de IA para clasificar publicaciones, redactar resúmenes e interpretar condiciones:**
   * *Justificación:* **Indispensable.** El texto extraído suele ser caótico o puramente publicitario. La IA permite estructurar ese texto en un esquema estricto (JSON), entender reglas del tipo *"Solo pagando con tarjeta X"*, clasificar el producto (*Combo*, *Papas*, *Postre*) y resumir la letra chica de forma clara.
4. **Extraer y estructurar todos los tipos de beneficio:**
   * *Justificación:* **Indispensable.** Las ofertas incluyen variantes como 2x1, porcentaje de descuento, regalos por compra, combos especiales o cupones de monto fijo. La IA liquida y etiqueta el *tipo de beneficio* exacto para mostrar el ahorro real.
5. **Limpieza automatizada de ofertas expiradas (Time-To-Live / Desactivación en BD):**
   * *Justificación:* **Indispensable.** Al correr de forma automática, las ofertas que dejan de aparecer en las páginas web oficiales o que superaron su fecha normalizada de vencimiento se desactivan automáticamente.

---

## Módulo 2: Frontend (Interfaz del Usuario, Comparación y Redirección)

Una pantalla única, extremadamente liviana y enfocada en la velocidad de respuesta.

### Funcionalidades Incluidas

1. **Tabla/Matriz Comparativa de Ofertas:**
   * *Justificación:* **Indispensable.** Núcleo de la propuesta de valor. Permite comparar en paralelo por **Marca** (McDonald's, BK, Mostaza), **Producto**, **Precio Normalizado**, **Tipo de Beneficio** e **Interpretación resumida de condiciones**.
2. **Botonera de Redirección con Enlace Directo (Deep Link):**
   * *Justificación:* **Indispensable.** Permite al usuario ir directamente a la oferta o sitio de la marca seleccionada para concretar la compra.
3. **Copiado Automático de Código Promocional / Distintivo de Canal (App vs Web):**
   * *Justificación:* **Indispensable.** Elimina la fricción principal al redirigir; le avisa al usuario si el cupón debe usarse en la web o en la App de la marca, y le permite copiar el código en un clic.
4. **Recibir reportes de usuarios sobre ofertas vencidas o datos incorrectos:**
   * *Justificación:* **Indispensable (para respaldar la IA).** Un botón simple de *"Reportar error / Oferta vencida"* permite que los propios usuarios actúen como filtro de calidad secundario cuando la IA o el scraper fallen.

---

## Funcionalidades Excluidas (Post-MVP)

* **Usar OCR para extraer promociones publicadas solo como imágenes y leer documentos automáticamente:**
  * *Justificación de exclusión:* Incorporar visión por computadora (OCR) encarece drásticamente los costos de API de IA, añade tiempo de procesamiento por imagen y complica la infraestructura. Para validar la propuesta de valor, basta con procesar el **texto plano** de las páginas, donde se encuentra más del 80% de las promociones activas.
