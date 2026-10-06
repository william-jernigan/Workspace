CREATE TABLE [exch_profile].[profile] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT IDENTITY(1, 1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [profileType] NVARCHAR(20) DEFAULT 'UNKNOWN' NOT NULL, -- For MVP "PERSON" or "ORGANIZATION"
    [externalSourceSystem] NVARCHAR(50),
    [externalReferenceId] NVARCHAR(50),
    [firstName] NVARCHAR(60),
    [lastName] NVARCHAR(60),
    CONSTRAINT [pk_profile] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [ck_profileType] CHECK (profileType in ('PERSON','ORGANIZATION','GROUP','UNKNOWN'))
)
GO
CREATE CLUSTERED INDEX [ix_profile_clustRowId] ON [exch_profile].[profile] ([_clustRowId])