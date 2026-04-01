/* ============================================================ */
/*   Database name:  Model_1                                    */
/*   DBMS name:      Sybase AS Enterprise 12.0                  */
/*   Created on:     3/29/2026  11:27 AM                        */
/* ============================================================ */

/* ============================================================ */
/*   Table: wpUserLoadTrans                                     */
/* ============================================================ */
create table wpUserLoadTrans
(
    loadId                numeric                identity,
    userId                numeric                not null,
    requestedDate         datetime               null    ,
    approvedDate          datetime               null    ,
    requestedAmount       decimal(10,2)          null    ,
    approvedAmount        decimal(10,2)          null    ,
    approvedBy            nvarchar(60)           null    ,
    isApproved            integer                null    ,
    attachmentFileName    nvarchar(100)          null    ,
    resultId              numeric                null    ,
    remarks               nvarchar(100)          null    ,
    receiverMobileNumber  nvarchar(25)           null    ,
    constraint PK_wpUserLoadTrans primary key (loadId, userId)
)
go

