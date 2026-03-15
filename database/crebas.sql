/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/14/2026  10:20 AM                        */
/* ============================================================ */

/* ============================================================ */
/*   Table: wpSmSettings                                        */
/* ============================================================ */
create table wpSmSettings
(
    varName             varchar(20)            not null,
    varValue            text                   null    ,
    description         varchar(100)           null    ,
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
    userName            varchar(30)            null    ,
    password            varchar(150)           null    ,
    email               varchar(100)           null    ,
    firstName           varchar(50)            null    ,
    lastName            varchar(50)            null    ,
    middleName          varchar(50)            null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
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
    betReferenceNo      varchar(20)            null    ,
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
    constraint PK_wpBetDetail primary key (betDetailId)
)
go

/* ============================================================ */
/*   Table: wpAgents                                            */
/* ============================================================ */
create table wpAgents
(
    agentCode           nvarchar(30)           not null,
    agentName           nvarchar(100)          null    ,
    commissionPct       decimal(10,2)          null    ,
    activeStatus        integer                null    ,
    constraint PK_wpAgent primary key (agentCode)
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

