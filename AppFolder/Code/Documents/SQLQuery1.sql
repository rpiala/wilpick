USE wilpick
select * from wpUserLogTransaction
select * from AspNetUsers
select * from wpBetHeader --where betid = 11
select * from wpBetDetail --where betid = 11

--insert into dbo.wpappusers(aspNetUserID,agentCode) values('tsetsetestsdfdsf','AAQAA')

select SCOPE_IDENTITY();

select * from wpAppUsers

delete from wpAppUsers where userid = 6

update wpAppUsers set betTicketPrice = 5, winningPrize = 25000

--truncate table wpappusers

--truncate table aspnetuserRoles
--truncate table aspnetuserClaims
--truncate table aspnetUserLogins
--truncate table aspnetUserTokens
--delete from AspNetUsers

select dbo.GetBaseCombination(N'ABCD')


--drop table wpSmSettings
--drop table wpBetDetail
--drop table wpBetHeader
--drop table wpUserLoadTrans
--drop table wpAppUsers
--drop table wpAgents
--drop table wpOwner


insert into wpAgents values('AAQAA','agent@gmail.com','Test Agent',10,1)
insert into wpOwner values('owner@gmail.com')

select * from wpAgents

update wpAgents set agentName = 'agent@gmail.com' where agentcode = 'AAQAA'


select COUNT(*) FROM wpAgents where AgentCode = 'AAQAA'

SELECT COUNT(*) FROM wpAgents WHERE AgentCode = 'AABAA'


SELECT COUNT(*) FROM dbo.wpAgents WHERE AgentCode = 'AA OR 1=1';

EXEC spInsertUpdateSmSettings 'CuttOff_Time','11:00:00','Cuttoff time betting is close'
EXEC spInsertUpdateSmSettings 'Start_Time','15:00:00','Betting is open'
EXEC spInsertUpdateSmSettings 'Bet_Limit','15','Bet Limit'
EXEC spInsertUpdateSmSettings 'Bet_Amount','5','Bet Amount'



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
where basecombination = dbo.GetBaseCombination('QWER') and drawDate = '2026-03-20 11:00:00.000'
GROUP BY baseCombination



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
where basecombination = dbo.GetBaseCombination('QWER') and drawDate = '2026-03-20 11:00:00.000'




SELECT    
    baseCombination,	
    SUM(CASE WHEN FirstDrawSelected = 1 THEN betAmount ELSE 0 END)  AS FirstTotal,
    SUM(CASE WHEN SecondDrawSelected = 1 THEN betAmount ELSE 0 END) AS SecondTotal,
    SUM(CASE WHEN ThirdDrawSelected = 1 THEN betAmount ELSE 0 END)  AS ThirdTotal
FROM wpBetDetail
where drawDate = '2026-03-20 11:00:00.000'
group by baseCombination




DECLARE @secret VARCHAR(MAX);

-- Encrypt
SET @secret = dbo.EncryptString(N'34');

select @secret

-- Decrypt
SELECT dbo.DecryptString(@secret) AS DecryptedValue;


SELECT *,betDetailIdEnc = dbo.EncryptString(CONVERT(VARCHAR(20),betDetailId)),LTRIM(CASE WHEN firstDrawSelected = 1 THEN '1,' ELSE '' END + CASE WHEN secondDrawSelected = 1 THEN '2,' ELSE '' END + CASE WHEN thirdDrawSelected = 1 THEN '3' ELSE '' END) AS drawDisplay 
FROM wpBetDetail WHERE betId = '15' AND drawDate ='3/23/2026 11:00:00 AM';

SELECT dbo.DecryptString('0x01000000BA1466EE6A41D1F1C61201B2CB7B9FA63E5709EE70CAFE2E');


select * from aspNetRoles
select * from AspNetUserRoles



SELECT
    usr.*,
    CASE 
        WHEN EXISTS (SELECT 1 FROM wpOwner WHERE userName = usr.userName) THEN 'Owner'
        WHEN EXISTS (SELECT 1 FROM wpAgents WHERE userName = usr.userName) THEN 'Agent'
        ELSE 'Client'
    END AS accessRole
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

