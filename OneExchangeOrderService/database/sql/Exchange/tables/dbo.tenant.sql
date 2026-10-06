-- Tenants
CREATE TABLE [exchange].[tenant] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [organizationId] UNIQUEIDENTIFIER NOT NULL,
    [tenantCode] NVARCHAR(50) NOT NULL, -- Business Code (e.g. BGV)
    [contractStartDate] DATETIMEOFFSET NOT NULL,
    [contractEndDate] DATETIMEOFFSET,
    CONSTRAINT [pk_tenant] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [fk_tenant_organization] FOREIGN KEY ([organizationId]) 
        REFERENCES [exchange].[organization] ([_id]),
    CONSTRAINT [uk_tenant_code] UNIQUE ([tenantCode])
)
GO
CREATE CLUSTERED INDEX [ix_tenant_clustRowId] ON [exchange].[tenant] ([_clustRowId])
