# DevOps checkpoint status

Snapshot: 2026-10-05. This distinguishes implemented code from evidence still needed; it does not claim that interview applications, hands-on drills, or production deployments have happened.

| Track | Evidence | Status |
|---|---|---|
| Git/GitHub fundamentals | [`git-github-labs.md`](git-github-labs.md) | Practice guide prepared; needs a reviewed PR example after the owner chooses to publish a change. |
| GitHub Actions | [Workspace CI workflow](../../../../.github/workflows/ci.yml) | Single workflow at the workspace root; it validates restore/build/test, Compose, and local image builds. The latest GitHub run must be checked after publication. |
| Linux troubleshooting | [`linux-troubleshooting.md`](linux-troubleshooting.md) | Scenarios and incident format documented; hands-on Linux drills remain to be completed. |
| Interview talk track | [`portfolio-talk-track.md`](../interview/portfolio-talk-track.md) | Spanish draft prepared; English practice/rehearsal remains. |
| FraudDetection solution | Last full recorded run (2026-10-02): Release build 0 warnings/errors, 123 unit + 45 integration tests, Compose config valid. API-only Release build passed 2026-10-05. | Full broker E2E demo remains to be verified locally with Docker Compose. |
| Local spending | No remote registry, cloud resource, or paid dependency introduced | Budget guard respected in this task. |

The 3–4 week checkpoint is partly prepared, not fully complete: hands-on GitHub/Linux evidence, a current green Actions run, local container E2E verification, and interview rehearsal remain before describing the full checkpoint as passed.
