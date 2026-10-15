# Convenciones del Proyecto - CazadorDescuentos

Este documento define las directrices y normas de trabajo para el desarrollo y mantenimiento del proyecto **CazadorDescuentos**.

---

## 1. Gestión de Versiones (Git y GitHub)

### ⚠️ Confirmación Obligatoria antes de `git push`
Antes de ejecutar cualquier comando de subida (`git push`) de cambios al repositorio remoto en GitHub (`origin` / `Agustin-Carabajal/CazadorDescuentos`), **el asistente debe pedir confirmación explícita al usuario**.

### Protocolo de subida de cambios:
1. **Revisión:** Identificar los archivos modificados, añadidos o eliminados (`git status`).
2. **Resumen al usuario:** Informar claramente los cambios realizados y el mensaje de commit propuesto.
3. **Solicitud de aprobación:** Pedir confirmación al usuario antes de enviar los cambios al repositorio remoto.
4. **Ejecución del push:** Ejecutar el comando `git push` únicamente tras haber recibido la autorización expresa del usuario.

---

## 2. Desarrollo y Filosofía del MVP (Scope Guard)
* **Principio Ultra-Lean:** No se implementará autenticación, gestión de usuarios, canal de delivery ni comparadores complejos de precios. 
* Todo desarrollo debe responder estrictamente a una de las 8 historias de [UserStories.md](UserStories.md). Si no valida la hipótesis central, queda fuera.

---

## 3. Resiliencia del Pipeline de Extracción e IA
* **Aislamiento de fallos (Batch isolation):** El fallo en la captura, OCR o respuesta del LLM de una oferta individual **nunca debe abortar el lote**. 
* **Fallback ante errores:** Si una respuesta del LLM es inválida o el OCR falla, la oferta se marca con estado `pendiente_revision` y el script continúa con la siguiente.

---

## 4. Estándares Mínimos de Datos (Para no romper la Web)
* **Fechas:** Siempre formato ISO `YYYY-MM-DD` para vencimientos (permite depuración automática sin parseos ambiguos).
* **Precios:** Valores numéricos enteros (sin símbolos `$`, puntos ni comas).
* **Filtros canónicos:** Nombres de bancos y marcas normalizados como identificadores en minúsculas (ej. `santander`, `galicia`, `mcdonalds`, `mostaza`).
* **Capturas:** Nomenclatura uniforme: `{marca}_{timestamp}_{indice}.png`.

---

## 5. Seguridad y Variables de Entorno
* **Cero secretos en el repositorio:** Toda API key (OpenAI/Anthropic/Google, etc.) y credenciales locales van en `.env`.
* Todo `.env` debe tener su contraparte `.env.example` actualizada con claves vacías para poder clonar y correr el proyecto en cualquier entorno.

