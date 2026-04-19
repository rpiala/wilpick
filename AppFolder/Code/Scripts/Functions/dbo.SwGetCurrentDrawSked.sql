IF OBJECT_ID('dbo.GetNextDrawDateSked', 'FN') IS NOT NULL
    DROP FUNCTION dbo.GetNextDrawDateSked;
GO


CREATE FUNCTION dbo.GetNextDrawDateSked
(
    @currentDateTime DATETIME
)
RETURNS DATETIME
AS
BEGIN
    DECLARE @date DATE = CAST(@currentDateTime AS DATE);
    DECLARE @time TIME = CAST(@currentDateTime AS TIME);

    DECLARE @result DATETIME;

    IF (@time <= '14:00:00')
        SET @result = DATEADD(HOUR, 14, CAST(@date AS DATETIME));

    ELSE IF (@time <= '17:00:00')
        SET @result = DATEADD(HOUR, 17, CAST(@date AS DATETIME));

    ELSE IF (@time <= '21:00:00')
        SET @result = DATEADD(HOUR, 21, CAST(@date AS DATETIME));

    ELSE
        SET @result = DATEADD(HOUR, 14, DATEADD(DAY, 1, CAST(@date AS DATETIME)));

    RETURN @result;
END;
GO