# Challenge Traceability

This matrix maps the supplied `Challenge BE-LT.docx` to the implementation and evidence in this repository. The challenge permits the candidate to choose the implementation style and database. The supplied PostgreSQL/ZooKeeper Compose file is a local development aid; this solution uses SQL Server and Kafka KRaft.

| ID | Requirement | Implementation | Verification evidence |
|----|-------------|----------------|-----------------------|
| R1 | Use .NET 8 | All application and test projects target `net8.0` | Release build passed as part of `dotnet test FraudDetection.sln --no-restore -c Release` on 2026-10-06. |
| R2 | Use Kafka for asynchronous evaluation | API publishes `TransactionCreated`; Worker consumes it and publishes `TransactionEvaluated`; API consumes the response | Local Compose E2E passed on 2026-10-06: a POST returned `pending`, then GET observed the terminal state after the worker response. |
| R3 | Anti-fraud microservice sends a response that updates transaction state | Worker evaluates and publishes the decision; API applies and persists the response after consuming it | Verified for approval and both rejection rules through the running Kafka/SQL/API/Worker stack. Offsets are committed after publish/persistence. |
| R4 | Exactly three states: `pending`, `approved`, `rejected` | `TransactionStatus` and pending-only domain transitions | Unit and integration tests passed; E2E observed pending followed by approved/rejected. |
| R5 | Reject when value is greater than 2000 | `HighValueSpecification` uses strict `>` | E2E: value 2500 became `rejected/highvalue`; threshold specification tests also pass. |
| R6 | Reject when daily accumulation is greater than 20000 | `DailyAccumulatedSpecification` uses strict `>` | E2E: ten sequential 1900 transactions approved; a further 1500 for the same account/day (20500 total) became `rejected/dailyaccumulated`. |
| R7 | Create resource accepts `sourceAccountId`, `targetAccountId`, `tranferTypeId`, and `value` | `CreateTransactionCommandConverter` accepts literal `tranferTypeId` and the correctly spelled alias | API E2E used the literal `tranferTypeId`; converter and API integration tests passed. |
| R8 | Retrieval resource returns transaction identifier and creation date | `GET /api/v1/transactions/{id}` returns identifier, creation time, status, and rejection reason when applicable | API integration tests and E2E polling passed. |
| R9 | Provide a Dockerfile to help run the development environment | `Dockerfile` and `docker-compose.yml` run SQL Server, Kafka, API, and Worker | `docker compose config --quiet` passed; API and Worker images built; all four services became healthy on 2026-10-06. |

## Explicit interpretation

The DOCX says only “Accumulated per day”; it does not define an aggregation key, time zone, or whether rejected transactions count. This implementation sums all transactions recorded for the same `sourceAccountId` on a UTC calendar day, including the transaction being evaluated and previously rejected transactions. This is an explicit implementation assumption, not wording from the challenge.

## Local end-to-end evidence (2026-10-06)

- Approval: value 120 → `approved`.
- High-value rejection: value 2500 → `rejected/highvalue`.
- Daily-accumulation rejection: ten sequential values of 1900 were approved; a further value of 1500 for the same account/day (20500 total) → `rejected/dailyaccumulated`.
- API readiness reported SQL Server and Kafka healthy. The API initially failed to start while its Kafka consumer blocked host startup on an absent topic; both consumers now yield during startup/idle polling and delay transient-error retries. Compose then started API and Worker healthy with topics created on first publish.
- `dotnet test FraudDetection.sln --no-restore -c Release`: 123 unit and 45 integration tests passed.

This is a manual broker-backed smoke test, not an automated E2E test in CI. No hosted broker, database, registry, cloud service, or paid infrastructure is used.