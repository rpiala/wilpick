/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     2/27/2026  4:09 PM                         */
/* ============================================================ */

/* ============================================================ */
/*   Table: smSettings                                          */
/* ============================================================ */
create table smSettings
(
    varName             varchar(20)            not null,
    varValue            text                   null    ,
    description         varchar(100)           null    ,
    constraint PK_smSettings primary key (varName)
)
go

/* ============================================================ */
/*   Table: appUsers                                            */
/* ============================================================ */
create table appUsers
(
    userId              numeric                identity,
    userName            varchar(30)            null    ,
    password            varchar(150)           null    ,
    email               varchar(100)           null    ,
    firstName           varchar(50)            null    ,
    lastName            varchar(50)            null    ,
    middleName          varchar(50)            null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    constraint PK_appUsers primary key (userId)
)
go

/* ============================================================ */
/*   Table: betHeader                                           */
/* ============================================================ */
create table betHeader
(
    betId               numeric                identity,
    userId              numeric                not null,
    betReferenceNo      varchar(20)            null    ,
    drawDate            datetime               null    ,
    betTicketPrice      decimal(10,2)          null    ,
    winningPrize        decimal(10,2)          null    ,
    constraint PK_betHeader primary key (betId, userId)
)
go

/* ============================================================ */
/*   Table: betDetail                                           */
/* ============================================================ */
create table betDetail
(
    betDetailId         numeric                identity,
    betId               numeric                not null,
    userId              numeric                not null,
    dateCreated         datetime               null    ,
    firstElement        char(1)                null    ,
    secondElement       char(1)                null    ,
    thirdElement        char(1)                null    ,
    fourthElement       char(1)                null    ,
    betAmount           decimal(10,2)          null    ,
    firstDrawCombi      varchar(150)           null    ,
    secondDrawCombi     varchar(150)           null    ,
    thirdDrawCombi      varchar(150)           null    ,
    firstDrawSelected   integer                null    ,
    secondDrawSelected  integer                null    ,
    thirdDrawSelected   integer                null    ,
    constraint PK_betDetail primary key (betDetailId, betId, userId)
)
go

alter table betHeader
    add constraint FK_BETHEADE_REF_55_APPUSERS foreign key  (userId)
       references appUsers (userId)
go

alter table betDetail
    add constraint FK_BETDETAI_REF_62_BETHEADE foreign key  (betId, userId)
       references betHeader (betId, userId)
go

