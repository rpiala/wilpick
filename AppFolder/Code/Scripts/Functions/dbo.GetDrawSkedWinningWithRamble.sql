IF OBJECT_ID('dbo.GetDrawSkedWinningWithRamble', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetDrawSkedWinningWithRamble;
GO


CREATE FUNCTION dbo.GetDrawSkedWinningWithRamble
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
		hdr.rambleWinningPrize,
        dtl.Combination,
        usr.firstName,    
        SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.combination = @firstResult THEN dtl.betAmount ELSE 0 END)  AS FirstTargetTotal,
		SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(@firstResult) THEN dtl.rambleBetAmount ELSE 0 END)  AS FirstRambleTotal,
        SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.combination = @secondResult THEN betAmount ELSE 0 END) AS SecondTargetTotal,
		SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(@secondResult) THEN dtl.rambleBetAmount ELSE 0 END)  AS SecondRambleTotal,
        SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.combination = @thirdResult THEN betAmount ELSE 0 END) AS ThirdTargetTotal,
		SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(@thirdResult) THEN dtl.rambleBetAmount ELSE 0 END)  AS ThirdRambleTotal
    FROM wpBetDetail dtl
        INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
        INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId
    WHERE CAST(dtl.drawDate AS DATE) = @drawDate	
    GROUP BY hdr.userId,hdr.winningPrize,hdr.rambleWinningPrize,hdr.betTicketPrice,dtl.Combination, usr.firstName)
    SELECT 
    userId
    ,firstName PlayerName
    ,SUM(FirstTargetTotal + SecondTargetTotal + ThirdTargetTotal) TotalWinningTargetBet
	,MAX(winningPrize) TargetWinningPrize
	,SUM(FirstRambleTotal + SecondRambleTotal + ThirdRambleTotal) TotalWinningRambleBet    
	,MAX(rambleWinningPrize) RambleWinningPrize
    ,SUM((((FirstTargetTotal + SecondTargetTotal + ThirdTargetTotal)/betTicketPrice) * winningPrize)  
		+ (((FirstRambleTotal + SecondRambleTotal + ThirdRambleTotal)/betTicketPrice) * rambleWinningPrize)) TotalWinningAmount
    from PlayerWinning 
    WHERE FirstTargetTotal > 0 OR FirstRambleTotal > 0 
		OR SecondTargetTotal > 0 OR SecondRambleTotal > 0 
		OR ThirdTargetTotal > 0 OR ThirdRambleTotal > 0
    GROUP BY userId,firstName
);
