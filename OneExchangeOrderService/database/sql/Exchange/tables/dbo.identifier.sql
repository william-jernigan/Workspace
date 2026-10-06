-- External identifiers for for persons (e.g. member Id) or organizations (e.g. tax Id)
CREATE TABLE [exch_profile].[identifier] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getdate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [profileId] UNIQUEIDENTIFIER NOT NULL,
    [_sequence] INT NOT NULL,
    [identifierType] NVARCHAR(50),
    [identifierSource] NVARCHAR(50),
    [identifier] NVARCHAR(50),
    [identifierEncryption] NVARCHAR(50),
    [encryptedIdentifier] VARBINARY(MAX),
    CONSTRAINT [pk_identifier] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_identifier] UNIQUE ([profileId], [identifierType]),
    CONSTRAINT [ck_identifier_type] CHECK (identifierType in ('TAX_ID', 'BUSINESS_LICENSE', 'PASSPORT', 'MEMBER_ID')),
    CONSTRAINT [fk_identifier_profile] FOREIGN KEY ([profileId]) 
        REFERENCES [exch_profile].[profile] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_identifier_clustRowId] ON [exch_profile].[identifier] ([_clustRowId])