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

## Design decisions

**Random codes, not sequential IDs.** Short codes are 7 random Base62
characters (`0-9a-zA-Z`), generated with `RandomNumberGenerator` and rejection
sampling to avoid modulo bias — not a database identity column run through a
Base62 encoder. A sequential ID encoded to Base62 is trivially walkable
(decode, decrement, re-encode) and would let anyone enumerate other people's
links. The `Code` column will carry a unique index with a small bounded
number of retries on collision.

More decisions (redirect status code, SSRF protection, caching strategy,
async click tracking) will be documented here as those pieces land — see the
[Roadmap](#roadmap).

## Running the tests

```bash
dotnet test
```

Currently: 32 unit tests covering the Base62 code generator and the
`ShortLink` domain rules (expiration, one-time use, password state).
Integration tests will be added once the data and API layers exist.

## Roadmap

- [x] Solution skeleton (Api / Core / Infrastructure + test projects)
- [x] Base62 random code generator
- [x] `ShortLink` domain entity and rules
- [ ] EF Core DbContext, entity configuration, unique index, migrations
- [ ] Link creation and redirect endpoints
- [ ] URL validation with SSRF protection
- [ ] RFC 7807 `ProblemDetails` error handling
- [ ] Expiring / one-time / password-protected link support
- [ ] Click tracking, stats and delete endpoints
- [ ] In-memory caching, IP-based rate limiting
- [ ] Dockerfile + Docker Compose
- [ ] Testcontainers-based integration tests
- [ ] GitHub Actions CI
- [ ] Swagger/OpenAPI documentation

## License

[MIT](LICENSE)
