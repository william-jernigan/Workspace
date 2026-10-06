CREATE TABLE [exch_profile].[phone] (
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
    [phoneType] NVARCHAR(20),
    [phonePurpose] NVARCHAR(20),
    [number] NVARCHAR(30),
    [extension] NVARCHAR(20),
    [verifiedDate] DATETIME,
    [notForMarketing] BIT DEFAULT 0 NOT NULL,
    CONSTRAINT [pk_phone] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_phone] UNIQUE ([profileId], [_sequence]),
    CONSTRAINT [ck_phone_type] CHECK (phoneType in ('MOBILE', 'BUSINESS', 'HOME', 'FAX')),
    CONSTRAINT [ck_phone_purpose] CHECK (phonePurpose in ('PRIMARY', 'EMERGENCY', 'BILLING')),
    CONSTRAINT [fk_phone_profile] FOREIGN KEY ([profileId]) 
        REFERENCES [exch_profile].[profile] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_phone_clustRowId] ON [exch_profile].[phone] ([_clustRowId])
GO