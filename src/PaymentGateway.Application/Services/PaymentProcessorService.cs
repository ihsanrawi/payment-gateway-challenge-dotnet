using AutoMapper;
using Microsoft.Extensions.Logging;

using PaymentGateway.Domain.Mappers;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.External;
using PaymentGateway.Infrastructure.Repository;

namespace PaymentGateway.Application.Services;

public class PaymentProcessorService(
    ILogger<PaymentProcessorService> logger,
    IBankClient bankClient,
    IPaymentsRepository paymentsRepository,
    IIdempotencyRepository idempotencyRepository,
    IMapper mapper)
    : IPaymentProcessorService
{
    public async Task<PostPaymentResponse> ProcessPaymentAsync(Guid idempotencyKey, PostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var existingPaymentId = idempotencyRepository.GetPaymentId(idempotencyKey);
        if (existingPaymentId.HasValue)
        {
            var existingPayment = paymentsRepository.Get(existingPaymentId.Value);
            if (existingPayment != null)    
            {
                return mapper.Map<PostPaymentResponse>(existingPayment);
            }
        }

        var bankPaymentResult = await bankClient.ProcessPaymentAsync(request, cancellationToken);

        if (!bankPaymentResult.Success)
        {
            logger.LogWarning("Bank service unavailable for idempotency key {IdempotencyKey}", idempotencyKey);
            throw new HttpRequestException("Bank service is unavailable");
        }

        var paymentId = Guid.NewGuid();
        var paymentEntity = PaymentCreationMapper.Map(paymentId, request, bankPaymentResult.Authorized);
        paymentsRepository.Add(paymentEntity);

        idempotencyRepository.StoreMapping(idempotencyKey, paymentId);

        return mapper.Map<PostPaymentResponse>(paymentEntity);
    }

    public Task<PostPaymentResponse?> RetrieveProcessedPaymentAsync(Guid paymentId)
    {
        var processedPayment = paymentsRepository.Get(paymentId);
        if (processedPayment == null)
        {
            logger.LogInformation("Payment not found with ID {PaymentId}", paymentId);
            return Task.FromResult<PostPaymentResponse?>(null);
        }

        return Task.FromResult<PostPaymentResponse?>(mapper.Map<PostPaymentResponse>(processedPayment));
    }

    public Task<PostPaymentResponse> ProcessRejectedPaymentAsync(Guid idempotencyKey, PostPaymentRequest request)
    {
        var paymentId = Guid.NewGuid();
        var paymentEntity = PaymentCreationMapper.MapRejected(paymentId, request);
        paymentsRepository.Add(paymentEntity);

        logger.LogInformation("Created rejected payment with idempotency key {IdempotencyKey} (key not locked)", idempotencyKey);

        return Task.FromResult(mapper.Map<PostPaymentResponse>(paymentEntity));
    }
}