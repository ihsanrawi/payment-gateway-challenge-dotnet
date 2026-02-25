namespace PaymentGateway.Infrastructure.Repository;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly Dictionary<Guid, Guid> _mappings = [];

    public Guid? GetPaymentId(Guid idempotencyKey)
    {
        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(idempotencyKey), "Idempotency key cannot be empty");
        }

        return _mappings.TryGetValue(idempotencyKey, out var paymentId) ? paymentId : null;
    }

    public void StoreMapping(Guid idempotencyKey, Guid paymentId)
    {
        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(idempotencyKey), "Idempotency key cannot be empty");
        }

        if (paymentId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(paymentId), "Payment ID cannot be empty");
        }

        _mappings[idempotencyKey] = paymentId;
    }
}