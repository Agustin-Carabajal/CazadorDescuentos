# Technical Conventions & Architecture Guidelines

This document outlines the core conventions for developing the **CazadorDescuentos** MVP using ASP.NET Core MVC, Entity Framework Core, and SQL Server.

---

## 1. Architecture & Feature Organization

* **Monolithic Feature-Based Layout:**
  * Group controllers, views, models, and domain services by business feature (`Ingestion` and `Catalog`) rather than strict technical layers.
  * Avoid over-engineering (no generic repository patterns or separate Unit of Work classes). Use `DbContext` directly inside services or controllers.
* **Dependency Injection (DI):**
  * Register services in `Program.cs` using constructor injection.
  * Use `AddScoped` for web-request-bound services (e.g., controllers, `DbContext`).
  * Use `AddHostedService` / `BackgroundService` for background tasks (scrapers and cleanup workers). Resolve `DbContext` inside background workers using `IServiceScopeFactory`.

---

## 2. Naming Conventions

* **C# Code:**
  * **Classes, Interfaces, Methods, Properties, Controllers:** `PascalCase` (e.g., `Offer`, `CalculateDiscountAsync()`, `HomeController`).
  * **Local variables and parameters:** `camelCase` (e.g., `offerId`, `discountRate`).
  * **Private read-only fields:** `_camelCase` with leading underscore (e.g., `private readonly ApplicationDbContext _context;`).
* **Controllers & Views:**
  * Controllers must end with `Controller` (e.g., `ReportsController`).
  * Views should match the action method name in `PascalCase` (e.g., `Index.cshtml`).
* **Database / EF Core:**
  * Primary keys must be named `Id` (integer).
  * Foreign key properties must end with `Id` (e.g., `OfferId`).
  * Table names must be pluralized (e.g., `Offers`, `UserReports`).

---

## 3. Database & Entity Framework Core

* **Code-First Approach:**
  * Use EF Core migrations to manage schema updates in SQL Server.
* **Nullability & Strings:**
  * Nullable reference types (`<Nullable>enable</Nullable>`) are enforced in C#.
  * Define basic maximum string lengths using Data Annotations or Fluent API (e.g., `[MaxLength(250)]`) to optimize SQL column types.
* **Direct Mapping:**
  * Avoid heavy mapping libraries (like AutoMapper). Perform manual projection or direct `.Select(...)` queries in EF Core to keep performance high and code clear.

---

## 4. Async & Error Handling

* **Asynchronous Operations:**
  * Use `async`/`await` with the `Async` suffix **only** for I/O-bound operations (Database calls via EF Core, HTTP calls to AI APIs or fast-food websites).
* **Resilience in Background Processes:**
  * Wrap scraping and AI processing in specific `try-catch` blocks within `BackgroundService` execution loops to prevent a single site failure from stopping the entire worker. Log errors using `ILogger<T>`.