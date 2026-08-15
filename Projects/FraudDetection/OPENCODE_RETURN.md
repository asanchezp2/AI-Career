# OPENCODE_RETURN.md — Observability Endpoints Phase

Phase implemented: observability endpoints (liveness/readiness split, version endpoint,
Swagger always-on, compose healthcheck) for the FraudDetection project.

Date: 2026-08-13 — all 152 tests pass (111 unit + 41 integration), build 0 warnings/0 errors.

---

## 1. SourceRevisionId / commit decision (build-context limitation)

**The Docker build cannot access git.** The git repository lives at `/mnt/d/AI-Career/.git`,
which is ABOVE the Docker build context (`Projects/FraudDetection/`, used by
`docker-compose.yml` `build.context`). Inside `docker build` the `.git` directory does not
exist (it is not copied into the context), so:

- `$(git rev-parse HEAD)` does not resolve in the Docker build,
- the SDK image finds no repository to query,
- therefore the Dockerfile publish deliberately does NOT pass
  `-p:SourceRevisionId=$(git rev-parse HEAD)` — no simulated/fixed values.

**Graceful support implemented instead** (per requirement): `GET /api/v1/version` includes
a `commit` field only when the assembly carries `AssemblyMetadata("SourceRevisionId")`
(read via reflection in `VersionResponse.FromAssembly`). Because the .NET SDK appends
`SourceRevisionId` to the informational version but does NOT emit the metadata attribute,
the API csproj declares an `EmitSourceRevisionIdMetadata` target (BeforeTargets=
`GetAssemblyAttributes`, conditioned on `'$(SourceRevisionId)' != ''`) that emits the
metadata whenever `SourceRevisionId` is set. Verified empirically with a clean publish:

- Debug build inside the git work tree → MSBuild auto-detects the SHA →
  `commit: 64660abd7144d9113e5613d830391e29096e42d1` (live-verified via `/api/v1/version`).
- `dotnet publish -p:SourceRevisionId=<sha>` → metadata follows the property (NOTE: inside a
  git work tree MSBuild's own git detection can override the command-line property; in the
  Docker container, where no git exists, the passed SHA wins).
- Docker builds → no `.git` in context → SourceRevisionId empty → `commit` omitted from JSON.

Any future build with access to git can add `-p:SourceRevisionId=$(git rev-parse HEAD)` to
the Dockerfile publish with ZERO code changes. See ADR-059.

## 2. NuGet / network status

**Network restore WORKED** (nuget.org reachable). Packages added:
`AspNetCore.HealthChecks.SqlServer` 9.0.0 and `AspNetCore.HealthChecks.Kafka` 9.0.0
(compatible with net8.0). Transitive resolution verified clean: Confluent.Kafka 2.15.0
(matches the version already pinned in FraudDetection.Infrastructure and
FraudDetection.Worker), Microsoft.Data.SqlClient 5.2.2, EF Core stays 8.0.11 — no conflicts.

## 3. Live smoke test (beyond the required verification)

The user's docker-compose stack (SQL Server :1433 + Kafka :9092) was already running, so the
Production-mode API was smoke-tested against the REAL dependencies:

```
GET /health/live   -> {"status":"Healthy","checks":[],"totalDurationMs":3}            HTTP 200
GET /health/ready  -> {"status":"Healthy","checks":[{"name":"sqlserver","status":"Healthy",
                      "durationMs":2078},{"name":"kafka","status":"Healthy","durationMs":622}],
                      "totalDurationMs":2086}                                          HTTP 200
GET /api/v1/version-> {"version":"1.0.0.0","informationalVersion":"1.0.0+64660abd...",
                      "environment":"Production","commit":"64660abd..."}               HTTP 200
GET /swagger/index.html                                                               HTTP 200 (Production)
```

Without the compose env vars (localdb default conn string), `/health/ready` honestly returned
503 with `{"name":"sqlserver","status":"Unhealthy","durationMs":1395,"description":"LocalDB is
not supported on this platform."}` — the failure contract works.

## 4. Notes for a future implementer

- The integration test factory replaces the health check registrations via
  `services.RemoveAll<IConfigureOptions<HealthCheckServiceOptions>>()` + re-registration of
  `FakeHealthCheck` instances with the same names/tags. This was verified empirically: in
  .NET 8.0.11 the registrations do NOT live as `IHealthCheck`/`HealthCheckRegistration`
  DI descriptors (RemoveAll on those changes nothing).
- `HealthCheckOptions.Timeout` does NOT exist in .NET 8 — per-check timeouts are set via the
  `timeout:` parameter of `AddSqlServer`/`AddKafka`; the Kafka `ProducerConfig` also carries
  `MessageTimeoutMs = 5000`.
- `AddKafka` 9.0.0 signature is `(builder, ProducerConfig|Action<ProducerConfig>, topic, name, ...)`
  — topic precedes name. Named arguments used in Program.cs, so ordering is safe.
- Local builds inside the git work tree auto-append the SHA to the informational version
  ("1.0.0+64660abd..."). The version endpoint integration test asserts the commit field
  conditionally (present ⇒ non-empty) for exactly this reason.
- Swagger is now enabled in ALL environments (Production compose container serves /swagger);
  HSTS remains non-dev-only.
- The Worker intentionally has no HTTP observability (documented in ADR-059).
- Dockerfile HEALTHCHECK and the compose api healthcheck both use `/health/live` to avoid
  restart cascades on dependency blips.