# Code Review — gh-info CLI

| | |
|---|---|
| **Datum** | 2026-05-22 |
| **Branch** | feature/cliprompt01 |
| **Modus** | uncommitted (Working Tree vs HEAD, inkl. untracked) |
| **Ziel-Framework** | net10.0 (über `Directory.Build.props`) |
| **Geprüfte Dateien** | 34 (Projekt `sources/gh-info`), ~1700 LOC |
| **Tools** | `dotnet build` ✅ (0 Warnungen) · `dotnet test` ✅ (23/23) · `dotnet format` nicht ausgeführt |

> Hinweis: Die SDK-Versionserkennung des Reviewers brach mit Exit 5 ab, weil `TargetFramework`
> zentral in `Directory.Build.props` liegt statt in den `.csproj`-Dateien. Das Review wurde
> mit .NET-10-Checklisten manuell durchgeführt.

## Executive Summary

Die Implementierung ist sauber geschnitten: klare Trennung in Core-Library (typed HTTP Client,
EF-Core-Cache, Orchestrierung, DI-Extension) und CLI (Generic Host, Serilog, Spectre.Console).
Public APIs sind vollständig XML-dokumentiert, `Ensure.*`-Guards und `ConfigureAwait(false)` werden
konsistent in Library-Code verwendet, der Build ist warnungsfrei (`TreatWarningsAsErrors`), und alle
23 Tests sind grün.

**Top-3-Risiken:**

1. **[BEHOBEN] Scoped Services aus dem Root-Provider** — die CLI löste `CacheDbContext` und
   `IGitHubUserService` über den `TypeRegistrar` aus `host.Services` (Root) auf. Funktionierte nur,
   weil der Host standardmäßig in der Production-Umgebung läuft (Scope-Validierung aus). Während des
   Reviews behoben: Befehlsausführung läuft jetzt in einem expliziten `IServiceScope`.
2. **Unbegrenztes Cache-Wachstum** — abgelaufene Einträge werden nie entfernt, nur bei erneuter
   Abfrage desselben Logins überschrieben.
3. **Keine Resilienz** — transiente GitHub-API-Fehler führen sofort zum Abbruch (kein Retry).

**Befund-Zähler:** 0 Critical · 1 Major (behoben) · 2 Minor · 4 Suggestion

---

## Findings

### [Major|Architecture] Scoped Services wurden aus dem Root-Provider aufgelöst — BEHOBEN

`Program.cs` übergab `host.Services` (Root-Provider) an den `TypeRegistrar`. Der `UserInfoCommand`
(transient) hängt von `IGitHubUserService` (scoped) ab, das wiederum den scoped `CacheDbContext`
nutzt. Unter aktivierter Scope-Validierung (Development) hätte das eine
`InvalidOperationException` geworfen.

**Fix (angewendet):** Ein einzelner Scope umspannt den gesamten Command-Lauf.

```csharp
using var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();

var dbContext = scope.ServiceProvider.GetRequiredService<CacheDbContext>();
await dbContext.Database.EnsureCreatedAsync();

var app = new CommandApp<UserInfoCommand>(new TypeRegistrar(scope.ServiceProvider));
return await app.RunAsync(args);
```

Verifiziert mit `DOTNET_ENVIRONMENT=Development` — Lauf erfolgreich.

### [Minor|Performance] Cache-Lookup nutzt `lower(Login)` und umgeht den Primärschlüssel-Index

`UserCacheService.GetAsync` filtert mit `x.Login.ToLower() == normalized`. Das übersetzt nach
SQLite `lower(Login)` und kann den PK-Index auf `Login` nicht nutzen → Full-Table-Scan. Bei einem
kleinen lokalen Cache vernachlässigbar, aber unsauber.

```csharp
// Option: normalisierten Schlüssel speichern und exakt vergleichen,
// oder die Spalte mit NOCASE-Collation versehen:
user.Property(x => x.Login).UseCollation("NOCASE");
```

### [Minor|Architecture] Abgelaufene Cache-Einträge werden nie entfernt

Stale Einträge bleiben in der DB; sie werden nur bei erneuter Abfrage desselben Logins
überschrieben. Bei vielen unterschiedlichen Logins wächst `gh-info-cache.db` unbegrenzt.

```csharp
// z. B. beim Start oder periodisch abgelaufene Zeilen entfernen:
var cutoff = _timeProvider.GetUtcNow() - _options.Expiration;
await _dbContext.Users.Where(u => u.FetchedAt < cutoff)
    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
```

### [Suggestion|Architecture] `EnsureCreatedAsync` schließt Migrationen aus

Für einen verwerfbaren Cache ist `EnsureCreated` vertretbar; ein späterer Schemawechsel erfordert
aber das Löschen der DB. Bewusst gewählt — sollte dokumentiert bleiben (README-Hinweis genügt).

### [Suggestion|Security] Access-Token aus `appsettings.json`

`GitHub:AccessToken` wird aus der Konfiguration gebunden (Default `null`). Für authentifizierte
Aufrufe sollte das Token aus Umgebungsvariablen oder User-Secrets kommen, nicht aus einer
eingecheckten Datei.

```jsonc
// bevorzugt: Umgebungsvariable
//   GitHub__AccessToken=ghp_xxx
```

### [Suggestion|Code-Quality] Semantik von `--no-cache`

`--no-cache` überspringt das Lesen aus dem Cache, schreibt das Ergebnis aber weiterhin zurück
(Cache-Refresh). Das ist beabsichtigt und in der XML-Doku beschrieben; der Flag-Name könnte einen
vollständigen Bypass suggerieren. Beibehalten ist okay — ggf. im README erläutern.

### [Suggestion|Performance] Keine Resilienz-Pipeline

Ein `AddStandardResilienceHandler()` (Paket `Microsoft.Extensions.Http.Resilience`) würde Retry mit
Backoff, Circuit Breaker und Timeout ergänzen und transiente Fehler robuster machen.

---

## Tool Output Appendix

```
dotnet build GhInfo.slnx   → Build erfolgreich, 0 Warnungen, 0 Fehler
dotnet test  GhInfo.slnx   → Bestanden: 23, Fehler: 0, übersprungen: 0
```
