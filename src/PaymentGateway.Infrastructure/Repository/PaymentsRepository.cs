using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Repository;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly List<Payment> _payments = [];

    public void Add(Payment payment)
    {
        _payments.Add(payment);
    }

    public Payment? Get(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(id), "Payment ID cannot be empty");
        }

        return _payments.FirstOrDefault(p => p.Id == id);
    }
}