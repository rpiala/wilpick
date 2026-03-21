
IF OBJECT_ID('dbo.EncryptString', 'FN') IS NOT NULL
    DROP FUNCTION dbo.EncryptString;
GO

CREATE FUNCTION dbo.EncryptString
(
    @PlainText NVARCHAR(MAX)
)
RETURNS VARCHAR(MAX)
AS
BEGIN
    DECLARE @Encrypted VARBINARY(MAX);

    SET @Encrypted = ENCRYPTBYPASSPHRASE(
        N'MyStrongPassphrase',
        @PlainText
    );

    -- Convert VARBINARY to VARCHAR (hex format)
    RETURN CONVERT(VARCHAR(MAX), @Encrypted, 1);
END
GO
