/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/29/2026  8:04 PM                         */
/* ============================================================ */

/* ============================================================ */
/*   Table: wpCashOutTransactions                               */
/* ============================================================ */
create table wpCashOutTransactions
(
    cashOutId             numeric                identity,
    userId                numeric                not null,
    requestedDate         datetime               null    ,
    completedDate         datetime               null    ,
    cashOutAmount         decimal(10,2)          null    ,
    isCompleted           integer                null    ,
    attachmentFileName    nvarchar(100)          null    ,
    processedBy           nvarchar(60)           null    ,
    receiverMobileNumber  nvarchar(25)           null    ,
    receiverName          nvarchar(100)          null    ,
    constraint PK_wpCashOutTransactions primary key (cashOutId, userId)
)
go

