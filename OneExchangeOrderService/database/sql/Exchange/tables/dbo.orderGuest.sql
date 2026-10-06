-- Stores guests for an order
CREATE TABLE [exch_order].[orderGuest] (
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
    [profileId] UNIQUEIDENTIFIER NOT NULL,
    [isPrimary] BIT NOT NULL,
    CONSTRAINT [pk_orderGuest] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [fk_orderGuest_order] FOREIGN KEY ([orderId]) 
        REFERENCES [exch_order].[order] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_orderGuest_clustRowId] ON [exch_order].[orderGuest] ([_clustRowId])
GO
CREATE NONCLUSTERED INDEX [ix_orderGuest_orderId] ON [exch_order].[orderGuest] ([orderId])