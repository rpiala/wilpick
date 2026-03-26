/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/26/2026  10:18 PM                        */
/* ============================================================ */

/* ============================================================ */
/*   Table: wpSmSettings                                        */
/* ============================================================ */
create table wpSmSettings
(
    varName             nvarchar(20)           not null,
    varValue            text                   null    ,
    description         nvarchar(100)          null    ,
    constraint PK_wpSmSettings primary key (varName)
)
go

/* ============================================================ */
/*   Table: wpAppUsers                                          */
/* ============================================================ */
create table wpAppUsers
(
    userId              numeric                identity,
    aspNetUserID        nvarchar(255)          not null,
    agentCode           nvarchar(30)           not null,
    dateRegistered      datetime               null    ,
    userName            nvarchar(60)           null    ,
    password            nvarchar(150)          null    ,
    email               nvarchar(100)          null    ,
    firstName           nvarchar(50)           null    ,
    lastName            nvarchar(50)           null    ,
    middleName          nvarchar(50)           null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    betType             nvarchar(20)           null    ,
    constraint PK_wpAppUsers primary key (userId, aspNetUserID, agentCode)
)
go

/* ============================================================ */
/*   Table: wpBetHeader                                         */
/* ============================================================ */
create table wpBetHeader
(
    betId               numeric                identity,
    userId              numeric                not null,
    aspNetUserID        nvarchar(255)          not null,
    agentCode           nvarchar(30)           not null,
    betReferenceNo      nvarchar(20)           null    ,
    drawDate            datetime               null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    constraint PK_wpBetHeader primary key (betId)
)
go

/* ============================================================ */
/*   Table: wpBetDetail                                         */
/* ============================================================ */
create table wpBetDetail
(
    betDetailId         numeric                identity,
    betId               numeric                not null,
    drawDate            datetime               null    ,
    dateCreated         datetime               null    ,
    combination         nvarchar(6)            null    ,
    baseCombination     nvarchar(6)            null    ,
    betAmount           decimal(10,2)          null    ,
    firstDrawSelected   integer                null    ,
    secondDrawSelected  integer                null    ,
    thirdDrawSelected   integer                null    ,
    betType             nvarchar(20)           null    ,
    constraint PK_wpBetDetail primary key (betDetailId)
)
go

/* ============================================================ */
/*   Table: wpAgents                                            */
/* ============================================================ */
create table wpAgents
(
    agentCode           nvarchar(30)           not null,
    userName            nvarchar(60)           null    ,
    agentName           nvarchar(100)          null    ,
    commissionPct       decimal(10,2)          null    ,
    activeStatus        integer                null    ,
    constraint PK_wpAgent primary key (agentCode)
)
go

/* ============================================================ */
/*   Table: wpOwner                                             */
/* ============================================================ */
create table wpOwner
(
    owerId              numeric                identity,
    UserName            nvarchar(60)           null    ,
    constraint PK_wpOwner primary key (owerId)
)
go

/* ============================================================ */
/*   Table: wpUserLoadTrans                                     */
/* ============================================================ */
create table wpUserLoadTrans
(
    loadId              numeric                identity,
    userId              numeric                not null,
    requestedDate       datetime               null    ,
    approvedDate        datetime               null    ,
    requestedAmount     decimal(10,2)          null    ,
    approvedAmount      decimal(10,2)          null    ,
    approvedBy          nvarchar(60)           null    ,
    isApproved          integer                null    ,
    attachmentFileName  nvarchar(100)          null    ,
    resultId            numeric                null    ,
    remarks             nvarchar(100)          null    ,
    constraint PK_wpUserLoadTrans primary key (loadId, userId)
)
go

/* ============================================================ */
/*   Table: wpDrawResults                                       */
/* ============================================================ */
create table wpDrawResults
(
    resultId            numeric                identity,
    drawDate            datetime               null    ,
    dateEntered         datetime               null    ,
    firstResult         nvarchar(6)            null    ,
    secondResult        nvarchar(6)            null    ,
    thirdResult         nvarchar(6)            null    ,
    enteredBy           nvarchar(60)           null    ,
    constraint PK_wpDrawResults primary key (resultId)
)
go

/* ============================================================ */
/*   Table: wpCashOutTransactions                               */
/* ============================================================ */
create table wpCashOutTransactions
(
    cashOutId           numeric                identity,
    userId              numeric                not null,
    requestedDate       datetime               null    ,
    completedDate       datetime               null    ,
    cashOutAmount       decimal(10,2)          null    ,
    isCompleted         integer                null    ,
    attachmentFileName  nvarchar(100)          null    ,
    processedBy         nvarchar(60)           null    ,
    constraint PK_wpCashOutTransactions primary key (cashOutId, userId)
)
go

/* ============================================================ */
/*   Table: wpUserLogTransaction                                */
/* ============================================================ */
create table wpUserLogTransaction
(
    Id                  numeric                identity,
    requestDate         datetime               null    ,
    userName            nvarchar(100)          null    ,
    transactionType     nvarchar(100)          null    ,
    requestDetails      nvarchar(255)          null    ,
    constraint PK_wpUserLogTransaction primary key (Id)
)
go

alter table wpAppUsers
    add constraint FK_WPAPPUSE_REF_113_WPAGENTS foreign key  (agentCode)
       references wpAgents (agentCode)
go

alter table wpBetHeader
    add constraint FK_WPBETHEA_REF_55_WPAPPUSE foreign key  (userId, aspNetUserID, agentCode)
       references wpAppUsers (userId, aspNetUserID, agentCode)
go

alter table wpBetDetail
    add constraint FK_WPBETDET_REF_125_WPBETHEA foreign key  (betId)
       references wpBetHeader (betId)
go

