-- Trading agreement between partners
CREATE TABLE [exchange].[tradingAgreement] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,
    [programId] UNIQUEIDENTIFIER  NULL,    
    [supplierPartnerId] UNIQUEIDENTIFIER NOT NULL,
    [buyerPartnerId] UNIQUEIDENTIFIER NOT NULL,
    [tradingAgreementCode] NVARCHAR(50) NOT NULL, -- Business code e.g. BGV-BGV-CHO 
    [effectiveStartDate] DATETIMEOFFSET NOT NULL,
    [effectiveEndDate] DATETIMEOFFSET,
    CONSTRAINT [pk_tradingAgreement] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_tradingAgreement_code] UNIQUE ([tenantId], [tradingAgreementCode]),
    CONSTRAINT [fk_tradingAgreement_tenant] FOREIGN KEY ([tenantId]) 
        REFERENCES [exchange].[tenant] ([_id]),
         
    CONSTRAINT [fk_tradingAgreement_supplier] FOREIGN KEY ([supplierPartnerId]) 
        REFERENCES [exchange].[partner] ([_id]),
    CONSTRAINT [fk_tradingAgreement_buyer] FOREIGN KEY ([buyerPartnerId]) 
        REFERENCES [exchange].[partner] ([_id]),
    CONSTRAINT [uk_tradingAgreement] UNIQUE ([tenantId], [supplierPartnerId], [buyerPartnerId])
)
GO
CREATE CLUSTERED INDEX [ix_tradingAgreement_clustRowId] ON [exchange].[tradingAgreement] ([_clustRowId])