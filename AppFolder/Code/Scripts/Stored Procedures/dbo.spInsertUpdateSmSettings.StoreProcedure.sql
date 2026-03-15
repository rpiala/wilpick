IF OBJECT_ID('[dbo].[spInsertUpdateSmSettings]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[spInsertUpdateSmSettings];
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
CREATE PROCEDURE [dbo].[spInsertUpdateSmSettings]	
    @VarKey NVARCHAR(100),
    @VarValue NVARCHAR(255),
    @VarRemarks NVARCHAR(255)   
AS
BEGIN
    BEGIN TRY        
        IF EXISTS(SELECT * FROM wpSmSettings WHERE VarName = @VarKey)
            BEGIN
                UPDATE wpSmSettings SET VarValue = @VarValue, Description = @VarRemarks WHERE VarName = @VarKey
            END
        ELSE
            BEGIN
                INSERT INTO wpSmSettings(VarName,VarValue,Description) VALUES(@VarKey,@VarValue,@VarRemarks)
            END
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);        
    END CATCH
END
GO