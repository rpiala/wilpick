IF OBJECT_ID('[dbo].[spGetTableDataQuery]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[spGetTableDataQuery];
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
CREATE PROCEDURE [dbo].[spGetTableDataQuery]	
    @sqlQuery NVARCHAR(MAX)   
AS
BEGIN
    BEGIN TRY        
        EXEC sp_executesql @sqlQuery;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);        
    END CATCH
END
GO