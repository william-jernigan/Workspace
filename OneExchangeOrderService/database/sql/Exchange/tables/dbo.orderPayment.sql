CREATE TABLE [exch_order].[orderPayment] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT IDENTITY(1, 1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,    
    [orderId] UNIQUEIDENTIFIER NOT NULL,
    [paymentPurpose] NVARCHAR(20) NOT NULL, -- "PURCHASE_BY_POINTS", "ORDER_FEE", "CANCELLATION_FEE"
    [currency] NVARCHAR(10),
    [amount] DECIMAL(19,4) NOT NULL,
    [paymentDate] DATETIME NOT NULL,
    [paymentAccountToken] NVARCHAR(200), -- Token provided by NGPG and used to charge the card
    CONSTRAINT [pk_orderPayment] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [fk_orderPayment_order] FOREIGN KEY ([orderId]) 
        REFERENCES [exch_order].[order] ([_id]),
    CONSTRAINT [ck_paymentPurpose] CHECK (paymentPurpose in ('PURCHASE_BY_POINTS', 'ORDER_FEE', 'CANCELLATION_FEE'))
)
GO
CREATE CLUSTERED INDEX [ix_orderPayment_clustRowId] ON [exch_order].[orderPayment] ([_clustRowId])
GO
CREATE NONCLUSTERED INDEX [ix_orderPayment_orderId] ON [exch_order].[orderPayment] ([orderId])
GO
CREATE NONCLUSTERED INDEX [ix_orderPayment_paymentPurpose] ON [exch_order].[orderPayment] ([paymentPurpose])