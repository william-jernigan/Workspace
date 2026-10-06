-- Captures all point and fiat based transactions
-- This table should have as many rows as the total number of rows in OrderPayment and OrderRefund tables for a given order
CREATE TABLE [exch_order].[exchangeTransactionLog] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,
    [orderId] UNIQUEIDENTIFIER NOT NULL,
    [transactionDate] DATETIMEOFFSET NOT NULL,
    [transactionType] NVARCHAR(20) NOT NULL,
    [fromAccountId] NVARCHAR(100), -- BG Member Id (if this is a purchase transaction, otherwise from and to are reversed)
    [fromCurrency] NVARCHAR(10), -- BG Points
    [fromTransactionAmount] DECIMAL(19,4) NOT NULL,
    [fromConfirmationNumber] NVARCHAR(100),
    [toAccountId] NVARCHAR(100), -- Choice Member Id
    [toCurrency] NVARCHAR(10), -- Choice Points
    [toTransactionAmount] DECIMAL(19,4) NOT NULL,
    [toConfirmationNumber] NVARCHAR(100),    
    [transactionReferenceId] UNIQUEIDENTIFIER,
    [memo] NVARCHAR(500),
    [ValidFrom] DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL
    CONSTRAINT DF_exchangeTransactionLog_ValidFrom DEFAULT SYSUTCDATETIME(),
    [ValidTo] DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL
    CONSTRAINT DF_exchangeTransactionLog_ValidTo DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),
    CONSTRAINT [pk_exchangeTransactionLog] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [ck_transactionType] CHECK (transactionType in ('PURCHASE_BY_POINTS', 'ORDER_FEE', 'CANCELLATION_FEE', 'POINTS_REFUND', 'FEE_REFUND', 'FEE_REIMBURSEMENT')),
    CONSTRAINT [fk_exchangeTransactionLog_order] FOREIGN KEY ([orderId]) 
        REFERENCES [exch_order].[order] ([_id])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [exch_order].[exchangeTransactionLog_history]));
GO
CREATE CLUSTERED INDEX [ix_exchangeTransactionLog_clustRowId] ON [exch_order].[exchangeTransactionLog] ([_clustRowId])
GO 
CREATE NONCLUSTERED INDEX [ix_exchangeTransactionLog_orderId] ON [exch_order].[exchangeTransactionLog] ([orderId])
GO
CREATE NONCLUSTERED INDEX [ix_exchangeTransactionLog_transactionType] ON [exch_order].[exchangeTransactionLog] ([transactionType])