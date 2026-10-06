-- Digital address (e.g. email, website URL, or social media address) for persons or organizations
-- Digital address (e.g. email, website URL, or social media address) for persons or organizations
CREATE TABLE [exch_profile].[digitalAddress] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [profileId] UNIQUEIDENTIFIER NOT NULL,
    [_sequence] INT NOT NULL,
    [digitalAddressType] NVARCHAR(50),
    [digitalAddress] NVARCHAR(100),
    [verifiedDate] DATETIME,
    [notForMarketing] BIT DEFAULT 0 NOT NULL,
    CONSTRAINT [pk_digitalAddress] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_digitalAddress] UNIQUE ([profileId], [_sequence]),
    CONSTRAINT [ck_digitalAddress_type] CHECK (digitalAddressType in ('EMAIL', 'WEBSITE', 'SOCIAL_MEDIA')),
    CONSTRAINT [fk_digitalAddress_profile] FOREIGN KEY ([profileId]) 
        REFERENCES [exch_profile].[profile] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_digitalAddress_clustRowId] ON [exch_profile].[digitalAddress] ([_clustRowId])

