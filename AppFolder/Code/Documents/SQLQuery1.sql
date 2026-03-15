USE wilpick
select * from wpUserLogTransaction
select * from AspNetUsers
select * from wpBetHeader
select * from wpBetDetail

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


drop table wpSmSettings
drop table wpBetDetail
drop table wpBetHeader
drop table wpBetDetail
drop table wpAppUsers
drop table wpAgents

insert into wpAgents values('AAQAA','Test Agent',10,1)

select * from wpAgents


select COUNT(*) FROM wpAgents where AgentCode = 'AAQAA'

SELECT COUNT(*) FROM wpAgents WHERE AgentCode = 'AABAA'


SELECT COUNT(*) FROM dbo.wpAgents WHERE AgentCode = 'AA OR 1=1';

EXEC spInsertUpdateSmSettings 'CuttOff_Time','11:00:00','Cuttoff time betting is close'
EXEC spInsertUpdateSmSettings 'Start_Time','15:00:00','Betting is open'