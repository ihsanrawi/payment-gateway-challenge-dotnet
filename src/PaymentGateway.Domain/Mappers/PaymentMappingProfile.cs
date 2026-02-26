using AutoMapper;

using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Models;
using PaymentGateway.Domain.Mappers.Converters;

namespace PaymentGateway.Domain.Mappers;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PostPaymentResponse>();
        CreateMap<PostPaymentRequest, Payment>()
            .ConvertUsing<PaymentRequestConverter>();
    }
}