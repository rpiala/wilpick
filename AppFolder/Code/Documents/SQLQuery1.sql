USE wilpick
select * from wpUserLogTransaction
select * from AspNetUsers
select * from wpBetHeader --where betid = 11
select * from wpBetDetail --where betid = 11

delete from AspNetUsers where UserName = 'agent4@gmail.com'
delete from wpAppUsers where  UserName = 'client@gmail.com'

--insert into dbo.wpappusers(aspNetUserID,agentCode) values('tsetsetestsdfdsf','AAQAA')

select SCOPE_IDENTITY();

update wpAppUsers set betTicketPrice = 1, winningPrize = 10000, rambleWinningPrize = 1000

update wpBetHeader set betTicketPrice = 1, winningPrize = 10000, rambleWinningPrize = 1000

select * from wpAppUsers

update wpAgents set commissionPct = 33

select * from wpAgents
select * from wpSmSettings

delete from wpAppUsers where userid = 6

update wpAppUsers set betType = 'LOAD'

--truncate table wpappusers

--truncate table aspnetuserRoles
--truncate table aspnetuserClaims
--truncate table aspnetUserLogins
--truncate table aspnetUserTokens
--delete from AspNetUsers

select dbo.GetBaseCombination(N'ABCD')

--drop table wpUserLoadTrans
--drop table wpSmSettings
--drop table wpBetDetail
--drop table wpBetHeader
--drop table wpUserLoadTrans
--drop table wpAppUsers
--drop table wpAgents
--drop table wpOwner

truncate table wpOwner

insert into wpAgents values('AZLAN','azlan@gmail.com','Power Agent',10,1)
insert into wpOwner values('owner@gmail.com')

select * from wpAgents
select * from wpOwner

update wpAgents set agentName = 'agent@gmail.com' where agentcode = 'AAQAA'


select COUNT(*) FROM wpAgents where AgentCode = 'AAQAA'

SELECT COUNT(*) FROM wpAgents WHERE AgentCode = 'AABAA'


SELECT COUNT(*) FROM dbo.wpAgents WHERE AgentCode = 'AA OR 1=1';

EXEC spInsertUpdateSmSettings 'CuttOff_Time','11:00:00','Cuttoff time betting is close'
EXEC spInsertUpdateSmSettings 'Start_Time','15:00:00','Betting is open'
EXEC spInsertUpdateSmSettings 'Bet_Limit','15','Bet Limit'
EXEC spInsertUpdateSmSettings 'Bet_Amount','5','Bet Amount'
EXEC spInsertUpdateSmSettings 'Gcash_Load_Receiver','09434331056','Gcash receiver number'
EXEC spInsertUpdateSmSettings 'Power_Agent_Code','AZLAN','Power Agent Code'



select basecombination,betAmount, firstDrawSelected, secondDrawSelected, thirdDrawSelected  
from wpBetDetail 
where basecombination = dbo.GetBaseCombination('QWER') and drawDate = '2026-03-20 11:00:00.000'

SELECT
    combination,
    betAmount,
    LTRIM(
        CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END +
        CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END +
        CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END
    ) AS drawDisplay
FROM wpBetDetail
where betid = 11;


select baseCombination,
 CASE WHEN sum(firstDrawSelected) > 0 THEN SUM(betAmount) ELSE 0 END AS firstDrawTotal,
 CASE WHEN sum(secondDrawSelected) > 0 THEN SUM(betAmount) ELSE 0 END AS secondDrawTotal,
 CASE WHEN sum(thirdDrawSelected) > 0 THEN SUM(betAmount) ELSE 0 END AS thirdDrawTotal
from wpBetDetail 
where drawDate = '2026-03-27 11:00:00.000' 



;WITH firstDrawTotal AS(
	select sum(betAmount) firstTotal from wpBetDetail 
	where basecombination = dbo.GetBaseCombination('QWER') and drawDate = '2026-03-20 11:00:00.000' and firstDrawSelected = 1
),
secondDrawTotal AS(
	select sum(betAmount) secondTotal from wpBetDetail 
	where basecombination = dbo.GetBaseCombination('QWER') and drawDate = '2026-03-20 11:00:00.000' and secondDrawSelected = 1
)
select * FROM firstDrawTotal



SELECT    
    SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal,
    SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal,
    SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal
FROM wpBetDetail
where drawDate = '2026-03-27 11:00:00.000'


;WITH PlayerWinning AS(
SELECT
	hdr.userId,
	hdr.winningPrize,
	hdr.betTicketPrice,
    dtl.Combination,    
    SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(N'MBNA') THEN dtl.betAmount ELSE 0 END)  AS FirstTotal,
    SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(N'OIEJ') THEN betAmount ELSE 0 END) AS SecondTotal,
    SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination(N'KDIL') THEN betAmount ELSE 0 END)  AS ThirdTotal
FROM wpBetDetail dtl
	INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
	INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId
WHERE dtl.drawDate = '2026-03-27 11:00:00.000' 	
GROUP BY hdr.userId,hdr.winningPrize,hdr.betTicketPrice,dtl.Combination)
SELECT 
UserId
,SUM(FirstTotal + SecondTotal + ThirdTotal) TotalWinningBet
,MAX(winningPrize) WinningPrize
,SUM(((FirstTotal + SecondTotal + ThirdTotal)/betTicketPrice) * winningPrize) TotalWinningAmount
from PlayerWinning 
WHERE FirstTotal > 0 OR SecondTotal > 0 OR ThirdTotal > 0
GROUP BY userId

select * from wpAppUsers
select * from wpBetHeader
select * from wpBetDetail

;WITH AgentPlayerBet AS(
SELECT
	agent.userId,	   
	ISNULL(agentA.commissionPct,0) commissionPct,    
    SUM(CASE WHEN FirstDrawSelected = 1 THEN dtl.betAmount ELSE 0 END)  AS FirstTotal,
    SUM(CASE WHEN SecondDrawSelected = 1 THEN dtl.betAmount ELSE 0 END) AS SecondTotal,
    SUM(CASE WHEN ThirdDrawSelected = 1  THEN dtl.betAmount ELSE 0 END)  AS ThirdTotal
FROM wpBetDetail dtl
	INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId	
	INNER JOIN wpAppUsers agent ON agent.agentCode = hdr.agentCode
	INNER JOIN wpAgents agentA ON agentA.userName = agent.userName AND agentA.agentCode = agent.agentCode
WHERE dtl.drawDate = '2026-03-27 11:00:00.000' 	
GROUP BY agent.userId, agentA.commissionPct)
SELECT 
userId, (commissionPct / 100) * SUM(FirstTotal + SecondTotal + ThirdTotal) Commission
,SUM(FirstTotal + SecondTotal + ThirdTotal) TotalBet
from AgentPlayerBet 
GROUP BY userId,commissionPct



SELECT    
    ROW_NUMBER() OVER (ORDER BY baseCombination) AS RowNum,
	baseCombination,	
    SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal,
    SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal,
    SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal,
	SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet
FROM wpBetDetail
where drawDate = '2026-03-27 11:00:00.000' --and betId = 4
group by baseCombination
order by baseCombination


SELECT        
	SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet
	,COUNT(*) AS TotalRows
FROM wpBetDetail
where drawDate = '2026-03-23 11:00:00.000' --and betId = 4


select dbo.GetPermutationsCSV2008('ABCD')

DECLARE @secret VARCHAR(MAX);

-- Encrypt
SET @secret = dbo.EncryptString(N'34');

select @secret

-- Decrypt
SELECT dbo.DecryptString(@secret) AS DecryptedValue;


SELECT *,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),betDetailId)),LTRIM(CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay 
,totalBet =  
CASE 
	WHEN includeRamble = 1 THEN (betAmount * (firstDrawSelected + secondDrawSelected + thirdDrawSelected) * 24)
	ELSE
	(betAmount * (firstDrawSelected + secondDrawSelected + thirdDrawSelected))
	
END 
FROM wpBetDetail WHERE drawDate ='3/30/2026 11:00:00 AM';

SELECT dbo.DecryptString('0x01000000BA1466EE6A41D1F1C61201B2CB7B9FA63E5709EE70CAFE2E');


select * from aspNetRoles
select * from AspNetUserRoles



SELECT
    usr.*,
    CASE 
        WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN 'Owner'
        WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN 'Agent'
        ELSE 'Player'
    END AS accessRole
	,AgentName = 
	CASE
		WHEN EXISTS (SELECT 1 FROM wpAgents WHERE agentCode = usr.agentCode) 
		THEN (SELECT usrA.firstName FROM wpAgents wa INNER JOIN wpAppUsers usrA ON usrA.userName = wa.userName  WHERE wa.agentCode = usr.agentCode)
		ELSE ''
	END
FROM wpAppUsers usr
WHERE usr.userName = 'agent@gmail.com';


SELECT usr.*,
CASE 
	WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN 'Owner' 
	WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN 'Agent' 
	ELSE 'Client' 
END AS accessRole 
FROM dbo.wpAppUsers usr WHERE usr.userName = 'agent@gmail.com';


select * from wpSmSettings

select * from wpUserLoadTrans

--delete from wpUserLoadTrans


;WITH PLAYERTOTALBET AS
(SELECT        
	SUM(((CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) + 
	(CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet
FROM wpBetDetail dtl 
INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
where hdr.userId = 7 AND dtl.betType = 'LOAD')
,PLAYERTOTALLOAD AS(
select 
sum(approvedAmount) TotalLoad 
from wpUserLoadTrans 
where isApproved = 1 and userid = 7
)
SELECT load.TotalLoad - bet.TotalBet AS RemainingLoad 
FROM PLAYERTOTALBET bet, PLAYERTOTALLOAD load


SELECT        
	SUM(((CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) + 
	(CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * betAmount) AS TotalBet
FROM wpBetDetail dtl 
INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
where hdr.userId = 7 AND dtl.betType = 'LOAD'

select 
sum(approvedAmount) 
from wpUserLoadTrans 
where isApproved = 1 and userid = 7

SELECT dbo.GetPlayerRemainingLoad(3)

SELECT * FROM dbo.GetDrawSkedWinningWithRamble('2026-04-06','ADCB','DCBA','DCBA')

SELECT * FROM dbo.GetAgentDrawSkedSales('2026-04-06')

select * From wpDrawResults
select * from wpAgents

select dbo.GetPermutationsCSV2008('ABCD')

select * from wpBetHeader

select * from wpBetDetail
--select dbo.GetBaseCombination('ABCD')
;WITH PlayerWinning AS(
    SELECT
        hdr.userId,
        hdr.winningPrize,
        hdr.betTicketPrice,
		hdr.rambleWinningPrize,
        dtl.Combination,
        usr.firstName,    
        SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.combination = 'ABCD' THEN dtl.betAmount ELSE 0 END)  AS FirstTargetTotal,
		SUM(CASE WHEN FirstDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination('ABCD') AND dtl.combination <> dbo.GetBaseCombination('ABCD') THEN dtl.betAmount ELSE 0 END)  AS FirstRambleTotal,
        SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination('OIEJ') THEN betAmount ELSE 0 END) AS SecondTargetTotal,
		SUM(CASE WHEN SecondDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination('OIEJ') AND dtl.combination <> dbo.GetBaseCombination('ABCD') THEN dtl.betAmount ELSE 0 END)  AS SecondRambleTotal,
        SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination('KDIL') THEN betAmount ELSE 0 END) AS ThirdTargetTotal,
		SUM(CASE WHEN ThirdDrawSelected = 1 AND dtl.baseCombination = dbo.GetBaseCombination('KDIL') AND dtl.combination <> dbo.GetBaseCombination('ABCD') THEN dtl.betAmount ELSE 0 END)  AS ThirdRambleTotal
    FROM wpBetDetail dtl
        INNER JOIN wpBetHeader hdr ON hdr.betId = dtl.betId
        INNER JOIN wpAppUsers usr ON usr.userId = hdr.userId
    WHERE dtl.drawDate = '2026-03-30 11:00'	
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


	SELECT         
		CASE 
			WHEN dtl.includeRamble = 1
			THEN ISNULL(SUM(
				(
                (CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) +
                (CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) +
                (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)
				) * dtl.BetAmount), 0) * 24
			ELSE
				ISNULL(SUM(
				(
                (CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) +
                (CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) +
                (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)
				) * dtl.BetAmount), 0)
			
		END
    FROM wpBetDetail dtl
    INNER JOIN wpBetHeader hdr 
        ON hdr.BetId = dtl.BetId
    WHERE hdr.UserId = 5
      AND dtl.BetType = 'LOAD'
	GROUP BY dtl.includeRamble;


	
SELECT
    ISNULL(SUM(
        (
            (CASE WHEN dtl.FirstDrawSelected = 1 THEN 1 ELSE 0 END) +
            (CASE WHEN dtl.SecondDrawSelected = 1 THEN 1 ELSE 0 END) +
            (CASE WHEN dtl.ThirdDrawSelected = 1 THEN 1 ELSE 0 END)
        )
        * dtl.BetAmount
        * CASE WHEN dtl.IncludeRamble = 1 THEN 24 ELSE 1 END
    ), 0) AS TotalBet
FROM wpBetDetail dtl
INNER JOIN wpBetHeader hdr 
    ON hdr.BetId = dtl.BetId
WHERE hdr.UserId = 5
  AND dtl.BetType = 'LOAD';


SELECT *
,ROW_NUMBER() OVER (ORDER BY betDetailId) AS RowNum
,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),betDetailId))
,LTRIM(CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay
,totalBet = (betAmount + rambleBetAmount) * (firstDrawSelected + secondDrawSelected + thirdDrawSelected)
FROM wpBetDetail 
WHERE betId = '6' AND drawDate ='4/6/2026 11:00:00 AM';


SELECT ROW_NUMBER() OVER (ORDER BY Combination) AS RowNum,Combination
,SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotalBet
,SUM(CASE WHEN FirstDrawSelected = 1 THEN RambleBetAmount ELSE 0 END)  AS FirstTotalRambleBet
, SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotalBet
,SUM(CASE WHEN SecondDrawSelected = 1 THEN RambleBetAmount ELSE 0 END)  AS SecondTotalRambleBet
,SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotalBet 
,SUM(CASE WHEN ThirdDrawSelected = 1 THEN RamblebetAmount ELSE 0 END)  AS ThirdTotalRambleBet
,SUM(((CASE WHEN FirstDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN SecondDrawSelected = 1 THEN 1 ELSE 0 END) + (CASE WHEN ThirdDrawSelected = 1 THEN 1 ELSE 0 END)) * (betAmount + RambleBetAmount)) AS TotalBet 
FROM wpBetDetail 
WHERE drawDate >= '2026-04-06 00:00' AND drawDate < '2026-04-06 23:59' AND Combination LIKE '%' GROUP BY Combination ORDER BY Combination;

select * From co_bet_dtl
WHERE cbd_dtl_no = '0000000000130543'

select * From wpAppUsers

select * from pred_variables

select * from wpOwner

update wpOwner set mobileNumber = NULL where owerid = 2

select * from co_wp_nos where cw_id = 155

--delete from co_wp_nos where cw_id = 155

select * from wpSmSettings

select * from co_all_messages