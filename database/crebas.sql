/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/10/2026  8:16 PM                         */
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
    userName            varchar(30)            null    ,
    password            varchar(150)           null    ,
    email               varchar(100)           null    ,
    firstName           varchar(50)            null    ,
    lastName            varchar(50)            null    ,
    middleName          varchar(50)            null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    constraint PK_wpAppUsers primary key (userId, aspNetUserID)
)
go

/* ============================================================ */
/*   Table: wpBetHeader                                         */
/* ============================================================ */
create table wpBetHeader
(
    betId               numeric                identity,
    userId              numeric                not null,
    betReferenceNo      varchar(20)            null    ,
    drawDate            datetime               null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    constraint PK_wpBetHeader primary key (betId, userId)
)
go

/* ============================================================ */
/*   Table: wpBetDetail                                         */
/* ============================================================ */
create table wpBetDetail
(
    betDetailId         numeric                identity,
    betId               numeric                not null,
    userId              numeric                not null,
    dateCreated         datetime               null    ,
    combination         nvarchar(6)            null    ,
    betAmount           decimal(10,2)          null    ,
    firstDrawCombi      varchar(150)           null    ,
    secondDrawCombi     varchar(150)           null    ,
    thirdDrawCombi      varchar(150)           null    ,
    firstDrawSelected   integer                null    ,
    secondDrawSelected  integer                null    ,
    thirdDrawSelected   integer                null    ,
    constraint PK_wpBetDetail primary key (betDetailId, betId, userId)
)
go

alter table wpBetHeader
    add constraint FK_WPBETHEA_REF_55_WPAPPUSE foreign key  (userId)
       references wpAppUsers (userId)
go

alter table wpBetDetail
    add constraint FK_WPBETDET_REF_62_WPBETHEA foreign key  (betId, userId)
       references wpBetHeader (betId, userId)
go

