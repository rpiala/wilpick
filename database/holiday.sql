/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     4/3/2026  9:26 AM                          */
/* ============================================================ */

/* ============================================================ */
/*   Table: wpDrawHoliday                                       */
/* ============================================================ */
create table wpDrawHoliday
(
    holidayId    numeric                identity,
    holidayDate  datetime               null    ,
    holidayName  nvarchar(100)          null    ,
    addedBy      nvarchar(60)           null    ,
    addedDate    datetime               null    ,
    isDeleted    integer                null    ,
    constraint PK_wpDrawHoliday primary key (holidayId)
)
go

