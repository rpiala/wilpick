truncate table aspnetuserRoles
truncate table aspnetuserClaims
truncate table aspnetUserLogins
truncate table aspnetUserTokens
delete from AspNetUsers

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

insert into wpOwner values('owner@gmail.com')

EXEC spInsertUpdateSmSettings 'CuttOff_Time','11:00:00','Cuttoff time betting is close'
EXEC spInsertUpdateSmSettings 'Start_Time','15:00:00','Betting is open'
EXEC spInsertUpdateSmSettings 'Bet_Limit','15','Bet Limit'
EXEC spInsertUpdateSmSettings 'Bet_Amount','5','Bet Amount'
EXEC spInsertUpdateSmSettings 'Gcash_Load_Receiver','09434331056','Gcash receiver number'
EXEC spInsertUpdateSmSettings 'Power_Agent_Code','AZLAN','Power Agent Code'