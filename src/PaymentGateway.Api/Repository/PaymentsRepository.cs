using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repository;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly List<PostPaymentResponse> _payments = [];
    
    public void Add(PostPaymentResponse payment)
    {
        _payments.Add(payment);
    }

    public PostPaymentResponse? Get(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(id), "Payment ID cannot be empty");
        }

        return _payments.FirstOrDefault(p => p.Id == id);
    }
}