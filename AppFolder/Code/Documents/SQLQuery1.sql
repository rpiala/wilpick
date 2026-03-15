USE wilpick
select * from wpUserLogTransaction
select * from AspNetUsers
select * from wpBetHeader

--insert into dbo.wpappusers(aspNetUserID) values('tsetsetestsdfdsf')

select SCOPE_IDENTITY()

select * from wpAppUsers

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