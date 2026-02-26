using AutoMapper;
using Microsoft.Extensions.Logging;

using PaymentGateway.Domain.Entities;
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
                logger.LogInformation("Payment already processed with idempotency key {IdempotencyKey}. Returning existing payment with ID {PaymentId}", idempotencyKey, existingPaymentId);
                return mapper.Map<PostPaymentResponse>(existingPayment);
            }
        }

        var bankPaymentResult = await bankClient.ProcessPaymentAsync(request, cancellationToken);

        if (!bankPaymentResult.Success)
        {
            logger.LogWarning("Bank service unavailable for idempotency key {IdempotencyKey}", idempotencyKey);
            throw new HttpRequestException(bankPaymentResult.ErrorMessage);
        }

        var paymentId = Guid.NewGuid();
        var paymentEntity = mapper.Map<Payment>(request, opts =>
        {
            opts.Items["PaymentId"] = paymentId;
            opts.Items["IsAuthorized"] = bankPaymentResult.Authorized;
        });
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
}