using AutoMapper;

using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Mappers;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PostPaymentResponse>();
    }
}