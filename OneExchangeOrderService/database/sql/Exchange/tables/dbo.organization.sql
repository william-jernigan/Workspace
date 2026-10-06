
-- Organizations
CREATE TABLE [exchange].[organization] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [profileId] UNIQUEIDENTIFIER NOT NULL,
    [organizationName] NVARCHAR(50),
    [organizationSuffix] NVARCHAR(10),
    CONSTRAINT [pk_organization] PRIMARY KEY NONCLUSTERED ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_organization_clustRowId] ON [exchange].[organization] ([_clustRowId])