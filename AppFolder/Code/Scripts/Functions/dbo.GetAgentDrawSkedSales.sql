IF OBJECT_ID('dbo.GetAgentDrawSkedSales', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetAgentDrawSkedSales;
GO


CREATE FUNCTION dbo.GetAgentDrawSkedSales
(
    @drawDate datetime
)
RETURNS TABLE
AS
RETURN
(
    
    WITH AgentPlayerBet AS(
    SELECT
        agent.userId,
		agent.firstName,	   
        ISNULL(agentA.commissionPct,0) commissionPct,    
        SUM(CASE WHEN FirstDrawSelected = 1 THEN (dtl.betAmount + dtl.rambleBetAmount) ELSE 0 END)  AS FirstTotal,
        SUM(CASE WHEN SecondDrawSelected = 1 THEN (dtl.betAmount + dtl.rambleBetAmount) ELSE 0 END) AS SecondTotal,
        SUM(CASE WHEN ThirdDrawSelected = 1  THEN (dtl.betAmount + dtl.rambleBetAmount) ELSE 0 END)  AS ThirdTotal
    FROM wpBetDetail dtl
        INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId	
        INNER JOIN wpAppUsers agent ON agent.agentCode = hdr.agentCode
        INNER JOIN wpAgents agentA ON agentA.userName = agent.userName AND agentA.agentCode = agent.agentCode
    WHERE CAST(dtl.drawDate AS DATE) = @drawDate 	
    GROUP BY agent.userId, agentA.commissionPct,agent.firstName)
    SELECT 
    userId,firstName AgentName, (commissionPct / 100) * SUM(FirstTotal + SecondTotal + ThirdTotal) Commission
    ,SUM(FirstTotal + SecondTotal + ThirdTotal) TotalBet
    from AgentPlayerBet 
    GROUP BY userId,firstName,commissionPct
);