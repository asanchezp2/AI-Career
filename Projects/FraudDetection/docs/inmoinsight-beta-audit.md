# InmoInsight beta readiness

Audit and follow-up date: 2026-10-02. Source checked against the local InmoInsight checkout and roadmap last updated 2026-05-27. The user authorized follow-up code changes in `D:\projects\inmoinsight`. No hosted database, OAuth provider, deployment, paid service, or cloud resource was modified.

## Applied locally

- Production session secrets must be explicitly configured, at least 32 characters, and not the development placeholder. Production authorization fails closed if Supabase is unavailable.
- Google login requires a verified email and provider subject. Production HTTPS cookies are Secure, HttpOnly, and SameSite.
- Upload processing/status access checks ownership; upload reading has a 50 MB streaming cap and uses UUID-based names. Upload tracking failure cleans the raw file; deletion also cleans the anonymizer mapping.
- Added `/health/live` and `/health/ready`; Docker probes liveness.
- Added `file_size` to the initial schema and an idempotent migration for existing deployments.
- Removed misleading language that treated Railway credits as a guaranteed no-cost hosting plan. No deployment was made.
- Updated outdated message-filter tests to use the existing Supabase-scoped loader and match the default exclusion of `IRRELEVANTE`.

## Verification

- Python syntax compilation passed for the modified application and test modules.
- Full test suite: 282 passed, 14 warnings. The remaining stale assertion was aligned with the default exclusion of `IRRELEVANTE`.
- Docker daemon, hosted Supabase schema, OAuth configuration, persistence/restore, and production deletion lifecycle have not been validated.

## Remaining beta blockers

| Priority | Blocker | Required before inviting real users |
|---|---|---|
| P0 | Supabase RLS not implemented or verified; server-side owner checks are not a substitute for database policies. | Design and test per-user policies and key scope against a real or local Supabase instance. |
| P0 | Local shared filesystem storage has no verified persistence/backup/restore or retention expiry. | Isolate job artifacts, define retention, test deletion and restore, and provide clear privacy notice. |
| P1 | ZIP archive expansion, member-count, path, and CPU/time bounds remain unimplemented. | Add archive-level resource limits before accepting untrusted exports. |
| P1 | User ownership key is normalized email rather than immutable provider subject/internal ID. | Migrate identity and data relations before product use. |
| P1 | Session expiry/rotation and live Google OAuth callback configuration were not validated. | Set expiry/rotation policy and verify provider configuration in an allowed environment. |
| P1 | Free hosting does not guarantee zero spend or durable storage. | Keep the beta local until a free option has enforceable budget and storage guarantees; user policy is zero spend. |

## Decision

The changes reduce local development risk, but InmoInsight is not ready for a public or invited beta. Continue with synthetic/anonymized local data. Do not provision hosting or paid services under the current zero-spend constraint.
