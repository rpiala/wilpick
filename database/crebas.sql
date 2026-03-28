/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/28/2026  10:59 AM                        */
/* ============================================================ */

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
    includeRamble       integer                null    ,
    constraint PK_wpBetDetail primary key (betDetailId)
)
go

alter table wpBetDetail
    add constraint FK_WPBETDET_REF_125_WPBETHEA foreign key  (betId)
       references wpBetHeader (betId)
go

