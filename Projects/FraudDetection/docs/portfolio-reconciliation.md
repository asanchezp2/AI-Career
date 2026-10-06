# Portfolio repository reconciliation

Read-only snapshot on 2026-10-02, after the local FraudDetection implementation in this workspace:

- Canonical working copy: `D:\projects\Fraud-Detection-Challenge`, branch `main`, with local uncommitted changes and no configured Git remote.
- Index repository: `D:\projects\AI-Career`, `origin` is `https://github.com/asanchezp2/AI-Career.git`; local `main` is one commit ahead of `origin/main` (`8fd919b`, “refactor: extract FraudDetection to independent repo and clean up workspace files”). It also contains `Projects/FraudDetection`.
- Tree comparison excluding `.git`, `bin`, and `obj`: 87 files have identical hashes, 16 shared paths differ, 15 files exist only in the standalone tree, and no files exist only in the embedded copy. The new/changed docs and response-consumer implementation account for the current divergence; do not present the embedded tree as an equivalent copy.
- AI-Career's local README is already an index and currently links to the public standalone URL. The exact public standalone contents were not changed or verified by publishing this local work.

## Safe publication sequence

1. Review/commit the standalone changes locally after the user approves a commit.
2. Publish the standalone repository only after explicit publication validation. There is currently no remote configured in the standalone checkout.
3. Verify the public README and CI from GitHub.
4. Update AI-Career's index and remove/deprecate its embedded copy only after the standalone is public and its content is verified. Preserve both repositories' history; never force-push.

No commit, push, or cleanup of the duplicate was performed in this task.
