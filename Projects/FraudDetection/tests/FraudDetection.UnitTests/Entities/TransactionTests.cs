using FraudDetection.Domain.Entities;
using FraudDetection.Domain.Enums;

namespace FraudDetection.UnitTests.Domain.Entities;

public class TransactionTests
{
    private static readonly Guid SourceAccountId = Guid.NewGuid();
    private static readonly Guid TargetAccountId = Guid.NewGuid();
    private static readonly DateTime FixedCreatedAt = new(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc);

    private static Transaction CreateTransaction(
        decimal value = 100m,
        int transferTypeId = 1,
        Guid? id = null,
        DateTime? createdAt = null) =>
        new(
            id ?? Guid.NewGuid(),
            SourceAccountId,
            TargetAccountId,
            transferTypeId,
            value,
            createdAt);

    // Construction --------------------------------------------------------

    [Fact]
    public void Constructor_WithPositiveValue_CreatesPendingTransaction()
    {
        var transaction = CreateTransaction(value: 100m, createdAt: FixedCreatedAt);

        Assert.NotEqual(Guid.Empty, transaction.TransactionExternalId);
        Assert.Equal(SourceAccountId, transaction.SourceAccountId);
        Assert.Equal(TargetAccountId, transaction.TargetAccountId);
        Assert.Equal(1, transaction.TransferTypeId);
        Assert.Equal(100m, transaction.Value);
        Assert.Equal(FixedCreatedAt, transaction.CreatedAt);
        Assert.Equal(TransactionStatus.Pending, transaction.Status);
        Assert.Null(transaction.RejectionReason);
    }

    [Fact]
    public void Constructor_WithoutCreatedAt_SetsUtcTimestamp()
    {
        var transaction = CreateTransaction();

        Assert.Equal(DateTimeKind.Utc, transaction.CreatedAt.Kind);
        Assert.NotEqual(default(DateTime), transaction.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NegativeOrZeroValue_ThrowsArgumentOutOfRangeException(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTransaction(value: value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveTransferTypeId_ThrowsArgumentOutOfRangeException(int transferTypeId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateTransaction(transferTypeId: transferTypeId));
    }

    [Fact]
    public void Constructor_EmptyTransactionExternalId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => CreateTransaction(id: Guid.Empty));
    }

    [Fact]
    public void Constructor_EmptySourceAccountId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Transaction(
                Guid.NewGuid(),
                Guid.Empty,
                TargetAccountId,
                1,
                100m));
    }

    [Fact]
    public void Constructor_EmptyTargetAccountId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Transaction(
                Guid.NewGuid(),
                SourceAccountId,
                Guid.Empty,
                1,
                100m));
    }

    // State transitions ---------------------------------------------------

    [Fact]
    public void Approve_FromPending_ReturnsSuccess()
    {
        var transaction = CreateTransaction();

        var result = transaction.Approve();

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
        Assert.Null(transaction.RejectionReason);
    }

    [Fact]
    public void Reject_FromPendingWithReason_ReturnsSuccess()
    {
        var transaction = CreateTransaction();

        var result = transaction.Reject(RejectionReason.HighValue);

        Assert.True(result.IsSuccess);
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
        Assert.Equal(RejectionReason.HighValue, transaction.RejectionReason);
    }

    [Fact]
    public void Reject_FromPendingWithUndefinedReason_ThrowsArgumentOutOfRangeException()
    {
        // RejectionReason is a mandatory, defined enum value — there is no way
        // to reject without a documented reason, which keeps the audit trail
        // intact (ADR-056). An undefined enum value is the closest invalid input.
        var transaction = CreateTransaction();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => transaction.Reject((RejectionReason)999));
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ReturnsFailureAndKeepsStatus()
    {
        var transaction = CreateTransaction();
        transaction.Approve();

        var result = transaction.Approve();

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
    }

    [Fact]
    public void Reject_WhenAlreadyApproved_ReturnsFailureAndKeepsStatus()
    {
        var transaction = CreateTransaction();
        transaction.Approve();

        var result = transaction.Reject(RejectionReason.HighValue);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
        Assert.Null(transaction.RejectionReason);
    }

    [Fact]
    public void Approve_WhenAlreadyRejected_ReturnsFailureAndKeepsStatus()
    {
        var transaction = CreateTransaction();
        transaction.Reject(RejectionReason.DailyAccumulated);

        var result = transaction.Approve();

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
        Assert.Equal(RejectionReason.DailyAccumulated, transaction.RejectionReason);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ReturnsFailureAndKeepsStatus()
    {
        var transaction = CreateTransaction();
        transaction.Reject(RejectionReason.DailyAccumulated);

        var result = transaction.Reject(RejectionReason.HighValue);

        Assert.True(result.IsFailure);
        Assert.Equal(TransactionStatus.Rejected, transaction.Status);
        Assert.Equal(RejectionReason.DailyAccumulated, transaction.RejectionReason);
    }
}