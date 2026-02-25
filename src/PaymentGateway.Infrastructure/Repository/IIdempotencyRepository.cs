namespace PaymentGateway.Infrastructure.Repository;

public interface IIdempotencyRepository
{
    /// <summary>
    /// Gets the payment ID associated with an idempotency key.
    /// Returns null if the key has not been used before.
    /// </summary>
    Guid? GetPaymentId(Guid idempotencyKey);

    /// <summary>
    /// Stores a mapping from idempotency key to payment ID.
    /// </summary>
    void StoreMapping(Guid idempotencyKey, Guid paymentId);
}