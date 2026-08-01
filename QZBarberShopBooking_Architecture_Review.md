# QZBarberShopBooking — Backend Architecture & Code Review

**Reviewer stance:** Senior Software Architect / Clean Architecture / Code Review, evaluating as a pre-merge PR gate.
**Scope:** All 5 projects, 134 `.cs` files — `QZBarberShopBooking.Domain`, `.Application`, `.Infrastructure`, `.Service`, `.API`. Stack: .NET 10, EF Core 10.0.1, SQL Server, AutoMapper 16, FluentValidation 12, JWT Bearer auth, Swashbuckle.
**Method:** Every finding below is grounded in source actually read during this review (file path + line numbers given wherever feasible), not inferred from naming or convention alone.

---

## 0. TL;DR for the impatient

| | |
|---|---|
| **Production-ready?** | **No.** One exploitable IDOR, no rate limiting on auth endpoints, zero automated tests. |
| **Approve this PR?** | **Request changes.** See §12 for the full top-10 blocking list. |
| **Biggest single win available** | Fix `GET /api/Bookings/{id}` — any authenticated user can read any other user's booking (§8, finding S1). |
| **Biggest architectural debt** | `QZBarberShopBooking.Service.csproj` references `QZBarberShopBooking.Infrastructure.csproj` directly, breaking the Clean Architecture Dependency Rule (§3). |
| **What's actually good** | Exception→HTTP mapping is centralized and clean; Domain has zero external dependencies; async is used correctly everywhere (no `.Result`/`.Wait()`/`async void` anywhere in 134 files); JWT/password cryptography is sound; secrets management follows the right pattern (User Secrets + env vars, empty placeholders in source). |

---

## 1. SOLID Principles

### 1.1 Single Responsibility Principle (SRP)

**Violation — `AuthService` is four services wearing one trenchcoat**
`QZBarberShopBooking.Service/Service/Auth/AuthService.cs` (390 lines) implements `LoginAsync`, `RegisterAsync`, `RegisterEmployeeAsync`, `RefreshTokenAsync`, `LogoutAsync`, `ChangePasswordAsync`, `ResetPasswordAsync`, `VerifyResetTokenAsync`, plus private token-generation/hashing helpers (`GenerateTokens`, `GenerateRefreshToken`, `GenerateResetToken`, `HashTokenSha256`, `SetRefreshTokenAsync`).

- **Why it's a violation:** the class has at least four independent reasons to change — authentication policy (login/refresh/logout), registration policy (two different flows for customer vs. employee), password-reset policy, and JWT/token format. A change to how reset tokens are hashed has no business touching login logic, yet both live in the same 390-line file with 8 injected dependencies.
- **Impact:** every unit test for "does login work" has to compile/mock all 8 dependencies, including ones only `RegisterEmployeeAsync` needs (`IRepository<Employee>`). Merge conflicts concentrate in this one file for unrelated auth features.
- **Fix:** split into `IAuthenticationService` (login/refresh/logout), `IRegistrationService` (customer/employee registration), `IPasswordResetService` (reset/verify), `ITokenService` (token generation/hashing — currently duplicated logic, see §2 Duplication). Each gets its own focused dependency list.
- **Severity: Major.**

**Violation — `IUserService` bundles self-service and admin-management concerns**
`QZBarberShopBooking.Application/Interfaces/IUserService.cs` mixes `GetCurrentUserProfile`, `GetProfileAsync`, `UpdateProfileAsync` (self-service, consumed only by `ProfileController`, class-level `[Authorize]`) with `GetAllAsync`, `GetPagedAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ToggleStatusAsync` (admin CRUD, consumed only by `UsersController`, class-level `[Authorize(Roles = "Admin")]`).

- **Why it's a violation:** two distinct client roles (any authenticated user vs. Admin) with two distinct reasons to change (self-service UX vs. admin policy) share one interface and one implementation class (`UserService.cs`, 246 lines).
- **Impact:** `ProfileController` depends on `IUserService` in full even though it calls exactly 2 of its 10 methods — this is really an ISP symptom caused by an SRP root cause. See 1.4 for the interface-level fix.
- **Fix:** split `UserService` into `UserProfileService : IUserProfileService` and `UserAdminService : IUserAdminService`; the existing controller split already proves the natural seam.
- **Severity: Minor–Major** (not urgent, but the fix is cheap and the boundary already exists in the controllers).

**Not a violation — `BookingService`**
At 413 lines it's the largest file in the solution, but every method is a genuine booking-lifecycle concern (create/update/cancel/confirm/complete/availability/stats). Private helpers (`EnsureNoOverlapAsync`, `ResolveLineItemsAsync`, `LoadBookingQuery`, `MapBooking`) are appropriately extracted. Borderline on size, but cohesive. **Not flagged as a violation**, only noted under Clean Code (§2, long class).

### 1.2 Open/Closed Principle (OCP)

**Violation — role-based `switch` in `UserService.CreateAsync` (this is also a live production bug)**
`QZBarberShopBooking.Service/Service/User/UserService.cs:128-155`:
```csharp
switch (role.Name)
{
    case "Customer":
        var customer = _mapper.Map<Customer>(createUserDto);
        ...
        createdUser = customer;
        break;
    case "Employee":
        var employee = _mapper.Map<Domain.Entities.Employee>(createUserDto);
        ...
        createdUser = employee;
        break;
    default:
        throw new ValidationException(new Dictionary<string, string[]>
        {
            { "RoleId", new[] { $"Role '{role.Name}' is not supported for user creation." } }
        });
}
```
- **Why it's a violation:** adding support for a new user type requires editing this method rather than extending the system.
- **Impact — this is not theoretical.** `DatabaseSeeder.cs:8` seeds three roles: `["Admin", "Employee", "Customer"]`. Every controller in the solution gates admin-only actions with `[Authorize(Roles = "Admin")]` (7+ occurrences across `UsersController`, `EmployeesController`, `ServicesController`, `BookingsController`). But `QZBarberShopBooking.Domain.Entities.User` is `abstract` (`User.cs:8`) with only two concrete subclasses, `Customer` and `Employee` — **there is no `Admin` entity type**. `AuthController.Register` always creates a `Customer` (`AuthService.cs:81`, hardcoded `role.Name == "Customer"` lookup). `AuthController.RegisterEmployee` always creates an `Employee`. And `UserService.CreateAsync`'s `default` branch throws for any role name that isn't literally `"Customer"` or `"Employee"` — **which includes `"Admin"`.** There is no code path anywhere in this repository that can create an Admin account. The Admin role can be seeded into the database, but no admin user can ever be provisioned through the API. Someone has to hand-write a SQL `INSERT` against the `identity.Users` table (with a `UserType` discriminator value that doesn't even have a corresponding C# class to instantiate it through) to bootstrap the first admin.
- **Fix (quick):** add an `"Admin"` case (even if it maps to a plain `User`-shaped row via a concrete `Admin : User` entity, or reuses `Employee` with a distinguishing role). **Fix (proper, resolves both the OCP violation and the bug):** replace the switch with a small `IUserFactory` keyed by role name, or a `Dictionary<string, Func<CreateUserDto, User>>` registered at startup, so new roles are additive.
- **Severity: Critical** (confirmed functional defect — a whole user type is unreachable through the product's own API — not just a design-purity concern).

**Violation — type-switch on `User` subtype for JWT claims**
`AuthService.cs:166-174`:
```csharp
if (user is Domain.Entities.Employee employee)
{
    claims.Add(new("userType", "Employee"));
    claims.Add(new("isAvailable", (employee.IsAvailableForBooking ?? true).ToString()));
}
else if (user is Customer)
{
    claims.Add(new("userType", "Customer"));
}
```
- **Why it's a violation:** every new `User` subtype (including the missing `Admin`, per above) requires editing this method.
- **Fix:** a claims-enrichment strategy keyed by `Type`, or (better, ties into §2's anemic-model discussion) a `virtual IEnumerable<Claim> GetExtraClaims()` on `User` overridden per subtype.
- **Severity: Minor.**

**Acceptable, not flagged** — `ExceptionHandlingExtensions.GetSafeMessage` (`ExceptionHandlingExtensions.cs:78-86`) pattern-matches over the closed set of first-party `AppException` subtypes to pick a safe message. This is a translation table over a deliberately closed hierarchy the team owns; extending it means adding both the exception type and the switch arm together, which is normal and low-risk. Not the same risk class as the two violations above.

### 1.3 Liskov Substitution Principle (LSP)

No behavioral LSP violations found in the entity hierarchy itself: `Customer : User` and `Employee : User` are used interchangeably everywhere a `User` is expected (`IRepository<User>` queries, `AuthService.GenerateTokens(User user)`, `SetRefreshTokenAsync`), and both substitute correctly — no overridden member narrows a contract or throws where the base wouldn't.

There is a related but distinct **inheritance-hierarchy completeness gap**, not a substitutability break: the TPH discriminator configuration (`UserConfiguration.cs:16-20`) declares three discriminator values —
```csharp
.HasDiscriminator<string>("UserType")
    .HasValue<User>("User")
    .HasValue<Customer>("Customer")
    .HasValue<Employee>("Employee");
```
`.HasValue<User>("User")` maps the abstract base type itself to a discriminator value, but `User` can never be instantiated (it's `abstract`), so that discriminator value is dead/unreachable configuration. Combined with the missing `Admin` subtype (§1.2), the entity hierarchy doesn't actually cover the three roles the rest of the system assumes exist. **Severity: Minor** (configuration dead code, not a runtime risk) — cross-referenced from the OCP finding above, which is where the real damage is.

### 1.4 Interface Segregation Principle (ISP)

**Violation — `IRepository<T>` is a 20-member fat interface**
`QZBarberShopBooking.Application/Interfaces/IRepository.cs:8-40` declares: `GetById`/`GetByIdAsync`, two `GetAll` overloads, two `GetAllListAsync` overloads, two `GetWithIncludes` overloads, `Insert`/`InsertAsync`, `InsertRange`/`InsertRangeAsync`, `Update`/`UpdateAsync`, `UpdateRange`, `Delete(int)`/`DeleteAsync(int)`/`Delete(T)`/`DeleteAsync(T)`, `DeleteRange`/`DeleteRangeAsync`, two `AnyAsync` overloads, `CountAsync` — 20 members total, sync and async duplicated for nearly everything.

- **Why it's a violation:** every consumer depends on the whole interface regardless of use. Verified against `CatalogService.cs` (the smallest, cleanest consumer): it calls only `GetAll()`, `GetByIdAsync`, `AnyAsync`, `InsertAsync`, `UpdateAsync` — 5 of 20 members. The sync `GetById`/`Insert`/`Update`/`Delete` overloads, `GetWithIncludes`, `GetAllListAsync`, all the `*Range` methods, and `CountAsync` are unused by this consumer but it still compiles against and mocks/fakes the full surface.
- **Impact:** any hand-written test double for `IRepository<T>` has to implement 20 members to satisfy the compiler even if the test only exercises 2 of them. It also signals — correctly, see §3 — that this "repository" is really just EF's `DbSet<T>` behind a thin façade, which is the deeper problem.
- **Fix:** split into `IReadRepository<T>` (query members) and `IWriteRepository<T>` (mutation members), or better, drop the generic-repository experiment in favor of purpose-built repositories/query objects per aggregate (see §10, refactor #5).
- **Severity: Major** (fat interface + leaky abstraction combine to defeat the pattern's purpose).

**Violation — see §1.1**: `IUserService` (self-service + admin CRUD) is the same root cause viewed from the interface side. `ProfileController` needs 2 of 10 members; `UsersController` needs 8 of 10 (doesn't call `GetCurrentUserProfile`/`GetProfileAsync`/`UpdateProfileAsync`). **Severity: Minor–Major.**

**Milder case, noted not flagged as urgent** — `IBookingService` mixes customer-, employee-, and admin-facing methods, but unlike `IUserService` there is exactly one controller (`BookingsController`) serving all three roles, so the "one consumer needs a narrow slice" argument is weaker here. Worth splitting eventually (customer booking vs. employee booking-management vs. admin reporting) but lower priority.

### 1.5 Dependency Inversion Principle (DIP)

**What's actually correct:** every interface (`IRepository<T>`, `IUnitOfWork`, `IAuthService`, `IBookingService`, `IEmployeeService`, `IServiceService`, `IUserService`, `INotificationService`) is *defined* in `QZBarberShopBooking.Application` and *implemented* in an outer layer (`Infrastructure` for persistence, `Service` for business logic). At the level of "who owns the abstraction," this is textbook-correct DIP, and no Service or Controller class anywhere imports `Microsoft.EntityFrameworkCore`'s `DbContext` type directly (confirmed via grep — zero `_context`/`DbContext` usage outside `Infrastructure`). Credit where due.

**Violation — the project-reference graph contradicts the interface ownership**
Read directly from the `.csproj` files:
```
QZBarberShopBooking.Service.csproj → ProjectReference: Application, Domain, Infrastructure
```
`Service` (the business-logic layer) has a compile-time dependency on `Infrastructure` (the persistence-detail layer). Two concrete causes, both verified in source:
1. `AutoMapperProfile` physically lives at `QZBarberShopBooking.Infrastructure/Mappings/AutoMapperProfile.cs`, and `Service/DI/ServiceCollectionExtensions.cs:15` does `services.AddAutoMapper(cfg => cfg.AddProfile<QZBarberShopBooking.Infrastructure.Mappings.AutoMapperProfile>())` — Service has to reference Infrastructure just to register a mapper that maps *Application* DTOs to *Domain* entities, which is conceptually an Application-layer concern, not an Infrastructure one.
2. `Service/DI/ServiceRegistrationExtensions.cs:14-15` directly wires concrete Infrastructure types:
```csharp
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));   // Repository<T> is Infrastructure
services.AddScoped<IUnitOfWork, UnitOfWork>();                     // UnitOfWork is Infrastructure
```
This is composition-root work — deciding *which concrete class* satisfies an abstraction — and it belongs in the outermost layer (`API/Program.cs`), not inside the business-logic project.

- **Why it's a violation:** Clean Architecture's Dependency Rule says source-code dependencies must point only inward. `Service` is more "policy" than `Infrastructure`; it should not need to compile against EF Core, the SQL Server provider, or any Infrastructure type at all.
- **Impact:** you cannot build, or unit-test, `QZBarberShopBooking.Service` in isolation — it drags in `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, and every EF config class transitively. This defeats one of Clean Architecture's primary paybacks: infrastructure-independent, fast, in-memory-testable business rules. It's also *why* `Service.csproj` needs an `Infrastructure` reference at all — remove these two things and the reference can be deleted entirely.
- **Fix:** move `AutoMapperProfile` into `Service` (or `Application`); move the `Repository<>`/`UnitOfWork` DI registrations out of `ServiceRegistrationExtensions.AddServiceLayer()` and into a new `Infrastructure`-owned `AddInfrastructure(IServiceCollection, IConfiguration)` extension called directly from `API/Program.cs`, alongside `AddDbContext`. After this, `Service.csproj`'s `ProjectReference` to `Infrastructure` can be deleted outright.
- **Severity: Major.**

**Minor DIP nit:** `PasswordService` has no `IPasswordService` interface — `AuthService`, `EmployeeService`, and `UserService` all inject the *concrete class* directly (`PasswordService _passwordService` in every constructor). It's registered via the `IScopedService` marker with no other interfaces implemented, so `ServiceRegistrationExtensions.cs:35-38`'s fallback path (`services.AddScoped(implementationType)`, i.e., register-as-self) kicks in. Trivial fix: add `IPasswordService` and register against it. **Severity: Minor.**

---

## 2. Clean Code

**Namespace/folder chaos across the Service layer (systemic, verified in 5 of 6 service files)**
| File (folder) | Declared namespace | Class |
|---|---|---|
| `Service/Auth/AuthService.cs` | `Service.Service.Auth` | `AuthService` |
| `Service/Booking/BookingService.cs` | `Service.Bookings` (plural, folder is singular) | `BookingService` |
| `Service/Employee/EmployeeService.cs` | `Service.Barbers` (unrelated word) | `EmployeeService` |
| `Service/SalonService/SalonServiceService.cs` | `Service.Catalog` (unrelated word, and mismatched *twice* — folder says `SalonService`, file says `SalonServiceService`, namespace says `Catalog`, class is `CatalogService`) | `CatalogService` |
| `Service/Service/PasswordService.cs` | `Service.Service` (double "Service" segment, looks like a paste artifact) | `PasswordService` |
| `Service/User/UserService.cs` | `Service.Service.User` | `UserService` |

This isn't a one-off typo, it's the norm for this layer. It costs real navigation time (searching for "CatalogService" won't find `SalonServiceService.cs` by filename; searching the `SalonService` folder won't find a class called `CatalogService`). **Fix:** pick one convention (namespace mirrors folder, both mirror class name) and apply it uniformly — e.g. `Service/Catalog/CatalogService.cs`, namespace `QZBarberShopBooking.Service.Catalog`. **Severity: Minor** individually, **Major** in aggregate as an onboarding/navigation tax.

**Type-name collision forcing verbose fully-qualified references**
`QZBarberShopBooking.Domain.Entities.BookingService` (the join entity: service-line-item of a booking) and `QZBarberShopBooking.Service.Bookings.BookingService` (the application service class) share the exact same short name. Inside `Service/Booking/BookingService.cs` this forces constructs like `Domain.Entities.BookingService` fully qualified at lines 157, 397, 405 and throughout — `using QZBarberShopBooking.Domain.Entities;` can't be used bare for this one type in its own file. **Fix:** rename the entity — `BookingServiceLine` or `AppliedService` both read cleanly and describe the concept (a specific service applied within a booking) better than the generic `BookingService` anyway. **Severity: Minor**, but a five-minute rename with an outsized daily-readability payoff.

**Dead code (confirmed, zero callers via grep across the solution)**
1. `QZBarberShopBooking.Application/Helpers/JWTHelper.cs` — `GenerateJwtToken`, `ValidateTokenWithLifeTime`, `ValidateTokenWithoutLifetime`, `GenerateRefreshToken` (lines 13-95) have no callers anywhere; only `GetPrincipalFromExpiredToken` (line 97) is used, by `AuthService.RefreshTokenAsync`. The three unused methods hardcode `Issuer = "VMS_API"` / `Audience = "VMS_WEB"` (line 34-35) — inconsistent with the real, config-driven issuer/audience (`QZBarberShop_API`/`QZBarberShop_Client`) used everywhere else, and a strong signal this file is leftover from a different project template ("VMS").
2. `QZBarberShopBooking.Infrastructure/DbContextConfigurator.cs` — an unreferenced second `DbContext` factory duplicating what `Program.cs`'s `AddDbContext<BarberShopDbContext>(...)` already configures.
3. `AuthService.RegisterEmployeeAsync`, lines 262-291 — builds an `Expression<Func<User,bool>>` via reflection to query `NormalizedEmail`/`NormalizedUserName` properties. `typeof(User).GetProperty("NormalizedEmail")` always returns `null` because `User.cs` has no such property (confirmed — only `Username`, `Email`, `PasswordHash`, `PhoneNumber`, `FirstName`, `LastName`, etc.), so this branch always falls through to the `else`. ~15 lines of always-dead reflection code, almost certainly copy-pasted from an ASP.NET Identity-style codebase.
4. `Service/DI/ServiceRegistrationExtensions.cs:44-47` — `[Obsolete("Use AddServiceLayer instead.")] AddScopedServicesFromAssembly(...)` has no callers outside its own declaration.
5. `Domain.Enums.UserType` (`Admin`/`Employee`/`Customer`) — declared, never referenced. Role discrimination is actually done via `RoleId`/`Role.Name` string comparisons and C# `is Employee`/`is Customer` pattern matching instead.
6. `Domain.Entities.Page`, `Permission`, `RolePermission` — a fully-modeled RBAC scaffold (with EF configurations, `DbSet`s, indexes) that no Service or Controller in the codebase ever reads or writes. A half-built permission system sitting dormant next to the simpler role-string authorization that's actually in use.

**Fake data shipped as if real**
`QZBarberShopBooking.Infrastructure/Mappings/AutoMapperProfile.cs:115-119`:
```csharp
private static decimal CalculateRating(Employee employee)
{
    // يمكن إضافة منطق حساب التقييم لاحقاً  ("rating calc logic can be added later")
    return 4.5m; // قيمة افتراضية ("default value")
}
```
Wired into `CreateMap<Employee, EmployeeDto>().ForMember(dest => dest.Rating, opt => opt.MapFrom(src => CalculateRating(src)))` (line 55). Every single employee, in every response, shows a `Rating` of exactly `4.5`. A frontend built against this field will render star ratings that are pure fiction. **This is the kind of thing that should never reach a PR that gets approved** — either implement real rating aggregation (there's no `Review`/`Rating` entity in the Domain to aggregate from yet, so this is more work than it looks) or remove the field from `EmployeeDto` until it's real. Shipping a stubbed value silently, with no `[Obsolete]`, no API doc caveat, and no visible marker to API consumers, is actively misleading. **Severity: Moderate** (not a security/correctness-of-money issue, but a shipped falsehood in the public API contract).

**Functionally broken DTO fields — `UpdateProfileDto`**
`QZBarberShopBooking.Application/DTO/Users/UpdateProfileDto.cs:9-13` exposes `FirstName`, `LastName`, `PhoneNumber`, `DateOfBirth`, `Address`. `ProfileController.UpdateProfile` (`ProfileController.cs:30-36`) accepts this DTO from any authenticated user and maps it via `AutoMapperProfile.cs:99-100`'s `CreateMap<UpdateProfileDto, User>()`. But `User.cs` (base class, lines 10-35) has **no `DateOfBirth` and no `Address` property at all** — only `Customer` has `DateOfBirth` (per the entity's own field list), and *nothing* in the entire Domain model has `Address`. The only configured map targets `User`, not `Customer`. **Concrete consequence:** a customer who fills in their date of birth on a profile-settings form gets a `200 OK` from `PUT /api/Profile`, but the value is never persisted anywhere — the API silently promises functionality it doesn't deliver. **Fix:** either add `CreateMap<UpdateProfileDto, Customer>()` + the missing `Address` column/property, or remove the two dead fields from the DTO so the contract stops lying to API consumers. **Severity: Moderate** (functional bug, easy to fix, easy to miss in manual testing since the endpoint "succeeds").

**Long methods / long parameter lists**
- `BookingService.CreateAsync` (`BookingService.cs:99-180`, ~80 lines including the transaction lambda) is the largest single method. Its size is partly forced by `IUnitOfWork.ExecuteInTransactionAsync(Func<CancellationToken, Task> action, ...)` having no return value, so the method has to declare `BookingDto? result = null;` (line 101) outside the lambda and assign into it at line 176, then `return result!;` (line 179) — a captured-variable + null-forgiving-operator workaround for a UoW interface gap. **Fix:** add an `ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, ...)` overload to `IUnitOfWork` and this whole pattern disappears.
- `AuthService.RegisterEmployeeAsync` (64 lines) is inflated ~40% by the dead reflection block above — deleting the dead code shrinks it to a clean ~35 lines.
- Constructor parameter counts double as an SRP smell signal: `AuthService` takes 8 dependencies, `BookingService` takes 9, `EmployeeService` takes 9. These numbers are symptoms of the SRP findings in §1.1, not separate issues — flagged here only as corroborating evidence.
- `BookingAvailabilityHelper.BuildAvailableSlots(DateTime, int, Employee, IEnumerable<EmployeeSchedule>, IEnumerable<EmployeeTimeOff>, IEnumerable<Booking>)` — 6 parameters, past the conventional ≤4 guideline. A small `AvailabilityQuery` parameter object would clean this up. **Severity: Minor.**

**Duplication**
- **Pagination boilerplate** — the "apply search filter → `ApplyOrdering`/default `OrderBy` → `Skip`/`Take` → `PaginatedResponse.Create`" sequence is repeated near-verbatim in `BookingService.GetPagedAsync`, `EmployeeService.GetPagedAsync`, `UserService.GetPagedAsync`, `CatalogService.GetPagedAsync` — 4 occurrences, ~15-20 lines each. A shared `IQueryable<T>.ToPagedResponseAsync(PagedRequest, Func<T,TDto>, CancellationToken)` extension would collapse all four to one line each.
- **Soft-delete-by-hand** — `EmployeeService.DeleteAsync`, `UserService.DeleteAsync`, `CatalogService.DeleteAsync` each manually flip `IsDeleted`/`IsActive`/`DeletedDate` inline with slightly different field combinations. A shared extension on `IDeletable` (or a `SaveChanges` interceptor that does this automatically for anything implementing `IDeletable`) would remove the duplication *and* fix the inconsistency noted in §3 (repository `Delete()` hard-deletes while every caller actually wants soft-delete).
- **Uniqueness-check-then-throw** — the "does email/username already exist → throw `ValidationException`" pattern is duplicated near-identically in `AuthService.RegisterAsync`, `AuthService.RegisterEmployeeAsync`, `EmployeeService.CreateAsync`, and `UserService.CreateAsync` (4 occurrences).
- **Interval-overlap math reimplemented four times** — the exact formula `start < otherEnd && end > otherStart` for "do two time ranges overlap" appears independently in `BookingService.EnsureNoOverlapAsync` (`BookingService.cs:357-358`) and three places inside `BookingAvailabilityHelper` (`IsInsideBreak` line 67, `IsInTimeOff` lines 74-75, `HasBookingConflict` lines 83-84). This is a correctness risk, not just a style nit: nothing stops a future edit from changing one copy's `<`/`<=` boundary semantics without touching the other three, silently making availability-checking and booking-conflict-checking disagree. **Fix:** one `static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)` helper (or a proper `TimeRange` value object, see Primitive Obsession below), used everywhere. **Severity: Moderate** — flagged here for the correctness-drift risk, not just DRY aesthetics.

**Primitive obsession**
- Money is a raw `decimal` everywhere (`Booking.SubTotal/DiscountAmount/TaxAmount/TotalAmount`, `Service.BasePrice`, `Employee.HourlyRate`, `EmployeeService.CustomPrice`) with no currency concept and no `Money` value object.
- Time windows are raw `DateTime` pairs (`StartTimeUtc`/`EndTimeUtc`) repeated across `Booking`, the `BookingService` entity, `TimeSlotDto`, `EmployeeTimeOff` — see the interval-overlap duplication above, which is the direct consequence of not having a `TimeRange` type with its own `Overlaps()` method.

**Error handling / null handling**
- **Good:** the `AppException` hierarchy (`AppException` → `BusinessRuleException`/`ConflictException`/`ForbiddenException`/`NotFoundException`/`UnauthorizedException`/`ValidationException`, all in `Application/Exceptions/`) plus `ExceptionHandlingExtensions.UseGlobalExceptionHandler` (`API/Extensions/ExceptionHandlingExtensions.cs`) is a clean, centralized, well-designed error strategy — dev-mode leaks stack traces, prod doesn't, every custom exception maps to the right status code. Zero controllers other than `AuthController` need try/catch. This is genuinely good design; credit it explicitly.
- **Bad — the one controller that doesn't trust it:** `AuthController` wraps all 7 actions in local `try { ... } catch (Exception ex) { _logger.LogError(...); return Unauthorized/BadRequest(...); }` (`AuthController.cs`, every action). This duplicates the global handler's job *and* actively produces worse behavior: `Login`'s catch-all (lines 33-39) maps **any** exception — not just wrong credentials — to `401 "Invalid email or password"`. A transient DB connectivity failure, a null-reference bug, a timeout — all get reported to the client identically as "wrong password." A real incident (DB down) would look, from the outside, like every single login attempt failing with bad credentials, actively misleading whoever's debugging it. **Fix:** delete the try/catch blocks in `AuthController`, let `AppException` subtypes bubble to the global handler like every other controller already does. **Severity: Major** (masks real failures, inconsistent with the rest of the codebase's own established pattern).
- **Nullable-reference gap:** every `.csproj` has `<Nullable>enable</Nullable>`, but `User.cs:10-15` declares `Username`, `Email`, `PasswordHash`, `PhoneNumber`, `FirstName`, `LastName` as non-nullable `string` with no initializer, no `required` modifier, and no constructor — the compiler should emit CS8618 for every one of these on every entity. Combined with the validation-coverage gap (§5 — only 3 of ~46 DTOs have FluentValidation rules), a DTO field left blank on an unvalidated Create/Update path can legitimately produce a `null` in a property the type system promises is never null. **Severity: Minor–Moderate.**

**What's genuinely good, worth crediting explicitly**
- No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or `async void` anywhere in 134 files — fully consistent async all the way through.
- `PasswordService`'s magic numbers (`Iterations`, `SaltSize`, `HashSize`) are named `const int`s, not inline literals — good practice.
- `BookingAvailabilityHelper` is a clean, pure, well-isolated static helper — a good example of *avoiding* feature envy by extracting cross-entity logic rather than cramming it into `BookingService` or onto an entity that doesn't own all the data.
- `PagedRequest.PageSize`'s setter clamps via `Math.Clamp(value, 1, MaxPageSize)` (`PagedRequest.cs:20`) — a nice, self-defending value object pattern, applied consistently everywhere `PagedRequest` is used.
- `ResetPasswordAsync` returns `true` even when the user doesn't exist (`AuthService.cs:335-336`) — correct, deliberate protection against user-enumeration via the password-reset endpoint.

---

## 3. Clean Architecture

### Layer map (as actually built, not as intended)

| Layer | Project | Contains | Depends on |
|---|---|---|---|
| Domain | `QZBarberShopBooking.Domain` | 14 entities, `TEntity` base, `IAuditable`/`IDeletable`, 3 enums | *nothing* — zero NuGet packages, zero project refs |
| Application | `QZBarberShopBooking.Application` | Interfaces (repository/UoW/6 services), 46 DTOs, 7 exceptions, 3 validators, `JWTHelper`, `UserContext`, `IQueryableExtensions` | Domain |
| Infrastructure | `QZBarberShopBooking.Infrastructure` | `DbContext`, 12 EF configs, `Repository<T>`, `UnitOfWork`, `AutoMapperProfile`, migrations, seeder | Application, Domain |
| Service | `QZBarberShopBooking.Service` | 6 business-logic service implementations, DI registration extensions | Application, Domain, **Infrastructure** ← violation |
| API | `QZBarberShopBooking.API` | 6 controllers, middleware, filters, `Program.cs` composition root | Application, Infrastructure, Service |

### Dependency Rule: technically violated, practically mostly-respected

The **only** actual reference-graph violation is `Service → Infrastructure`, fully explained in §1.5 (DIP) — not repeating the detail here, but flagging it again because it's *the* Clean Architecture finding: everything else in this section is about how well the pattern is *realized* within otherwise-correct boundaries.

**Repository pattern doesn't deliver persistence-ignorance.** `IRepository<T>.GetAll()` returns raw `IQueryable<T>` (`IRepository.cs:13-14`), and every one of the 6 Service classes does `using Microsoft.EntityFrameworkCore;` and chains EF-specific LINQ (`.Include()`, `.ThenInclude()`, `.FirstOrDefaultAsync()`, `.AnyAsync()`, `.CountAsync()`) directly against it. Representative example, `BookingService.cs:397-403`:
```csharp
private IQueryable<Domain.Entities.Booking> LoadBookingQuery()
{
    return _bookingRepository.GetAll()
        .Include(b => b.Customer).ThenInclude(c => c.Role)
        .Include(b => b.AssignedEmployee).ThenInclude(e => e.Role)
        .Include(b => b.Services).ThenInclude(s => s.Service);
}
```
The Repository pattern's whole point is to let the Application/Service layer stay ignorant of the persistence technology — swappable, in-memory-testable. Here, the "repository" is a thin pass-through to `DbSet<T>`, and the real querying logic (including `.Include` graphs) lives in the Service layer, which is directly coupled to EF Core's API surface. This isn't wrong in the sense of "will it work" — it works fine — but it means the pattern is present in *name* without delivering its primary *benefit*. See §10 for the recommended fix (entity-specific repositories or query objects that own their own `.Include` graphs, keeping `IQueryable`/EF types out of Service).

**Entity exposure through an Application contract.** `Application/Interfaces/INotificationService.cs`'s `NotifyEmployeeBookingCreatedAsync(Booking booking, CancellationToken)` takes the full `Domain.Entities.Booking` entity directly, not a lean payload DTO. This doesn't break the Dependency Rule (Application legitimately depends on Domain, which is more inward), but it does mean any future real notification channel (push payload, email template) is coupled to the entire EF entity shape — including navigation properties, audit fields, `IsDeleted` — rather than a minimal `BookingCreatedNotification(int BookingId, string BookingNumber, int EmployeeId, DateTime StartTimeUtc)` record. **Severity: Minor**, but worth fixing before `NotificationService` becomes a real integration (see §11).

**Business rules location: correctly centralized.** Verified: zero business logic in any Controller (`AuthController`'s issue is error-handling duplication, not business-rule leakage); zero business logic in Infrastructure. All business rules — overlap checks, availability computation, pricing, status transitions — live in `Service/*`, which is the right place given this codebase's chosen (non-CQRS) architecture style.

**CQRS/MediatR: not used, and that's fine.** No `MediatR` package, no `IRequest`/`IRequestHandler`. This is a legitimate, simpler architectural choice for a project this size (Controller → interface → Service class), not a defect — noted here only so it doesn't get silently read as "missing."

**Mapping: centralized but misplaced.** One `AutoMapperProfile` (121 lines, ~25 `CreateMap` calls spanning Auth/User/Employee/Booking/Service concerns) is genuinely good for mapping consistency — but its physical location in `Infrastructure/Mappings/` is the direct cause of the `Service→Infrastructure` reference (§1.5). Moving it into `Service` (or a `Mapping` folder inside `Application`) removes the need for that reference entirely.

**DTOs: purpose-built, not 1:1 mirrors.** Create/Update/Read DTOs are distinct types per operation with appropriately different nullability (`UpdateEmployeeDto` has 7 nullable fields vs. `Employee`'s full set) — this is done well. The one clear case of unnecessary indirection is `CreateCustomerDto : RegisterDto { }` (empty body, pure inheritance alias) — harmless but pointless. **Severity: Trivial.**

---

## 4. Design Patterns

| Pattern | Present? | Assessment |
|---|---|---|
| **Repository** | Yes | Implemented, but generic-only and leaks `IQueryable`/EF Core into every consumer (§3). Present in name, doesn't deliver persistence-ignorance. |
| **Unit of Work** | Yes | Correctly implemented (`UnitOfWork.cs` wraps `SaveChangesAsync`/`BeginTransactionAsync`/commit/rollback cleanly), but **under-used**: only `BookingService.CreateAsync` calls `ExecuteInTransactionAsync` explicitly; other multi-repository writes (e.g. `EmployeeService.CreateAsync`, which inserts an employee, seeds a schedule, and assigns services across 3 repositories) rely on implicit single-`SaveChangesAsync` atomicity instead. Inconsistent, not wrong (same `DbContext` per scope makes it work today), but fragile if that assumption ever changes. |
| **Dependency Injection** | Yes, plus a homemade convention layer | `ServiceRegistrationExtensions.AddServiceLayer()` scans the `Service` assembly by reflection for anything implementing the empty marker interface `IScopedService` and auto-registers it Scoped against its interfaces. Correctly implemented (no lifetime mismatches found — everything ends up Scoped, matching the per-request `DbContext`), but it's a bespoke reflection convention rather than a named pattern; the well-known `Scrutor` library does the same thing with far less custom code to maintain. **Low-priority** swap suggestion. |
| **Factory** | Narrowly, correctly | `BarberShopDbContextFactory : IDesignTimeDbContextFactory<BarberShopDbContext>` exists solely for `dotnet ef` design-time tooling — correctly scoped, not a general-purpose app Factory. No Factory pattern exists (or is currently needed) for entity/DTO creation. |
| **Strategy** | Absent, would help in 2 spots | (1) The `is Employee`/`is Customer` type-switch in `AuthService.GenerateTokens` (§1.2) is exactly the shape Strategy solves. (2) `NotificationService` is a single stub; when real channels (email/SMS/push) arrive, a Strategy/Decorator-based notifier registry avoids a growing if/switch. |
| **Decorator** | Absent | Would be a natural, non-invasive way to add caching or logging/retry behavior around `IRepository<T>` or the service interfaces without touching their implementations. Currently there's no caching and no resilience layer beyond EF's own retry policy (§7, §11) — Decorator is the idiomatic place those would go. |
| **Builder** | Absent | Not needed — DTOs are simple POCOs with no complex construction. No complaint. |
| **Singleton** | Correctly avoided as a DI lifetime | No service is registered Singleton; everything business-related is Scoped, matching the per-request `DbContext` lifetime — no captive-dependency risk found. |
| **Observer** | Absent | `NotificationService`'s placeholder (see §11) is the natural home for an eventual domain-events/pub-sub mechanism — booking creation currently *directly awaits* the notification call inline inside the transaction (`BookingService.cs:173`) rather than raising an event that decoupled subscribers react to. |
| **Mediator** | Absent (no MediatR) | Deliberate, consistent architectural choice — not flagged as a gap. |

**Anti-pattern present, not requested but worth flagging: Ambient Context / Service Locator via `UserContext`.**
`Application/Helpers/UserContext.cs` is a `static class` holding request-scoped state (`UserId`, `RoleId`, `RoleName`, etc.) sourced from a `static IHttpContextAccessor _httpContextAccessor` field that gets `Configure()`d once by `UserContextMiddleware` per request. It's used directly in controllers (`BookingsController.Create`: `UserContext.GetUserIdOrThrow()`) and services (`UserService.GetCurrentUserProfile`: `UserContext.UserId`). This is the Ambient Context anti-pattern: static, globally-reachable, externally-mutated state standing in for constructor-injected `ICurrentUserService`. It works correctly in a single ASP.NET Core app today, but (a) makes any code that reads it harder to unit test in isolation — you need real `HttpContext` plumbing rather than a fake injected abstraction — and (b) is exactly the kind of static mutable global that DI exists to avoid. **Fix:** introduce `ICurrentUserService`/`IUserContext`, constructor-inject it, populate it from `IHttpContextAccessor` inside the implementation instead of a static setter. **Severity: Minor–Moderate** (works today, actively hostile to the test-project this codebase needs — see §11).

---

## 5. Best Practices

- **DI lifetimes:** correct — everything Scoped, matching per-request `DbContext`; no Transient/Singleton misuse found (§4).
- **Configuration:** correct pattern — `appsettings.json` ships with empty `ConnectionStrings`/`JwtSettings:SecretKey` placeholders, real values via User Secrets (`UserSecretsId` set in `API.csproj`) + environment variables in prod; `AuthenticationRegistration.AddJwtAuthentication` throws `InvalidOperationException` at startup with an actionable message if the secret key is missing — good fail-fast behavior. `secrets.template.json` and `appsettings.Development.example.json` are thoughtful, safe-to-commit templates.
- **Validation coverage — a real gap.** Only 3 FluentValidation validators exist, all in `Application/Validators/Auth/` (`LoginDtoValidator`, `RegisterDtoValidator`, `RegisterEmployeeDtoValidator`). None of the Booking, Employee, Service, or User create/update DTOs have any Application-layer validation — input shape/range checking for those paths is either absent or done ad hoc inside Service methods via manual guard clauses (`BookingService.CreateAsync` throwing `ValidationException` for "Select at least one service", `BookingService.cs:116-120`). This is inconsistent and easy to forget on new endpoints since the `ValidationFilter` (`API/Filters/ValidationFilter.cs`) already does the reflection-based `IValidator<T>` lookup generically — **adding a validator for a new DTO is the entire fix, no wiring required**, which makes the current gap purely a matter of coverage, not infrastructure. **Severity: Moderate.**
- **Authentication:** sound. JWT with HMAC-SHA256, correct issuer/audience/lifetime validation, `ClockSkew` explicitly bounded to 5 minutes (not left at the 5-minute *default* by accident — it's set deliberately, but worth knowing it's not zero). Refresh tokens are cryptographically random (64 bytes), stored only as a SHA-256 hash (`AuthService.cs:231`, `HashTokenSha256`), compared via a mix of direct-equality-then-hash-fallback (`RefreshTokensMatch`, lines 351-360) — this fallback path (`storedToken == providedToken`) is a **non-constant-time comparison** used *before* the constant-time hash comparison; since the stored value is always a hash (never the raw token) in the current code path, the direct-equality branch is realistically dead in practice, but it's worth removing for clarity since a leftover raw-token-storage code path elsewhere could reactivate a timing side-channel here. **Severity: Trivial-Minor.**
- **Authorization:** role-string-based only (`[Authorize(Roles = "...")]`), no policy-based or resource-based authorization. This is adequate for coarse role gating but is **exactly why** the IDOR in §8 exists — role checks alone can't express "this Customer may only see their own bookings," which needs resource-level ownership checks that this codebase does implement correctly in some places (`BookingService.CancelAsync` checks `booking.CustomerId != userId && booking.EmployeeId != userId`) but not others (`GetByIdAsync` — see §8). The inconsistency, not the absence of a framework, is the real finding.
- **Async/CancellationToken:** used correctly (no blocking calls anywhere), but **inconsistently threaded**: `IAuthService` and parts of `IUserService`/`IBookingService` accept `CancellationToken cancellationToken = default` on every method; `IEmployeeService` and `IServiceService` accept it on **none** of their methods. A long-running paged query against either of those two interfaces cannot be cancelled by an aborted HTTP request. **Severity: Minor.**
- **Transaction consistency:** `BookingService.CreateAsync` explicitly wraps multi-step writes in `ExecuteInTransactionAsync`; `EmployeeService.CreateAsync` (insert employee + seed schedule + assign services, 3 repositories) does not, relying on implicit `SaveChangesAsync` atomicity via the shared per-request `DbContext`. Works today, inconsistent risk profile for structurally similar multi-entity writes. **Severity: Minor.**
- **Pagination/filtering:** `PagedRequest` clamps page size defensively (good, §2); `IQueryableExtensions.ApplyOrdering` builds an `OrderBy`/`OrderByDescending` expression tree from a client-supplied `sortBy` string via reflection over `typeof(T).GetProperty(sortBy, ...)`. This is **not SQL-injectable** (it's an expression tree, not string SQL), but it does let a client sort by *any public property* of the entity — including `PasswordHash`, `RefreshToken`, `ResetPasswordToken` on `User`-derived types — via a query parameter with no allow-list. Sorting doesn't leak the column *values*, only relative ordering, so practical exploitability is low, but it's an unnecessary surface. **Fix:** whitelist sortable columns per endpoint. **Severity: Minor** (see also §8, Security).

---

## 6. Project Structure

- **Solution format:** `.slnx` (the newer XML solution format), not legacy `.sln` — a modern, deliberate choice, all 5 projects correctly listed.
- **Target framework:** `net10.0` across all 5 projects, EF Core 10.0.1 — current, consistent, no version skew between projects.
- **Folder organization per project** is reasonable and conventional at the top level (`DTO/Auth`, `DTO/Bookings`, etc. mirrored by feature; `Configurations/` one file per entity; `Controllers/<Feature>/<Feature>Controller.cs`) — the structural problems are all *within* the `Service` project's internal namespace consistency (§2), not at the solution/project level.
- **Misplaced file:** `AuthenticationRegistration.cs` physically lives at `API/VMSRegistration/AuthenticationRegistration.cs` but declares `namespace QZBarberShopBooking.Infrastructure.Authentication` — neither the folder name (`VMSRegistration`, an apparent leftover from a different project called "VMS") nor the namespace (`Infrastructure.Authentication`, even though the file is physically in the `API` project) match where the file actually is. Its sibling extension classes (`CorsExtensions.cs`, `DatabaseExtensions.cs`, `ExceptionHandlingExtensions.cs`) all correctly live in `API/Extensions/` with namespace `QZBarberShopBooking.API.Extensions`. **Fix:** move the file to `API/Extensions/AuthenticationExtensions.cs`, fix the namespace to match, and update the one `using` in `Program.cs`. **Severity: Minor**, but a two-minute fix.
- **`Service/Service/PasswordService.cs`** sits directly under `Service/Service/` rather than its own subfolder (`Service/Password/PasswordService.cs`), unlike every sibling service — minor inconsistency, cosmetic.
- **No `.editorconfig`, no `Directory.Build.props`, no `nuget.config`, no `global.json`.** In practice the formatting observed across every file read in this review was consistent (4-space indent, consistent brace style) — so the *current* state is fine — but nothing is enforced going forward; style consistency is currently 100% a function of manual developer discipline. **Severity: Minor**, but cheap and high-leverage to add (see §11).
- **No circular project references** — confirmed clean DAG (Domain ← Application ← Infrastructure ← Service ← API, plus the one flagged Service→Infrastructure edge, which is a Dependency Rule violation but not a cycle).
- **No README, no `docs/` folder.** The only architecture documentation in the entire repository is a single comment in `Domain.csproj` ("Infrastructure-level package references... are intentionally omitted from the Domain project. Per Clean Architecture guidance..."). For a 5-project, 134-file solution, this is a real onboarding gap.

---

## 7. Performance Review

**In-memory aggregation where SQL aggregation belongs (the most concrete, verifiable performance finding in this review):**

`BookingService.GetStatsAsync` (`BookingService.cs:308-328`):
```csharp
var bookings = await query.ToListAsync();   // pulls every matching row into app memory

return new BookingStatsDto
{
    TotalBookings = bookings.Count,
    PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
    ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed),
    CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
    CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
    TotalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalAmount)
};
```
Every booking row matching the (optional) date range is materialized into application memory, then counted/summed five separate times via LINQ-to-Objects. The same pattern appears in `EmployeeService.GetStatsAsync` (`EmployeeService.cs:280-302`), which additionally does `.Include(e => e.Bookings)` to pull an employee's **entire booking history** into memory before filtering by date range in C# (`bookings.Where(b => b.BookingDate >= startDate.Value)`, line 289) — the date filter should be a `WHERE` clause, not a post-load `.Where()`. For a salon with years of history and many employees, this endpoint's memory/IO cost grows unboundedly with historical data rather than with the size of the requested reporting window. **Fix:** push both the date filter and the aggregation into SQL — either one `GroupBy(b => b.Status)` + `Select(g => new { g.Key, Count = g.Count() })` query, or straightforward parallel `CountAsync`/`SumAsync` calls with the `Where` applied before materialization. **Severity: High impact** once booking volume grows past a small dataset; today (single migration, presumably low data volume) it's invisible.

**Filter-after-load instead of filter-in-query:** `EmployeeService.GetAvailableEmployeesAsync` (`EmployeeService.cs:184-200`) loads *all* active/available employees with their schedules via `.ToListAsync()` (line 190), **then** filters by day-of-week/time-window availability in C# (`employees.Where(e => e.Schedules.Any(...))`, line 192). The schedule-availability predicate is expressible in SQL (`.Where(e => e.Schedules.Any(s => ...))` before `ToListAsync()`) and should be pushed down. **Severity: Medium impact**, grows with roster size.

**What's done well, credited explicitly:**
- `LoadBookingQuery()` (`BookingService.cs:397-403`) uses proper eager `.Include()/.ThenInclude()` — no lazy-loading-in-a-loop N+1 pattern anywhere in the codebase, and lazy loading isn't even enabled (`UseLazyLoadingProxies()` is absent from `Program.cs`/`Infrastructure.csproj`), which prevents accidental N+1 by construction rather than by discipline.
- `Program.cs` globally configures `sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` — a deliberate, correct choice given `LoadBookingQuery`'s three collection-shaped `Include`s (avoids the cartesian-product row explosion that a single-query strategy would produce here).
- `sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)` and `CommandTimeout(60)` are sensible resilience defaults for transient SQL Server faults.
- `Repository<T>.GetAll(bool isTracking = false)` defaults to `AsNoTracking()` — the right default for a read-heavy API, applied consistently by every Service class.

**No caching anywhere:** confirmed zero `IMemoryCache`/`IDistributedCache`/`[ResponseCache]` usage in the solution. Static, rarely-changing reference data (`Service` catalog, `UserRole`s) hits the database on every single request — every unauthenticated `GET /api/Services` call, for instance. **Severity: Medium-High impact opportunity** — low implementation cost, meaningful win given how often catalog/role data is read relative to how rarely it changes.

**Minor EF-usage note:** every `Update*Async` flow across all services (e.g. `BookingService.UpdateAsync`, line 191) loads the entity via `GetAll()` (which defaults to `AsNoTracking()`), mutates it, then calls `Repository<T>.UpdateAsync` which does `_dbSet.Attach(entity); _context.Entry(entity).State = EntityState.Modified;` (`Repository.cs:62-66`). This correctly persists the change (no bug), but marks the **entire row** as modified rather than only the changed columns, producing a wider `UPDATE` statement than necessary on every write. Not incorrect, just not maximally efficient. **Severity: Low.**

---

## 8. Security Review

**S1 — CRITICAL — Insecure Direct Object Reference (IDOR) on booking retrieval.**
`BookingsController.cs:70-76`:
```csharp
[HttpGet("{id:int}")]
[Authorize]
public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(int id)
{
    var booking = await _bookingService.GetByIdAsync(id);
    return Ok(ApiResponse<BookingDto>.Success(booking, "Booking retrieved"));
}
```
`[Authorize]` here means "any authenticated user, any role" — no `Roles=` restriction. And `BookingService.GetByIdAsync` (`BookingService.cs:49-55`) does no ownership filtering whatsoever:
```csharp
public async Task<BookingDto> GetByIdAsync(int id)
{
    var booking = await LoadBookingQuery().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted)
        ?? throw new NotFoundException("Booking", id);
    return MapBooking(booking);
}
```
**Any logged-in Customer or Employee can retrieve any other user's booking — full name, notes, service breakdown, pricing — just by incrementing the `id` in `GET /api/Bookings/{id}`.** This is directly contrasted by `CancelAsync` in the very same class (`BookingService.cs:203`), which *does* correctly check `if (booking.CustomerId != userId && booking.EmployeeId != userId) throw new ForbiddenException(...)` — proving the codebase knows how to do ownership checks, it just wasn't applied here. **Fix:** add the same ownership check (or restrict the endpoint to Admin + the booking's own customer/employee) before returning the booking. **This must block merge.**

**S2 — Major — rate limiting is configured but never enforced.** `appsettings.json` has a `RateLimiting` section (`PermitLimit: 100, Window: 1, QueueLimit: 0`), but grep for `AddRateLimiter`/`UseRateLimiter` across every `.cs` file in the solution returns zero matches. `POST /api/Auth/login`, `/register`, and `/refresh-token` are all `[AllowAnonymous]` and completely unthrottled — open to credential-stuffing and brute-force attacks with no mitigation beyond whatever sits in front of the API at the network/WAF layer (unknown, out of scope of this codebase). **Fix:** wire up ASP.NET Core's built-in `AddRateLimiter`/`UseRateLimiter` using the config section that already exists — the hard part (deciding the limits) is already done.

**S3 — Moderate — PBKDF2 iteration count below current guidance.** `PasswordService.cs:11`, `Iterations = 100_000`. The algorithm choice and implementation are otherwise sound (random 16-byte salt per password, `Rfc2898DeriveBytes` with SHA-256, `CryptographicOperations.FixedTimeEquals` for constant-time comparison — no shortcuts taken). 100,000 iterations was reasonable guidance some years ago; current OWASP recommendations for PBKDF2-HMAC-SHA256 are materially higher (≥600,000), or migrating to Argon2id entirely. **Fix:** raise the iteration count (cheap, but invalidates nothing since it's applied at hash time, not verify time — existing hashes keep working since the iteration count would ideally be encoded per-hash, which the current format doesn't do — see note below) or migrate to Argon2id.
  - *Related note:* the stored hash format (`salt || hash`, `PasswordService.cs:24-28`) doesn't encode the iteration count used, so raising `Iterations` in code would make **all existing password hashes unverifiable** unless a migration path is added (e.g., prefixing the stored value with the iteration count used, and reading it back for verification). Flag this as a migration-planning item, not just a one-line constant change.

**S4 — Minor — unrestricted sort-by surface.** Already covered in §5: `IQueryableExtensions.ApplyOrdering` allows sorting by any public property via reflection, including secret-shaped columns like `PasswordHash`/`RefreshToken`/`ResetPasswordToken`. No value disclosure, only ordering — low practical severity, but should be allow-listed as defense in depth.

**S5 — no SQL injection risk found.** Confirmed via grep: zero occurrences of `FromSqlRaw`/`ExecuteSqlRaw`/`FromSqlInterpolated`/string-concatenated SQL anywhere in the solution. All data access is parameterized LINQ-to-Entities. `ApplyOrdering`'s reflection-based sort is an expression tree, not string SQL — also not injectable.

**S6 — CORS: correct.** `CorsExtensions.cs` — Development policy is wide-open (`AllowAnyOrigin`, expected/acceptable for local dev only); Production policy is **deny-by-default** if no origins are configured (`SetIsOriginAllowed(_ => false)`, `CorsExtensions.cs:36`) rather than failing open. This is exactly the right default.

**S7 — secrets management: correct pattern.** Empty placeholders committed in `appsettings.json`, real values via `dotnet user-secrets` (dev) + environment variables (prod, `JwtSettings__SecretKey` convention documented directly in the `InvalidOperationException` message at `AuthenticationRegistration.cs:19-21` — a nice touch, the error message tells the developer exactly what command to run). `.gitignore` explicitly excludes `appsettings.Production.json`, `secrets.json`, `*.local.json` variants.

**S8 — password-reset flow: cryptographically correct, functionally incomplete.** Token generation (32 random bytes), hashing (SHA-256, Base64Url), storage (hash only, never the raw token), 1-hour expiry, and constant-time comparison on verification are all implemented correctly (`AuthService.cs:332-386`). But the raw token is never delivered anywhere — `AuthService.cs:347`: `// TODO: send raw token to user's email via Email service`. The endpoint returns `200 OK` regardless, so **the feature appears to work from the API contract but is completely non-functional** — no user can actually reset their password today, since the token they'd need never leaves the server. This is categorized primarily as a Missing Practice (§11) rather than a vulnerability, since the failure mode is "does nothing" rather than "leaks something" — but it's worth knowing this is currently a fully dark, silently-broken feature.

**S9 — over-posting / mass-assignment: checked, not an issue.** `UpdateProfileDto` (self-service, any authenticated user) exposes only `FirstName`/`LastName`/`PhoneNumber`/`DateOfBirth`/`Address` — no `RoleId`, `IsActive`, or `PasswordHash` field, so there's no privilege-escalation-via-profile-update path. `UpdateUserDto` (admin-only, `UsersController` is class-level `[Authorize(Roles = "Admin")]`) does expose `RoleId`/`IsActive`, which is correctly gated. **Good design, credited.**

**S10 — HTTPS:** `app.UseHttpsRedirection()` is only called outside Development (`Program.cs`) — correct, avoids local self-signed-cert friction while enforcing HTTPS in any real deployment.

---

## 9. Code Quality Scores (0–10)

| Axis | Score | Why |
|---|---|---|
| **SOLID** | **5/10** | DIP broken at the project-reference level (Service→Infrastructure); ISP broken on `IRepository<T>` (20-member interface) and `IUserService`; OCP broken by two type-switches, one of which (§1.2) is a confirmed production bug blocking Admin-user creation entirely. Interface *ownership* direction is consistently correct, and most classes are reasonably scoped — this keeps the score off the floor. |
| **Clean Code** | **6/10** | Consistent formatting and naming in the large majority of the code; the centralized exception hierarchy is genuinely clean. Marked down for confirmed dead code (6 distinct instances), a systemic namespace/folder mismatch across the Service layer, a fake hardcoded `Rating` value shipped as real data, and a functionally broken DTO (`UpdateProfileDto`). |
| **Clean Architecture** | **6/10** | Domain is genuinely pure (zero dependencies). Layer boundaries are respected in the actual code paths (no EF leakage into Controllers, no business logic in Infrastructure). Marked down because the Dependency Rule is technically violated at the project-reference level, and the Repository pattern doesn't deliver real persistence-ignorance (`IQueryable`/EF Core used directly throughout Service). |
| **Maintainability** | **6/10** | No tests to protect refactors (this alone caps the score at "moderate" for any nontrivial change), no `.editorconfig`/analyzers to hold the current — otherwise good — consistency in place going forward. Offset by consistent DI/exception-handling patterns that genuinely ease onboarding once you know where things live. |
| **Readability** | **7/10** | Good naming at the member/parameter level, mostly short focused methods. Docked for the namespace chaos (§2/§6) and the `BookingService` entity/class name collision forcing verbose fully-qualified references in the busiest file in the solution. |
| **Scalability** | **6/10** | Async throughout, retry policies configured, `SplitQuery` deliberately set — the foundation is sound. Docked for in-memory stat aggregation that scales with total historical data rather than the requested window, and zero caching on reference data. |
| **Performance** | **6/10** | No blocking calls, no accidental N+1 (lazy loading is off by construction), good default `AsNoTracking()`. Docked for the confirmed stats-endpoint in-memory aggregation and the filter-after-load pattern in `GetAvailableEmployeesAsync`. |
| **Security** | **5/10** | One CRITICAL, directly exploitable IDOR (S1) is disqualifying on its own for a 10 or even an 8. No SQL injection, sound cryptography, correct secrets/CORS handling, correct anti-over-posting design pull the score up from the floor — but a live, unauthenticated-within-the-app data-exposure bug plus unenforced rate limiting on login are not "minor" findings. |
| **Testability** | **2/10** | Zero test projects exist anywhere in the solution (confirmed via filesystem search). Interfaces exist everywhere, which *would* make testing straightforward — except the Service→Infrastructure reference (§1.5) means you can't even compile the business-logic project in isolation from EF Core/SQL Server, and the static `UserContext` ambient-context pattern (§4) adds friction to isolating anything that reads current-user state. |
| **Overall Architecture** | **6/10** | A genuinely well-intentioned, mostly-correct Clean Architecture attempt (the Domain-purity discipline and centralized exception handling are real signals of care) undermined by one clear boundary violation, an under-delivering Repository abstraction, and zero test coverage protecting any of it. Solidly "competent mid-project," not yet "production-grade." |

---

## 10. Refactoring Opportunities

**High Impact**
1. **Fix the IDOR** on `GET /api/Bookings/{id}` (§8, S1) — trivial code change, critical risk reduction.
2. **Break the `Service → Infrastructure` project reference** (§1.5, §3): move `AutoMapperProfile` into `Service`/`Application`; move `Repository<>`/`UnitOfWork` DI registration into an `Infrastructure`-owned `AddInfrastructure()` extension called from `API/Program.cs`. Restores the Dependency Rule and unlocks infrastructure-free unit testing of the entire Service layer.
3. **Wire up rate limiting** (§8, S2) using the `RateLimiting` config section that already exists — apply at minimum to `/api/Auth/login`, `/register`, `/refresh-token`.
4. **Add a test project** — xUnit + `WebApplicationFactory` for integration tests, plain unit tests against Service classes once #2 above makes that possible without an EF/SQL Server dependency. Currently 134 files of business logic have zero automated verification.
5. **Introduce entity-specific repositories or query objects** (§1.4, §3) to stop leaking `IQueryable<T>`/EF Core through `IRepository<T>` into every Service class — restores the abstraction the Repository pattern is supposed to provide, and would let #4's unit tests use simple in-memory fakes instead of an EF `DbContext`.

**Medium Impact**
6. Extend FluentValidation coverage to Booking/Employee/Service/User DTOs (§5) — the wiring (`ValidationFilter`) is already generic; this is pure coverage work.
7. Split `AuthService` into `IAuthenticationService`/`IRegistrationService`/`IPasswordResetService`/`ITokenService` (§1.1).
8. Split `IUserService` into `IUserProfileService`/`IUserAdminService` (§1.1/§1.4) to match the existing `ProfileController`/`UsersController` boundary.
9. Extract a shared `Overlaps(DateTime, DateTime, DateTime, DateTime)` helper (or a `TimeRange` value object) to replace the four independent reimplementations of interval-overlap logic (§2) — a real correctness-drift risk, not just DRY.
10. Remove confirmed dead code: `JWTHelper`'s 4 unused methods, `DbContextConfigurator.cs`, the dead `NormalizedEmail`/`NormalizedUserName` reflection block in `AuthService.RegisterEmployeeAsync`, the `[Obsolete]` `AddScopedServicesFromAssembly` shim, and either wire up or delete `Domain.Enums.UserType` and the unused `Page`/`Permission`/`RolePermission` RBAC scaffold.
11. **Fix Admin-user creation** (§1.2) — add the missing case/factory branch; this is a correctness fix as much as a refactor.
12. Fix `UpdateProfileDto`'s phantom `Address`/`DateOfBirth` fields (§2) — add the missing entity properties + AutoMapper map, or remove the fields from the contract.
13. Implement or explicitly remove password-reset email delivery (§8, S8) — currently a dark, silently-broken feature.
14. Implement or explicitly remove `NotificationService`'s "push notification" promise (§4, §11) — currently logging-only despite the interface name.
15. Fix `AutoMapperProfile.CalculateRating` (§2) — implement real aggregation or remove the field.

**Low Impact**
16. Resolve the namespace/folder inconsistencies across the Service project (§2, §6).
17. Rename `Domain.Entities.BookingService` to remove the name collision with the `BookingService` application-service class (§2).
18. Move `AuthenticationRegistration.cs` into `API/Extensions/` with a matching namespace (§6).
19. Remove `BaseApiController`'s redundant class-level `[Route("api/[controller]/[action]")]` — every derived controller already declares its own `[Route("api/[controller]")]`, and with `RouteAttribute`'s `Inherited=true, AllowMultiple=true` semantics both apply simultaneously; worth verifying via Swagger/route-debug output whether this produces unintended duplicate route templates, and removing the base one regardless for clarity.
20. Add `.editorconfig` and turn on nullable-warnings-as-errors for the entity-property gap noted in §2, to lock in the consistency that's already present in practice.

---

## 11. Missing Practices

Confirmed absent via direct filesystem search (`find` for test projects, Docker files, and CI/CD config all returned zero results):

- **Unit tests / integration tests** — zero test projects anywhere in the solution.
- **CI/CD** — no `.github/workflows/`, no Azure Pipelines YAML, no build pipeline of any kind.
- **Docker** — no `Dockerfile`, no `docker-compose.yml` (though `.gitignore` pre-emptively includes Node.js exclusion rules, suggesting a frontend may be added to this repo later — worth keeping in mind for future containerization planning).
- **Domain Events** — booking creation directly awaits `NotifyEmployeeBookingCreatedAsync` inline inside its own transaction rather than raising a decoupled event; no infrastructure for domain events exists.
- **Pipeline behaviors / cross-cutting request handling** — no MediatR, so no pipeline behaviors; no request-timing middleware or structured request logging either, so this cross-cutting concern is simply absent rather than handled another way.
- **API Versioning** — no `Asp.Versioning`/`Microsoft.AspNetCore.Mvc.Versioning` package, no `[ApiVersion]`, no versioned route segments. Every route is unversioned `api/[controller]`.
- **Structured/enriched logging & observability** — `ILogger`/console only, no Serilog, no correlation IDs, no request-scoped logging context, no metrics/tracing (no OpenTelemetry).
- **`.editorconfig` / analyzer ruleset** — nothing enforces the style consistency currently present in practice.
- **README / architecture documentation** — none exists beyond a single `.csproj` comment.
- **Caching layer** — no `IMemoryCache`/`IDistributedCache` anywhere (§7).
- **Resilience beyond EF's own retry** — no Polly, no circuit breakers; not urgent today since there are no outbound integrations yet, but relevant the moment email/SMS/payment providers are added for the currently-stubbed notification/reset-email features.

**What is correctly present and should NOT be listed as missing (verified, not assumed):**
- **Health checks** — genuinely implemented: `AddHealthChecks().AddDbContextCheck<BarberShopDbContext>(...)`, exposed at `/health`, `AllowAnonymous()`'d correctly (`Program.cs`).
- **Swagger/OpenAPI** — implemented, JWT bearer security scheme wired in, Development-only exposure.
- **Migrations** — EF Core migrations exist and are applied automatically in Development (`DatabaseExtensions.InitializeDatabaseAsync`); note this method is Development-only (`if (!app.Environment.IsDevelopment()) return;`, `DatabaseExtensions.cs:10-11`) and **swallows** any migration/seed failure into a log line with no rethrow (`catch (Exception ex) { logger.LogError(...); }`, lines 34-37) — meaning a failed migration in dev lets the app start anyway and fail confusingly later on first DB access, rather than failing fast at startup. Worth tightening, but migrations-as-a-concept are present and working, just not automated for Production (which is a defensible, common choice — Production migrations are often run as an explicit release step rather than on app boot).

---

## 12. Final Verdict

**1. Does this project truly follow SOLID?**
**No, not fully.** DIP is correct at the interface-ownership level but broken at the project-reference level (§1.5). ISP is violated by a 20-member `IRepository<T>` and a bundled `IUserService`. OCP is violated by two type-switches, one of which is a live bug preventing Admin-user creation. SRP is stretched in `AuthService`. This is "SOLID-aware," not "SOLID-compliant."

**2. Does it truly follow Clean Code?**
**Mostly, with confirmed, concrete gaps.** Naming and formatting are consistent across the large majority of the 134 files, and the exception-handling design is genuinely clean. But there is verified dead code (6 distinct instances), a systemic namespace/folder mismatch across the entire Service layer, a hardcoded fake value shipped as real API data, and a functionally broken DTO. "Mostly clean, not rigorously clean."

**3. Does it truly follow Clean Architecture?**
**Mostly, with one real structural violation.** Domain is genuinely pure and dependency-free — that discipline is real and worth crediting. Layer boundaries are respected in the actual code paths. But the Dependency Rule is technically broken by the `Service→Infrastructure` project reference, and the Repository pattern doesn't deliver real persistence-ignorance since `IQueryable<T>`/EF Core LINQ operators are used directly throughout the Service layer. "Clean Architecture in spirit and mostly in practice, not in the letter of the Dependency Rule."

**4. Is it production-ready?**
**No.** A confirmed, exploitable IDOR that exposes any user's booking data to any other authenticated user; no rate limiting on authentication endpoints despite the configuration existing; and zero automated test coverage across the entire business-logic layer are each independently disqualifying for a system handling customer PII and financial (pricing) data.

**5. Would I approve this project in a professional code review?**
**No — Request Changes.** The security and functional-correctness items below are blocking. The architectural and clean-code items should be tracked and largely fixed before or shortly after merge, but don't need to gate this specific review the way the blocking items do.

**6. Top 10 issues that must be fixed before approval, ranked by severity:**

1. **[CRITICAL — Security]** IDOR on `GET /api/Bookings/{id}` — any authenticated user can read any other user's booking (§8, S1; `BookingsController.cs:70-76`, `BookingService.cs:49-55`).
2. **[CRITICAL — Correctness]** Admin users cannot be created through any code path in the product — `UserService.CreateAsync`'s role switch has no `"Admin"` case (§1.2; `UserService.cs:128-155`).
3. **[Major — Security]** No rate limiting enforced on `/api/Auth/login`, `/register`, `/refresh-token` despite a ready-to-use config section (§8, S2).
4. **[Major — Architecture]** `Service.csproj` references `Infrastructure.csproj`, breaking the Dependency Rule and blocking isolated unit testing of business logic (§1.5, §3).
5. **[Major — Testing]** Zero automated tests across 134 files of business logic (§11).
6. **[Major — Correctness]** `AuthController`'s local try/catch masks the real exception type on every action — a DB outage during login is reported to the client identically to "wrong password" (§2; `AuthController.cs`).
7. **[Moderate — Clean Code]** `AutoMapperProfile.CalculateRating` always returns `4.5m` — fake data shipped as real in the public API contract (§2; `AutoMapperProfile.cs:115-119`).
8. **[Moderate — Correctness]** `UpdateProfileDto.Address`/`DateOfBirth` have no reachable destination property in the configured mapping — the profile-update contract silently doesn't do what it claims (§2; `UpdateProfileDto.cs`).
9. **[Moderate — Security]** PBKDF2 iteration count (100,000) is below current OWASP guidance for the algorithm in use (§8, S3; `PasswordService.cs:11`).
10. **[Moderate — Validation]** Only Auth DTOs have FluentValidation coverage; Booking/Employee/Service/User create/update paths are unvalidated at the Application layer despite the wiring already existing (§5).

---

*End of review. Every finding above cites a specific file and, wherever the underlying evidence supports it, exact line numbers, from source read directly during this review — not inferred from naming conventions or general familiarity with similar codebases.*
