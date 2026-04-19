IF OBJECT_ID('dbo.GetSwDrawSkedWinning', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetSwDrawSkedWinning;
GO

CREATE FUNCTION dbo.GetSwDrawSkedWinning
(
    @drawSked datetime,
    @resultCombination NVARCHAR(6)    
)
RETURNS TABLE
AS
RETURN
(
    
    WITH PlayerWinning AS(
    SELECT
        usr.userId,
		usr.firstName,        
        MAX(dtl.cbd_msg) AS cbd_msg,
        MAX(dtl.aser_prize) AS aser_prize,
		MAX(dtl.divider) AS divider,
		SUM(CASE WHEN dtl.target > 0 AND dtl.cbd_msg = @resultCombination THEN dtl.target ELSE 0 END) AS TargetHits,
		SUM(CASE WHEN dtl.ramble > 0 AND dbo.GetBaseCombination(dtl.cbd_msg) = dbo.GetBaseCombination(@resultCombination) 
			THEN dtl.ramble ELSE 0 END) AS RambleHits          
    FROM wpAppUsers usr
		INNER JOIN co_wp_nos cwn ON cwn.fb_id = usr.email
		INNER JOIN co_valid_message cvm ON cvm.cwn_id = cwn.cwn_id AND cvm.co_id = cwn.co_id AND cvm.cw_id = cwn.cw_id AND cvm.wp_id = cwn.wp_id
		INNER JOIN co_bet_dtl dtl ON dtl.cvm_no = cvm.cvm_no
    WHERE dtl.draw_sked = @drawSked 
	GROUP BY usr.userId,usr.firstName	
   )

    
	SELECT 
        userId,
        firstName AS PlayerName,
		MAX(divider) AS divider,
        MAX(cbd_msg) AS cbd_msg,
        MAX(aser_prize) AS aser_prize,
        SUM(TargetHits) AS TotalTargetHits,
        SUM(RambleHits) AS TotalRambleHits,
		SUM((aser_prize / 10) * TargetHits) AS TargetAmount,
		SUM(ROUND((aser_prize / 10) * (RambleHits / divider),0)) AS RambleAmount
    FROM PlayerWinning
    WHERE TargetHits > 0 OR RambleHits > 0
    GROUP BY userId, firstName
	
);
