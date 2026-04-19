IF OBJECT_ID('dbo.GetPlayerRemainingLoad', 'FN') IS NOT NULL
    DROP FUNCTION dbo.GetPlayerRemainingLoad;
GO

CREATE FUNCTION dbo.GetPlayerRemainingLoad
(
    @UserId DECIMAL(18, 0)
)
RETURNS DECIMAL(18, 2)
AS
BEGIN
    DECLARE @TotalBet  DECIMAL(18, 2) = 0;
    DECLARE @TotalLoad DECIMAL(18, 2) = 0;
    DECLARE @TotalOut DECIMAL(18, 2) = 0;
    DECLARE @TotalSwBet  DECIMAL(18, 2) = 0;

    -- Total Bet
    SELECT 
        @TotalBet = 
		ISNULL(SUM(
        (
            (CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) +
            (CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) +
            (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)
        )
        * (dtl.BetAmount + dtl.RambleBetAmount)         
    ), 0)
    FROM wpBetDetail dtl
    INNER JOIN wpBetHeader hdr 
        ON hdr.BetId = dtl.BetId
    WHERE hdr.UserId = @UserId
      AND dtl.BetType = 'LOAD'	

    SELECT 
        @TotalSwBet = 
		ISNULL(SUM(dtl.ramble + dtl.target), 0)    
	FROM wpAppUsers usr
	INNER JOIN co_wp_nos cwn ON cwn.fb_id = usr.email
	INNER JOIN co_valid_message cvm ON cvm.cwn_id = cwn.cwn_id AND cvm.co_id = cwn.co_id AND cvm.cw_id = cwn.cw_id AND cvm.wp_id = cwn.wp_id
	INNER JOIN co_bet_dtl dtl ON dtl.cvm_no = cvm.cvm_no	
    WHERE usr.UserId = @UserId
      AND dtl.bet_type = 'LOAD'  

    -- Total Approved Load
    SELECT 
        @TotalLoad = ISNULL(SUM(ApprovedAmount), 0)
    FROM wpUserLoadTrans
    WHERE IsApproved = 1
      AND UserId = @UserId;

    SELECT  
         @TotalOut = ISNULL(SUM(cashOutAmount), 0)
    FROM wpCashOutTransactions
    WHERE UserId = @UserId AND isDeleted = 0;

    RETURN @TotalLoad - @TotalBet - @TotalOut - @TotalSwBet;
END;
GO
