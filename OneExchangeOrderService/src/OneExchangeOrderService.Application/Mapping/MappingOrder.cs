using AutoMapper;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;

namespace OneExchangeOrderService.Application.Mapping
{
    public class MappingOrder : Profile
    {
        public MappingOrder()
        {
            CreateMap<Order, OrderResponse>()
            .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.OrderItems.FirstOrDefault()))
            .ForMember(dest => dest.CancellationNumber, opt => opt.MapFrom(src => src.OrderReturn.ExternalReferenceId ?? string.Empty));


            CreateMap<OrderItem, OrderItems>()
   .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.OrderItemAttributes));

            CreateMap<OrderItemAttribute, OrderAttributeModel>()
                    .ForMember(dest => dest.AttributeType, opt => opt.MapFrom(src => src.AttributeType))
                    .ForMember(dest => dest.AttributeValue, opt => opt.MapFrom(src => src.AttributeValue));
        }
    }
}
