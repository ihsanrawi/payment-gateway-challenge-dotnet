using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Repository;

public interface IPaymentsRepository
{
    /// <summary>
    /// Adds a new payment to the list of payments.
    /// </summary>
    void Add(Payment payment);
    
    /// <summary>
    /// Gets a payment by its ID.
    /// Returns null if no payment with the given ID exists.
    /// </summary>
    Payment? Get(Guid id);
}