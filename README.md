# SecureBankingSystem

SecureBankingSystem es un sistema bancario educativo construido progresivamente con una arquitectura limpia y controles de seguridad explícitos. Su propósito es mostrar cómo diseñar, implementar y verificar operaciones financieras sensibles en una aplicación moderna de ASP.NET Core.

> **Advertencia:** este proyecto es educativo. No es software certificado, auditado ni autorizado para manejar dinero real.

## Objetivo educativo

El repositorio servirá como referencia práctica para separar responsabilidades, modelar reglas bancarias, proteger datos sensibles, garantizar consistencia transaccional y probar comportamientos críticos. Cada capacidad se incorporará por fases para que sus decisiones técnicas y de seguridad puedan revisarse de forma aislada.

## Tecnologías planificadas

- .NET 10 y ASP.NET Core 10
- Entity Framework Core 10
- SQL Server
- xUnit para pruebas automatizadas
- OpenAPI para describir la API HTTP
- SHA3-256 para una auditoría encadenada
- AES-256-GCM para proteger datos sensibles en reposo

La Fase 3 está en desarrollo: el modelo de persistencia con Entity Framework Core, su registro por inyección de dependencias y la migración inicial para SQL Server ya están configurados. En desarrollo local se utiliza SQL Server LocalDB, mientras que los demás entornos deben suministrar su configuración de conexión de forma segura. Todavía no existen repositorios ni una API bancaria funcional; tampoco se incluyen autenticación, cifrado o transferencias entre cuentas.

## Arquitectura

La solución aplica una Clean Architecture pragmática:

```text
src/
├── SecureBanking.Domain
├── SecureBanking.Application
├── SecureBanking.Infrastructure
└── SecureBanking.WebApi
tests/
├── SecureBanking.Domain.UnitTests
├── SecureBanking.Application.UnitTests
├── SecureBanking.Infrastructure.UnitTests
└── SecureBanking.IntegrationTests
```

`Domain` permanece independiente. `Application` utiliza únicamente `Domain`; `Infrastructure` implementará capacidades externas apoyándose en `Application` y `Domain`; y `WebApi` actúa como punto de composición sobre `Application` e `Infrastructure`. Los proyectos de pruebas respetan esas mismas fronteras.

## Principios de seguridad

- No almacenar secretos, credenciales, cadenas de conexión ni claves en Git.
- Usar HTTPS para el transporte.
- Representar valores monetarios con `decimal`.
- Registrar fechas del sistema en UTC.
- Validar entradas y devolver errores con Problem Details.
- Mantener reglas de negocio fuera de la capa HTTP.
- Diseñar las futuras transferencias con atomicidad, consistencia, aislamiento y durabilidad.
- Incorporar concurrencia, trazabilidad, auditoría encadenada y cifrado autenticado en sus fases correspondientes.

## Uso local

Requisitos para ejecutar la solución y todas las pruebas:

- Windows.
- SDK de .NET 10.
- SQL Server Express LocalDB con la instancia `MSSQLLocalDB`.
- La herramienta local `dotnet-ef`, que se restaura desde el manifiesto del repositorio.

```powershell
dotnet tool restore
dotnet restore
dotnet build
dotnet test
dotnet run --project src/SecureBanking.WebApi
```

La API redirige a HTTPS. Al ejecutarla, consulte `GET /health` en la URL HTTPS indicada por el perfil de lanzamiento. En desarrollo, la descripción OpenAPI está disponible en `/openapi/v1.json`.

## Fases previstas

1. Base arquitectónica, endpoint de salud y pruebas iniciales.
2. Modelo de dominio para clientes y cuentas. **Completada.**
3. Persistencia con EF Core 10 y SQL Server. **En desarrollo: migración inicial y pruebas relacionales con LocalDB completadas; repositorios pendientes.**
4. Autenticación, autorización y gestión segura de identidades.
5. Transferencias ACID, historial y control de concurrencia.
6. Auditoría encadenada con SHA3-256.
7. Cifrado de datos sensibles en reposo con AES-256-GCM.
8. Endurecimiento, observabilidad y pruebas integrales de seguridad.

## Estado actual

Fase 2 terminada: además de la base arquitectónica y el endpoint `GET /health`, existe un modelo de dominio sin dependencias externas para clientes y cuentas bancarias.

El dominio incluye las entidades `Customer` y `BankAccount`; los objetos de valor `EmailAddress`, `Currency`, `Money` y `AccountNumber`; estados explícitos; y excepciones específicas para reglas esperadas del negocio. Protege, entre otras, las siguientes invariantes:

- Identificadores obligatorios y fechas de creación expresadas en UTC.
- Clientes cerrados inmutables y transiciones de estado controladas.
- Saldos no negativos, operaciones en una única moneda y prevención de sobregiros.
- Movimientos permitidos solamente en cuentas activas y cierre solamente con saldo cero.
- Números de cuenta de 12 dígitos que se muestran enmascarados de forma predeterminada.
- Direcciones de correo consideradas case-insensitive en su totalidad y almacenadas en minúsculas.
- Importes limitados a `999999999999999.9999`, compatibles con el futuro tipo `decimal(19,4)`.
- Los importes admiten como máximo cuatro decimales; una precisión mayor se rechaza y el dominio nunca redondea dinero implícitamente.
- Las cuentas congeladas deben descongelarse antes de poder cerrarse.

Todavía no existe una base de datos permanente ni una API bancaria funcional.

### Persistencia en desarrollo

La infraestructura contiene el modelo de EF Core 10 para SQL Server, su registro mediante inyección de dependencias y la migración inicial. `EmailAddress` y `AccountNumber` utilizan conversiones explícitas; `Money` se divide en columnas consultables con importe `decimal(19,4)` y moneda de tres caracteres. Las tablas aplican restricciones físicas y concurrencia optimista mediante columnas `rowversion`.

Las pruebas relacionales crean bases temporales denominadas `SecureBankingTests_{Guid}` en SQL Server LocalDB, aplican la migración y verifican la materialización completa del dominio, las restricciones de la base y conflictos reales de concurrencia; cada base se elimina al finalizar. La migración `InitialPersistence` permanece pendiente para `SecureBankingSystem_Development`: esa base de desarrollo no se ha creado ni migrado.

La conexión incluida es exclusivamente para desarrollo local. Producción debe proporcionar `ConnectionStrings:SecureBanking` mediante configuración externa segura. Para ejecutar estas pruebas en CI se necesitará un runner Windows con LocalDB o una adaptación futura a una instancia real de SQL Server, incluida una ejecutada en contenedor. EF InMemory y SQLite no sustituyen estas validaciones relacionales específicas de SQL Server.

`System.Security.Cryptography.Pkcs` 9.0.22 se fija directamente en Infrastructure como override de seguridad de la dependencia transitiva incorporada por Microsoft.Data.SqlClient. No se incluyen secretos, repositorios ni creación automática de bases al iniciar la API.
