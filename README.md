# AI Career Portfolio

Portfolio and learning workspace focused on **DevOps/Platform Engineering** in the near term, with a longer-term goal of building AI-enabled software solutions.

## Featured project

### [Fraud Detection Challenge](Projects/FraudDetection/README.md)

A .NET 8 API and Kafka worker that process transaction decisions asynchronously. The project demonstrates API design, messaging, persistence, automated tests, Docker Compose, GitHub Actions, and clear documentation of delivery guarantees and trade-offs.

- [Challenge requirements and traceability](Projects/FraudDetection/CHALLENGE_TRACEABILITY.md)
- [Architecture and design choices](Projects/FraudDetection/ARCHITECTURE.md)
- [Interview demo and talk track](Projects/FraudDetection/docs/interview/portfolio-talk-track.md)
- [DevOps learning checkpoint](Projects/FraudDetection/docs/devops/checkpoint-status.md)

The complete stack is designed for local Docker use. The repository does not publish container images or provision paid cloud resources.

## Workspace contents

- `Projects/` — portfolio projects; FraudDetection is the featured project.
- `KnowledgeBase/Architecture/` — personal reference notes. These support study but are not project evidence and some examples may not match the current FraudDetection domain.
- `NotebookLM/` — reusable study and self-assessment prompts. These are personal learning aids, not deliverables or implemented product features.

## How to review FraudDetection

1. Start with the [project README](Projects/FraudDetection/README.md) for setup, API examples, and validation status.
2. Read the [challenge traceability matrix](Projects/FraudDetection/CHALLENGE_TRACEABILITY.md) to see requirement-to-code/test evidence.
3. Review the [architecture](Projects/FraudDetection/ARCHITECTURE.md) and its documented limitations.
4. Run the local demo with Docker Compose by following the project README.

The workspace contains study notes in addition to portfolio material. The featured project is the primary evidence of implementation; learning plans and practice guides are labeled as such and are not claims of completed production experience.
