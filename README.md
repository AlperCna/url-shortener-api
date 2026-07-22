# URL Shortener API

A URL shortener REST API built with ASP.NET Core and PostgreSQL — written as a
portfolio project with an emphasis on clean layering, test coverage, and
documented design decisions, not just working code.

> **Status: early development.** The project is being built incrementally,
> one reviewable commit at a time. This README tracks what's actually done —
> see the [Roadmap](#roadmap) for what's still ahead.

## Planned features

- `POST /api/links` — shorten a URL
- `GET /{code}` — redirect to the original URL
- `GET /api/links/{code}/stats` — click count and creation date
- `DELETE /api/links/{code}` — remove a link
- **Expiring links** — `expiresAt`; requests to an expired link return `410 Gone`
- **One-time links** — `isOneTime: true` deactivates the link after its first click
- **Password-protected links** — a password is required before redirecting

## Tech stack

- .NET 10, ASP.NET Core Web API (controller-based)
- PostgreSQL + EF Core (Npgsql)
- xUnit + FluentAssertions for unit tests, Testcontainers for integration tests
- Docker Compose
- GitHub Actions (build + test)

## Architecture

```
src/
  UrlShortener.Api/            -> controllers, middleware, DI, Program.cs
  UrlShortener.Core/           -> entities, domain rules, Base62 code generator
  UrlShortener.Infrastructure/ -> DbContext, repositories, EF configuration
tests/
  UrlShortener.UnitTests/
  UrlShortener.IntegrationTests/
```

Deliberately three projects, not five. No MediatR/CQRS/AutoMapper — the
domain is small enough that those would add indirection without adding
clarity.

## Running with Docker

```bash
docker compose up --build
```

Starts Postgres and the API (`http://localhost:8080`); pending EF Core
migrations are applied automatically on startup, so there's no separate
database setup step.

```bash
curl -X POST localhost:8080/api/links -H "Content-Type: application/json" \
  -d '{"url":"https://example.com/very/long/path"}'
curl -i localhost:8080/aB3xK9c   # 302
```

## Design decisions

**Random codes, not sequential IDs.** Short codes are 7 random Base62
characters (`0-9a-zA-Z`), generated with `RandomNumberGenerator` and rejection
sampling to avoid modulo bias — not a database identity column run through a
Base62 encoder. A sequential ID encoded to Base62 is trivially walkable
(decode, decrement, re-encode) and would let anyone enumerate other people's
links. The `Code` column carries a unique index, with up to 3 retries on
collision.

**302, not 301, for the redirect.** A 301 gets cached by the browser, so
repeat visits never reach the server again and the click counter stops
working. 302 costs a request per click but keeps every click observable.

**SSRF protection rejects literal private/loopback IPs, not hostnames that
resolve to them.** `UrlValidator` blocks `localhost`, loopback, and the
private/link-local IPv4 and IPv6 ranges (including `169.254.169.254`, the
common cloud metadata endpoint) when the host in the URL is a literal IP.
A hostname that only resolves to one of those ranges at request time (DNS
rebinding) isn't covered — that needs resolving DNS at validation time,
which felt like scope creep for this project's threat model.

**Click counting is asynchronous, except when it can't be.** A plain click
count is just a statistic, so `GET /{code}` hands the code to a
Channel-backed background queue and redirects immediately, no DB write on
the request path. One-time links are the exception: deactivating them
*gates future access*, so it happens synchronously before the redirect — a
queued deactivation would let a second concurrent request through before
the first one lands.

**Caching a mutable entity, safely.** `IMemoryCache` sits in front of
`GetByCodeAsync` (the redirect hot path) and stores live object references,
not copies — so a click-count mutation on a cached `ShortLink` is visible to
the next reader without explicit invalidation. The one sharp edge: a cached
instance can be handed to a *different* request's `DbContext`, which
wouldn't be tracking it, so `SaveChanges` would silently drop the change.
`IShortLinkRepository.Update()` re-attaches a detached instance before
saving to close that gap.

**No Redis.** `IMemoryCache` is enough at this scale; a distributed cache
only earns its complexity once the API runs on more than one instance.

## Running the tests

```bash
dotnet test
```

80 unit tests (Base62 generator, `ShortLink` domain rules, URL/SSRF
validation, `ShortLinkService`) plus 12 integration tests that boot the
real API against a disposable Postgres container via Testcontainers —
covering create, redirect status codes, one-time deactivation, password
auth, delete, SSRF rejection, and the async click counter. The integration
tests need Docker running locally.

## Roadmap

- [x] Solution skeleton (Api / Core / Infrastructure + test projects)
- [x] Base62 random code generator
- [x] `ShortLink` domain entity and rules
- [x] EF Core DbContext, entity configuration, unique index, migrations
- [x] Link creation and redirect endpoints
- [x] URL validation with SSRF protection
- [x] RFC 7807 `ProblemDetails` error handling
- [x] Expiring / one-time / password-protected link support
- [x] Click tracking, stats and delete endpoints
- [x] In-memory caching, IP-based rate limiting
- [x] Dockerfile + Docker Compose
- [x] Testcontainers-based integration tests
- [ ] GitHub Actions CI
- [ ] Swagger/OpenAPI documentation

## License

[MIT](LICENSE)
