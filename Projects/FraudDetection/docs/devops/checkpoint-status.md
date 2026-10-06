# DevOps checkpoint status

Snapshot: 2026-10-02. This records what is implemented and verified, not a claim that interview applications or real-world user work have already happened.

| Track | Evidence | Status |
|---|---|---|
| Git/GitHub fundamentals | [`git-github-labs.md`](git-github-labs.md) | Practice guide prepared; needs a reviewed PR example after the owner chooses to publish a change. |
| GitHub Actions | [CI workflow](../../.github/workflows/ci.yml) | Implemented; local equivalent build/test passed. GitHub Actions itself has not run for these unpushed changes. |
| Linux troubleshooting | [`linux-troubleshooting.md`](linux-troubleshooting.md) | Scenarios and incident format documented; hands-on Linux drills remain to be completed. |
| Interview talk track | [`portfolio-talk-track.md`](../interview/portfolio-talk-track.md) | Spanish draft prepared; English practice/rehearsal remains. |
| FraudDetection solution | Release build: 0 warnings/errors; 123 unit + 45 integration tests pass; Compose config valid | Code checkpoint passes. Docker image builds and broker E2E demo await an available Docker daemon. |
| Local spending | No remote registry, cloud resource, or paid dependency introduced | Budget guard respected in this task. |

The 3–4 week checkpoint is therefore partly prepared, not fully complete: the automated code checks pass, but hands-on GitHub/Linux evidence, local container E2E verification, and interview rehearsal still need completion before describing the full checkpoint as passed.
