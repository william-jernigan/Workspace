-- For future extensibility, For the MVP there will always be a single orderItem
CREATE TABLE [exch_order].[orderItem] (
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
    [itemId] UNIQUEIDENTIFIER NOT NULL, -- This is the GUID of the Unit/UnitType booked 
    [itemCode] NVARCHAR(100) NOT NULL, -- This is the business code of the Unit/Unit Type booked
    [_sequence] INT DEFAULT 0 NOT NULL, -- Always 1 for MVP
    [itemDescription] NVARCHAR(200) NOT NULL, -- Name of the Property
    [quantity] DECIMAL(19,4) NOT NULL, -- Always 1 for MVP
    [unitPriceCurrency] NVARCHAR(10), -- Same as sub total in Order
    [unitPrice] DECIMAL(19,4) NOT NULL, -- Same as sub total in Order
    [taxCurrency] NVARCHAR(10) NOT NULL, -- Currency can be a FIAT currenct like USD, EUR or a partner point currenty like CHOP, or BGVP etc.     
    [taxAmount] DECIMAL(19,4) NOT NULL, -- Always 0 for MVP
    [memo] NVARCHAR(500),
    [extendedReference] NVARCHAR(2000),
    CONSTRAINT [pk_orderItem] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_orderItem_sequence] UNIQUE ([orderId], [_sequence]),
    CONSTRAINT [ck_orderItem_extendedReference] CHECK (ISJSON(extendedReference) = 1),
    CONSTRAINT [fk_orderItem_order] FOREIGN KEY ([orderId]) 
        REFERENCES [exch_order].[order] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_orderItem_clustRowId] ON [exch_order].[orderItem] ([_clustRowId])