-- Address for persons or organizations
-- Address for persons or organizations
CREATE TABLE [exch_profile].[address] (
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
    [addressType] NVARCHAR(20),
    [addressPurpose] NVARCHAR(50),
    [line1] NVARCHAR(100),
    [line2] NVARCHAR(100),
    [line3] NVARCHAR(100),
    [city] NVARCHAR(50),
    [county] NVARCHAR(50),
    [state] NVARCHAR(20),
    [province] NVARCHAR(20),
    [postalcode] NVARCHAR(20),
    [country] NVARCHAR(20),
    [verifiedDate] DATETIME,
    [postalServiceVerified] BIT DEFAULT 0 NOT NULL,
    [extendedReference] NVARCHAR(2000),
    [notForMarketing] BIT DEFAULT 0 NOT NULL,
    CONSTRAINT [pk_address] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_address] UNIQUE ([profileId], [_sequence]),
    CONSTRAINT [ck_address_extendedReference] CHECK (ISJSON(extendedReference) = 1),
    CONSTRAINT [ck_address_type] CHECK (addressType in ('BILLING', 'SHIPPING', 'BUSINESS', 'HOME')),
    CONSTRAINT [ck_address_purpose] CHECK (addressPurpose in ('PRIMARY', 'SECONDARY', 'SEASONAL')),
    CONSTRAINT [fk_address_profile] FOREIGN KEY ([profileId]) 
        REFERENCES [exch_profile].[profile] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_address_clustRowId] ON [exch_profile].[address] ([_clustRowId])

