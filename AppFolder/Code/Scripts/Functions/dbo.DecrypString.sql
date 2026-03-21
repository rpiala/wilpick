IF OBJECT_ID('dbo.DecryptString', 'FN') IS NOT NULL
    DROP FUNCTION dbo.DecryptString;
GO

CREATE FUNCTION dbo.DecryptString
(
    @CipherText VARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    RETURN CONVERT(
        NVARCHAR(MAX),
        DECRYPTBYPASSPHRASE(
            N'MyStrongPassphrase',
            CONVERT(VARBINARY(MAX), @CipherText, 1)
        )
    );
END

GO