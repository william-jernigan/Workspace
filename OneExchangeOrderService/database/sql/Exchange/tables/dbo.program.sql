-- Trading agreement between partners
CREATE TABLE [exchange].[program] (
    [_id] UNIQUEIDENTIFIER DEFAULT newId() NOT NULL,
    [_clustRowId] BIGINT UNIQUE IDENTITY(1,1) NOT NULL,
    [_insertedDateTime] DATETIMEOFFSET DEFAULT getDate() NOT NULL,
    [_insertedBy] NVARCHAR(50) NOT NULL,
    [_updatedDateTime] DATETIMEOFFSET,
    [_updatedBy] NVARCHAR(50),
    [_status] BIT DEFAULT 1 NOT NULL,
    [_statusReason] NVARCHAR(100),
    [tenantId] UNIQUEIDENTIFIER NOT NULL,
    [programCode] NVARCHAR(10) NOT NULL, -- Business Code for Program (e.g. DEX)    
    [programName] NVARCHAR(50) NOT NULL, -- Program Name (e.g. "Direct Exchange")
    CONSTRAINT [pk_program] PRIMARY KEY NONCLUSTERED ([_id]),
    CONSTRAINT [uk_program_code] UNIQUE ([tenantId], [programCode]),    
    CONSTRAINT [fk_program_tenant] FOREIGN KEY ([tenantId]) 
        REFERENCES [exchange].[tenant] ([_id])
)
GO
CREATE CLUSTERED INDEX [ix_program_clustRowId] ON [exchange].[program] ([_clustRowId])