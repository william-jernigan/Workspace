-- Partners of a Tenant
CREATE TABLE [exchange].[partner] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,
    [organizationId] UNIQUEIDENTIFIER NOT NULL,
    [partnerCode] NVARCHAR(50) NOT NULL, -- Business Code for Partner (e.g. CHO, GPV)    
    [defaultPointCurrencyCode] NVARCHAR(10), -- Currency code for partner points
    CONSTRAINT [pk_partner] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_partner_code] UNIQUE ([tenantId], [partnerCode]),    
    CONSTRAINT [fk_partner_tenant] FOREIGN KEY ([tenantId]) 
        REFERENCES [exchange].[tenant] ([_id]),
    CONSTRAINT [fk_partner_organization] FOREIGN KEY ([organizationId]) 
        REFERENCES [exchange].[organization] ([_id]),
    CONSTRAINT [uk_partner_tenant_organization] UNIQUE ([tenantId], [organizationId])
)
GO
CREATE CLUSTERED INDEX [ix_partner_clustRowId] ON [exchange].[partner] ([_clustRowId])