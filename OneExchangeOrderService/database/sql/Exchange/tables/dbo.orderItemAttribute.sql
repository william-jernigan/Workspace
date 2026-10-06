-- Extends order items with attributes that are not common to all orders
CREATE TABLE [exch_order].[orderItemAttribute] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT IDENTITY(1, 1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,    
    [orderItemId] UNIQUEIDENTIFIER NOT NULL,
    [attributeType] NVARCHAR(50) NOT NULL, -- "CHECK_IN_DATE", "CHECK_OUT_DATE", "NUM_ADULTS", "NUM_CHILDREN", "UNIT_TYPE" and any other booking specific attributes we need to store
    [attributeValue] NVARCHAR(100) NOT NULL, -- VALUE AS STRING
    [_sequence] INT DEFAULT 0 NOT NULL,
    CONSTRAINT [pk_orderItemAttribute] PRIMARY KEY NONCLUSTERED ([_id]),    
    CONSTRAINT [uk_orderItemAttribute] UNIQUE ([orderItemId], [attributeType], [_sequence]),
    --CONSTRAINT [ck_attributeType] CHECK (attributeType in ('CHECK_IN_DATE', 'CHECK_OUT_DATE', 'NUM_ADULTS', 'NUM_CHILDREN', 'UNIT_TYPE')),    
    CONSTRAINT [fk_orderItemAttribute_orderItem] FOREIGN KEY ([orderItemId]) 
        REFERENCES [exch_order].[orderItem] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_orderItemAttribute_clustRowId] ON [exch_order].[orderItemAttribute] ([_clustRowId])
GO
CREATE NONCLUSTERED INDEX [ix_orderItemAttribute_attributeType] ON [exch_order].[orderItemAttribute] ([attributeType])
GO
CREATE NONCLUSTERED INDEX [ix_orderItemAttribute_attributeValue] ON [exch_order].[orderItemAttribute] ([attributeValue])