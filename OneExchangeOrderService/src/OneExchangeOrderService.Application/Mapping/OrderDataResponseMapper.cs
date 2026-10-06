using AutoMapper;
using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.OrderDetails;
using OrderItem = OneExchangeOrderService.Domain.Entities.ExchangeOrder.OrderItem;


namespace OneExchangeOrderService.Application.Mapping
{
    public class OrderDataResponseMapper : Profile
    {  // Base -> Base
        public OrderDataResponseMapper()
        {
            CreateMap<BaseEntity, BaseResponse>();

            // Order -> OrderDataResponse
            CreateMap<Order, OrderDataResponse>()
                .IncludeBase<BaseEntity, BaseResponse>()
                .ForMember(d => d.OrderItems, m => m.MapFrom(s => s.OrderItems))
                .ForMember(d => d.ExchangeTransactions, m => m.MapFrom(s => s.ExchangeTransactionLogs))
                .ForMember(d => d.OrderGuests, m => m.MapFrom(s => s.OrderGuests))
                .ForMember(d => d.OrderPayments, m => m.MapFrom(s => s.OrderPayments))
                .ForMember(d => d.OrderReturn, m => m.MapFrom(s => s.OrderReturn))
                .ForMember(d => d.OrderRefunds, m => m.MapFrom(s => s.OrderRefunds));

            // OrderItem -> OrderItemResponse
            CreateMap<OrderItem, OrderItemResponse>()
                .IncludeBase<BaseEntity, BaseResponse>()
                .ForMember(d => d.Order, m => m.Ignore()) // avoid cycles
                .ForMember(d => d.OrderItemAttributes, m => m.MapFrom(s => s.OrderItemAttributes));

            // OrderItemAttribute -> OrderItemAttributeResponse
            CreateMap<OrderItemAttribute, OrderItemAttributeResponse>()
                .IncludeBase<BaseEntity, BaseResponse>()
                .ForMember(d => d.OrderItem, m => m.Ignore()); // avoid cycles

            // OrderGuest -> OrderGuestResponse
            CreateMap<OrderGuest, OrderGuestResponse>()
                    .IncludeBase<BaseEntity, BaseResponse>()
                    .ForMember(d => d.Order, m => m.Ignore()); // avoid cycles

            // OrderPayment -> OrderPaymentResponse
            CreateMap<OrderPayment, OrderPaymentResponse>();
            //.IncludeBase<BaseEntity, BaseResponse>();
            //.ForMember(d => d.Order, m => m.Ignore()) // DTO has entity-typed nav; ignore to prevent mismatch/cycle
            //.ForMember(d => d.OrderRefunds, m => m.MapFrom(s => s.OrderRefunds));

            // ExchangeTransactionLog -> ExchangeTransactionLogResponse
            CreateMap<ExchangeTransactionLog, ExchangeTransactionLogResponse>()
                .IncludeBase<BaseEntity, BaseResponse>()
                .ForMember(d => d.Order, m => m.Ignore()); // avoid cycles

            // OrderReturn -> OrderReturnResponse
            CreateMap<OrderReturn, OrderReturnResponse>()
                    .IncludeBase<BaseEntity, BaseResponse>()
                    .ForMember(d => d.Order, m => m.Ignore()) // avoid cycles
                    .ForMember(d => d.OrderRefunds, m => m.MapFrom(s => s.OrderRefunds))
                    // If your entity uses a different name (e.g., CancellationNumber), map it:
                    // .ForMember(d => d.ReturnNumber, m => m.MapFrom(s => s.CancellationNumber))
                    ;

            // OrderRefund -> OrderRefundResponse
            CreateMap<OrderRefund, OrderRefundResponse>();
            //.IncludeBase<BaseEntity, BaseResponse>();
            //.ForMember(d => d.Order, m => m.Ignore()) // avoid cycles
            //.ForMember(d => d.OrderReturn, m => m.Ignore()) // avoid cycles
            //.ForMember(d => d.OriginalPayment, m => m.Ignore()); // avoid cycles
        }
    }
}


