IF OBJECT_ID('dbo.GetAgentSwDrawSkedSales', 'IF') IS NOT NULL
    DROP FUNCTION dbo.GetAgentSwDrawSkedSales;
GO


CREATE FUNCTION dbo.GetAgentSwDrawSkedSales
(
    @drawSked datetime
)
RETURNS TABLE
AS
RETURN
(        
    SELECT
        AgentInfo.userId AS UserId,
		AgentInfo.firstName AS AgentName,		   
        MAX(AgentCwn.commission) AS Commission,
		SUM(ISNULL(dtl.target,0) + ISNULL(dtl.ramble,0)) AS TotalBet				        
    FROM wpAppUsers usr		
		INNER JOIN co_wp_nos cwn ON cwn.fb_id = usr.email
		INNER JOIN co_valid_message cvm ON cvm.cwn_id = cwn.cwn_id AND cvm.co_id = cwn.co_id AND cvm.cw_id = cwn.cw_id AND cvm.wp_id = cwn.wp_id
		INNER JOIN co_bet_dtl dtl ON dtl.cvm_no = cvm.cvm_no
		INNER JOIN wpAgents agentA ON agentA.agentCode = usr.agentCode
		INNER JOIN wpAppUsers AgentInfo ON AgentInfo.userName = agentA.userName AND AgentInfo.agentCode = agentA.agentCode
        INNER JOIN co_wp_nos AgentCwn ON AgentCwn.fb_id = AgentInfo.email
    WHERE dtl.draw_sked = @drawSked
	GROUP BY AgentInfo.userId,AgentInfo.firstName   
);