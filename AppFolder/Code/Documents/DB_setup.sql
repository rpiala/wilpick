truncate table aspnetuserRoles
truncate table aspnetuserClaims
truncate table aspnetUserLogins
truncate table aspnetUserTokens
delete from AspNetUsers

truncate table wpUserLogTransaction
truncate table wpCashOutTransactions
truncate table wpDrawResults
truncate table wpUserLoadTrans
truncate table wpSmSettings
truncate table wpBetDetail
truncate table wpBetHeader
truncate table wpAppUsers
truncate table wpAgents
truncate table wpOwner

drop table wpUserLogTransaction
drop table wpCashOutTransactions
drop table wpDrawResults
drop table wpUserLoadTrans
drop table wpSmSettings
drop table wpBetDetail
drop table wpBetHeader
drop table wpAppUsers
drop table wpAgents
drop table wpOwner

--insert into wpOwner values('owner@gmail.com')

EXEC spInsertUpdateSmSettings 'CuttOff_Time','11:00:00','Cuttoff time betting is close'
EXEC spInsertUpdateSmSettings 'Start_Time','14:00:00','Betting is open'
EXEC spInsertUpdateSmSettings 'Bet_Limit','10','Bet Limit'
EXEC spInsertUpdateSmSettings 'Ticket_Price','10','Prize per ticket'
EXEC spInsertUpdateSmSettings 'Gcash_Load_Receiver','09434331056','Gcash receiver number'
EXEC spInsertUpdateSmSettings 'Power_Agent_Code','AZLAN','Power Agent Code'
EXEC spInsertUpdateSmSettings 'Power_Owner_Code','PRIME','Power Agent Code'
EXEC spInsertUpdateSmSettings 'Agent_Commission_Pct','33','Agent Commission'
EXEC spInsertUpdateSmSettings 'Winning_Prize','30000','Winning Prize'
EXEC spInsertUpdateSmSettings 'Ramble_Winning_Prize','3000','Winning Prize'