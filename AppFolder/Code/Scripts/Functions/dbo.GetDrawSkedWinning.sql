IF OBJECT_ID('dbo.GetDrawSkedWinning', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetDrawSkedWinning;
GO


CREATE FUNCTION dbo.GetDrawSkedWinning
(
    @drawDate datetime,
    @firstResult NVARCHAR(6),
    @secondResult NVARCHAR(6),
    @thirdResult NVARCHAR(6)
)
RETURNS TABLE
AS
RETURN
(
    
    WITH PlayerWinning AS(
    SELECT
        hdr.userId,
        hdr.winningPrize,
        hdr.betTicketPrice,		
        dtl.Combination,
        usr.firstName,    
        SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.combination = @firstResult THEN dtl.betAmount ELSE 0 END)  AS FirstTargetTotal,		
        SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.combination = @secondResult THEN betAmount ELSE 0 END) AS SecondTargetTotal,		
        SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.combination = @thirdResult THEN betAmount ELSE 0 END) AS ThirdTargetTotal		
    FROM wpBetDetail dtl
        INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
        INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId
    WHERE dtl.drawDate = @drawDate	
    GROUP BY hdr.userId,hdr.winningPrize,hdr.betTicketPrice,dtl.Combination, usr.firstName)
    SELECT 
    userId
    ,firstName PlayerName
    ,SUM(FirstTargetTotal + SecondTargetTotal + ThirdTargetTotal) TotalWinningTargetBet
	,MAX(winningPrize) TargetWinningPrize		
    ,SUM(((FirstTargetTotal + SecondTargetTotal + ThirdTargetTotal)/betTicketPrice) * winningPrize) TotalWinningAmount
    from PlayerWinning 
    WHERE FirstTargetTotal > 0  
		OR SecondTargetTotal > 0 
		OR ThirdTargetTotal > 0 
    GROUP BY userId,firstName
);
