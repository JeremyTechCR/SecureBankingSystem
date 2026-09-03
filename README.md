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

La fase actual incorpora exclusivamente el modelo de dominio. No incluye Entity Framework Core, conexión a SQL Server, autenticación, cifrado, transferencias entre cuentas ni una API bancaria funcional.

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

Requisitos: SDK de .NET 10.

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/SecureBanking.WebApi
```

La API redirige a HTTPS. Al ejecutarla, consulte `GET /health` en la URL HTTPS indicada por el perfil de lanzamiento. En desarrollo, la descripción OpenAPI está disponible en `/openapi/v1.json`.

## Fases previstas

1. Base arquitectónica, endpoint de salud y pruebas iniciales.
2. Modelo de dominio para clientes y cuentas. **Completada.**
3. Persistencia con EF Core 10, SQL Server y migraciones.
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

Todavía no existe persistencia, integración con SQL Server ni una API bancaria funcional.
