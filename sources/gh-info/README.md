# gh-info

A small .NET 10 console tool that fetches a public GitHub user's profile and caches it locally.

## Usage

```bash
gh-info <username>            # show the profile (uses the local cache when fresh)
gh-info <username> --no-cache # bypass the cache and fetch a fresh copy
```

Output is a coloured [Spectre.Console](https://spectreconsole.net/) table showing the login, name,
bio, public-repo count, follower count and account creation date, plus whether the data came from
the cache or the GitHub API.

## How it works

- **Typed HTTP client** (`IGitHubUsersClient`) calls `GET https://api.github.com/users/{login}`.
- Results are cached in a local **SQLite** database (`gh-info-cache.db`, EF Core). A second lookup
  within the configured expiration window (15 minutes by default) is served from the cache.
- `--no-cache` forces a fresh API call; the cache is still refreshed with the new result.
- Bootstrapped with the **Generic Host** + dependency injection; structured logging via **Serilog**
  (console sink, written to `stderr` so it never pollutes the table on `stdout`).

## Configuration (`appsettings.json`)

| Section | Key | Default | Description |
|---|---|---|---|
| `GitHub` | `BaseAddress` | `https://api.github.com/` | API base URL |
| `GitHub` | `UserAgent` | `gh-info` | required `User-Agent` header |
| `GitHub` | `AccessToken` | `null` | optional PAT (raises rate limits) |
| `GitHub` | `Timeout` | `00:00:30` | request timeout |
| `Cache` | `Expiration` | `00:15:00` | max age before re-fetch |
| `Cache` | `DatabasePath` | `gh-info-cache.db` | SQLite file path |
| `Serilog` | `MinimumLevel` | `Information` | log level |

> Provide the access token via an environment variable (`GitHub__AccessToken`) rather than checking
> it into `appsettings.json`.

## Projects

| Project | Purpose |
|---|---|
| `src/GhInfo.Core` | GitHub client, models, EF Core cache, orchestration, DI extension |
| `src/GhInfo.Cli` | Generic Host bootstrapping, Serilog, Spectre command + table |
| `tests/GhInfo.Core.Tests` | xUnit + FakeItEasy + AwesomeAssertions unit tests |

## Build & test

```bash
dotnet build GhInfo.slnx
dotnet test  GhInfo.slnx
dotnet run --project src/GhInfo.Cli -- octocat
```

Note: the cache uses `EnsureCreated`, so a schema change requires deleting `gh-info-cache.db`.
