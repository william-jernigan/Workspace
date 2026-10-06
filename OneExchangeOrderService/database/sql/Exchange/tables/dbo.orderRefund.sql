-- So this table can have a combination of the below: 
-- 1) POINTS_REFUND for the original points transaction - if the order is cancelled and Inrule says points need to be refunded
-- 2) FEE_REFUND for the original order - if the order is cancelled and Inrule says fee needs to be refunded
-- 3) FEE_REIMBURSEMENT (order) - If the order is NOT cancelled, and the order fee has not been reimbursed before, and customer is now asking for a reimbursement
-- 4) FEE_REIMBURSEMENT (cancellation) - If the order is cancelled, and a fee was paid, and it has not been reimbursed before, and customer is now asking for a reimbursement
CREATE TABLE [exch_order].[orderRefund] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT IDENTITY(1, 1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
   [orderId] UNIQUEIDENTIFIER NOT NULL,  
    [tenantId] UNIQUEIDENTIFIER NOT NULL,
    [orderReturnId] UNIQUEIDENTIFIER, -- Order Return  Id can be null if this only a FEE_REIMBURSEMENT without cancelling the whole order
    [refundPurpose] NVARCHAR(20) NOT NULL, -- "POINTS_REFUND", "FEE_REFUND", "FEE_REIMBURSEMENT".
    [originalPaymentId] UNIQUEIDENTIFIER NOT NULL, -- Original payment Id that is being refunded
    [salesPersonEmail] NVARCHAR(255), -- Email address of the sales person
    [salesPersonFirstName] NVARCHAR(255), -- Full name of the sales person, to be displayed in reservation details
    [salesPersonLastName] NVARCHAR(255), -- Full name of the sales person, to be displayed in reservation details
    [currency] NVARCHAR(10),
    [amount] DECIMAL(19,4) NOT NULL,
    [refundDate] DATETIME,
    [refundAccountToken] NVARCHAR(200), -- NGPG Payment token for the refund, in most cases will be same as original payment
    CONSTRAINT [pk_orderRefund] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [ck_refundPurpose] CHECK (refundPurpose in ('POINTS_REFUND', 'FEE_REFUND', 'FEE_REIMBURSEMENT')),
    CONSTRAINT [fk_orderRefund_orderReturn] FOREIGN KEY ([orderReturnId]) 
        REFERENCES [exch_order].[orderReturn] ([_id]),
    CONSTRAINT [fk_orderRefund_originalPayment] FOREIGN KEY ([originalPaymentId]) 
        REFERENCES [exch_order].[orderPayment] ([_id]),
    CONSTRAINT [fk_orderRefund_order] FOREIGN KEY ([orderId]) 
        REFERENCES [exch_order].[order] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_orderRefund_clustRowId] ON [exch_order].[orderRefund] ([_clustRowId])
GO
CREATE NONCLUSTERED INDEX [ix_orderRefund_refundPurpose] ON [exch_order].[orderRefund] ([refundPurpose])

