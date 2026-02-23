using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repository;

public interface IPaymentsRepository
{
    void Add(PostPaymentResponse payment);
    PostPaymentResponse? Get(Guid id);
}