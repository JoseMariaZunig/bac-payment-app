# BAC Payment Console — versión web (Docker)

Migración del WinForm original a una arquitectura de dos contenedores:

- **`api/`** — ASP.NET Core 8 Web API. Contiene toda la lógica que antes vivía
  en `Form1.cs`: obtención de token OAuth2, consulta a Softland (SQL Server),
  construcción del payload ISO 20022 y envío/rastreo del pago en BAC.
- **`web/`** — Frontend estático (HTML/CSS/JS) servido por Nginx. Reproduce
  los 4 botones del formulario original (Obtener token → Consultar Softland →
  Realizar pago → Rastrear pago) con una consola de log con los mismos
  colores (verde = éxito, rojo = error) que tenía el `richTextBox1`.

El **token y los datos de la transacción viajan entre el navegador y la API
en cada llamada** (no se guardan en memoria del servidor), para que la API
sea completamente sin estado — esto es lo que permite correr varias réplicas
del contenedor `api` sin problema si algún día lo necesitas.

## ⚠️ Antes de correrlo

1. Las clases `PaymentRequest`, `TrackingRequest`, `Fr`, `FIId`, `AccountId`,
   `Othr`, etc. en `api/Models/BacIsoModels.cs` **fueron reconstruidas** a
   partir del uso que se les daba en tu `Form1.cs`. Si tienes el archivo
   original donde se definían esas clases, compártelo para verificar que los
   nombres de campo coincidan exactamente con lo que BAC espera.
2. El campo `categoriaPago` (`CtgyPurp.Cd`) nunca se asignaba en tu código
   original (quedaba vacío). Aquí asumí `SALA` para planilla y `SUPP` para
   proveedores, siguiendo el comentario `// SALA o SUPP` que ya tenías en el
   código. **Confirma estos códigos con BAC** antes de usar en producción.
3. Todas las credenciales (SQL, client secret de BAC) se sacaron del código y
   ahora se leen de variables de entorno — ver `.env.example`.

## Cómo correrlo

```bash
cp .env.example .env
# edita .env y llena los valores reales

docker compose up --build
```

- Frontend: http://localhost:8080
- API: http://localhost:8081 (health check en `/health`)

## Estructura de endpoints de la API

| Método | Ruta                     | Equivalente en el WinForm      |
|--------|--------------------------|---------------------------------|
| POST   | `/api/token/obtain`      | `button1_Click` (Obtener Token) |
| POST   | `/api/softland/consultar`| `button4_Click` (Softland)      |
| POST   | `/api/payment/procesar`  | `button2_Click` (Realizar Pago) |
| POST   | `/api/payment/rastrear`  | `button3_Click` (Rastrear Pago) |

## Notas de producción

- El `HttpClientHandler` acepta cualquier certificado del servidor
  (`DangerousAcceptAnyServerCertificateValidator`), igual que el WinForm
  original. Si BAC ya usa un certificado válido en el ambiente que vayas a
  usar, quita esa línea en `api/Program.cs`.
- Ajusta `CORS_ALLOWED_ORIGINS` en `.env` al dominio real donde sirvas el
  frontend cuando salgas de `localhost`.
- Considera un secreto manager (Azure Key Vault, Docker secrets, etc.) en
  vez de un `.env` plano si esto corre en un entorno compartido.
