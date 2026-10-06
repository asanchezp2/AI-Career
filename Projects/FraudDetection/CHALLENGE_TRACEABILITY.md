# Challenge Traceability

This matrix maps the supplied `Challenge BE-LT.docx` to the implementation and
verification evidence in this repository. The challenge permits the candidate
to choose implementation style and database. The supplied PostgreSQL/ZooKeeper
Compose file is a development aid; this repository uses SQL Server and a local
Kafka KRaft broker.

| ID | Requirement from the challenge | Implementation | Verification / note |
|----|---------------------------------|----------------|---------------------|
| R1 | Use .NET 8 | All application and test projects target `net8.0` | `dotnet build FraudDetection.sln -c Release` |
| R2 | Use Kafka for asynchronous evaluation | API publishes `TransactionCreated`; Worker consumes it and publishes `TransactionEvaluated`; API consumes that response | Handler/persistence tests pass; local Docker Compose smoke test is pending because the Docker daemon is unavailable in this environment |
| R3 | Every created transaction is evaluated by the anti-fraud microservice, which sends a response that updates its status | Worker computes the decision and publishes the response; API applies and persists it | Worker publishing tests and API-side response-handler persistence/idempotency tests |
| R4 | Exactly three states: `pending`, `approved`, `rejected` | `TransactionStatus` and pending-only domain transitions | Domain tests and response-handler tests |
| R5 | Reject a transaction when its value is greater than 2000 | `HighValueSpecification` uses the strict `>` threshold | Specification and fraud-engine tests; threshold-equality case remains approved by this rule |
| R6 | Reject when daily accumulated value is greater than 20000 | `DailyAccumulatedSpecification` uses the strict `>` threshold | Specification/repository tests and local end-to-end scenario below |
| R7 | Creation resource accepts `sourceAccountId`, `targetAccountId`, `tranferTypeId`, and `value` | `CreateTransactionCommandConverter` accepts the challenge's literal `tranferTypeId` and the correctly spelled alias | Converter and API integration tests |
| R8 | Retrieval resource returns the transaction identifier and creation date | `GET /api/v1/transactions/{id}` returns the identifier, creation time, status, and rejection reason when applicable | API integration tests for found and unknown transactions |
| R9 | A Dockerfile is provided to help run the development environment | Root `Dockerfile` and `docker-compose.yml` run SQL Server, Kafka, API, and Worker locally | Compose configuration validates; image builds and manual smoke test are pending Docker availability |

## Explicit interpretation

The DOCX says only “Accumulated per day” and does not state an aggregation key
or time zone. This implementation aggregates transactions from the same
`sourceAccountId` within a UTC day and includes the transaction under
evaluation. This interpretation was selected for the portfolio implementation;
it is documented as an assumption, not as wording present in the source
challenge.

## Local end-to-end scenarios

1. Create a transaction with `value` greater than 2000; `POST` returns
   `pending`, then `GET` eventually returns `rejected/highvalue`.
2. Create a transaction with `value` below or equal to 2000; `GET` eventually
   returns `approved` unless the daily sum exceeds 20000.
3. Create multiple transactions for the same source account on the same UTC
   date, each at or below 2000, until the accumulated sum exceeds 20000; the
   response message causes the API to persist `rejected/dailyaccumulated`.

Run these scenarios against the local Docker Compose stack. No hosted broker,
database, or paid registry is required.
