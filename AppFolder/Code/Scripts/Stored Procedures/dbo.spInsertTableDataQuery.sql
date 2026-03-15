IF OBJECT_ID('[dbo].[spInsertTableDataQuery]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[spInsertTableDataQuery];
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ===============================================================================================================================
-- Author:		Roselito Piala
-- Create date: 30 March 2025
-- Description:	Get table data with optional pagination
-- ===============================================================================================================================
CREATE PROCEDURE [dbo].[spInsertTableDataQuery]	
    @sqlQuery NVARCHAR(MAX),   
    @newId decimal(38,0) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY        
        
        DECLARE @stmt nvarchar(max) = @sqlQuery + N';
            SELECT @newId = SCOPE_IDENTITY();';

        EXEC sp_executesql
            @stmt,
            N'@newId decimal(38,0) OUTPUT',
            @newId = @newId OUTPUT;

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);        
    END CATCH
END
GO