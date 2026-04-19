IF OBJECT_ID('dbo.GetPlayerCashFlowByUserId', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetPlayerCashFlowByUserId;
GO

CREATE FUNCTION dbo.GetPlayerCashFlowByUserId
(
    @UserId DECIMAL(18, 0)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        
        TotalCashIn =
        (
            SELECT ISNULL(SUM(ApprovedAmount), 0)
            FROM wpUserLoadTrans
            WHERE UserId = @UserId
              AND IsApproved = 1
        ),

        
        TotalBet =
        (
            SELECT ISNULL(SUM(
                (
                    (CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) +
                    (CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) +
                    (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)
                )
                * dtl.BetAmount
                * CASE WHEN dtl.IncludeRamble = 1 THEN 24 ELSE 1 END
            ), 0)
            FROM wpBetDetail dtl
            INNER JOIN wpBetHeader hdr
                ON hdr.BetId = dtl.BetId
            WHERE hdr.UserId = @UserId
        ) + 
		(            
            SELECT ISNULL(SUM(dtl.ramble + dtl.target), 0)    
            FROM wpAppUsers usr
            INNER JOIN co_wp_nos cwn ON cwn.fb_id = usr.email
            INNER JOIN co_valid_message cvm ON cvm.cwn_id = cwn.cwn_id AND cvm.co_id = cwn.co_id AND cvm.cw_id = cwn.cw_id AND cvm.wp_id = cwn.wp_id
            INNER JOIN co_bet_dtl dtl ON dtl.cvm_no = cvm.cvm_no	
            WHERE usr.UserId = @UserId
            AND dtl.bet_type = 'LOAD'  
        ),
        
        TotalCashOut =
        (
            SELECT ISNULL(SUM(cashOutAmount), 0)
            FROM wpCashOutTransactions
            WHERE UserId = @UserId AND isDeleted = 0             
        ),

        TotalSwBet = 
        (            
            SELECT ISNULL(SUM(dtl.ramble + dtl.target), 0)    
            FROM wpAppUsers usr
            INNER JOIN co_wp_nos cwn ON cwn.fb_id = usr.email
            INNER JOIN co_valid_message cvm ON cvm.cwn_id = cwn.cwn_id AND cvm.co_id = cwn.co_id AND cvm.cw_id = cwn.cw_id AND cvm.wp_id = cwn.wp_id
            INNER JOIN co_bet_dtl dtl ON dtl.cvm_no = cvm.cvm_no	
            WHERE usr.UserId = @UserId
            AND dtl.bet_type = 'LOAD'  
        )        
);
GO