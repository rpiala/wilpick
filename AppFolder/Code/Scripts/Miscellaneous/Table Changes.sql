--SSPM Next Gen Changes 03/31/2025

CREATE TABLE [admin].[UpdatedSspmEmployeesTemporary](
	[EMPID] [varchar](20) NOT NULL,
	[Suffix] [varchar](10) NULL,
	[Surname] [varchar](255) NULL,
	[First_Name] [varchar](255) NULL,
	[Middle_Name] [varchar](255) NULL,
	[Salary_Grade] [varchar](20) NULL,
	[Division_Code] [varchar](20) NULL,
	[Division_Name] [varchar](255) NULL,
	[Division_BUCode] [varchar](100) NULL,
	[Department_ID] [varchar](20) NULL,
	[Department_Name] [varchar](100) NULL,
	[CostCenter] [varchar](30) NULL,
	[Work_Area_Code] [varchar](20) NULL,
	[Work_Area_Desc] [varchar](100) NULL,
	[Emp_Status_Code] [varchar](30) NULL,
	[Emp_Status_Name] [varchar](100) NULL,
	[Company_Code] [varchar](100) NULL,
	[Company_Name] [varchar](100) NULL,
	[MobileNum] [varchar](100) NULL,
	[Email_Address] [varchar](100) NULL,
	[Movement_Desc] [varchar](100) NULL,
	[Movement_Effectivity_Date] [datetime] NULL,
	[Separation_Date] [datetime] NULL,
	[Separation_Type] [varchar](255) NULL,
	[PositionCode] [varchar](25) NULL,
	[PositionName] [varchar](100) NULL,
	[LastRunDate] [datetime] NULL
) ON [PRIMARY]
GO

ALTER TABLE [admin].[UpdatedSspmEmployeesTemporary] ADD  DEFAULT (getdate()) FOR [LastRunDate]
GO


/****** Object:  Table [dbo].[RefreshAccess]    Script Date: 2/27/2025 1:48:25 PM ******/
CREATE TABLE [dbo].[RefreshAccess](
	[app_id1] [nvarchar](10) NOT NULL,
	[partner_id1] [nvarchar](10) NOT NULL,
	[access_token1] [nvarchar](50) NOT NULL,
	[refresh_token1] [nvarchar](50) NOT NULL,
	[app_id2] [nvarchar](10) NOT NULL,
	[partner_id2] [nvarchar](10) NOT NULL,
	[access_token2] [nvarchar](50) NOT NULL,
	[refresh_token2] [nvarchar](50) NOT NULL,
 CONSTRAINT [APP_ID] PRIMARY KEY CLUSTERED 
(
	[app_id1] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


INSERT INTO RefreshAccess(app_id1,partner_id1,access_token1,refresh_token1,app_id2,partner_id2,access_token2,refresh_token2)
VALUES ('20', 'T4EyG', '9BAe46Ve2TxubMtKH2s1', 'OVdjHb6Ry3AY2jzjPqPV', '20', 'T4EyG', '9BAe46Ve2TxubMtKH2s1', 'OVdjHb6Ry3AY2jzjPqPV')


CREATE TABLE [dbo].[SM_SETTINGS](
    [Varkey] [nvarchar](100) NOT NULL,
    [VarValue] [nvarchar](255) NOT NULL,
    [VarRemarks] [nvarchar](255) NULL
)
GO

EXEC spInsertUpdateSmSettings'SSPM_DATA_VALIDITY','5','SSPM Data validity in minutes'
EXEC spInsertUpdateSmSettings'SSPM_LAST_DOWNLOAD_DATE',CONVERT(VARCHAR, GETDATE(),120),'SSPM Last download date'
EXEC spInsertUpdateSmSettings'TRACK_CHANGES_EMAIL_RECEPIENTS','c_rmpiala@unilab.com.ph,mdsantos@unilab.com.ph,CVResulto@unilab.com.ph','Email recipients for tracking employee changes'
EXEC spInsertUpdateSmSettings'ICOMMS_EMAIL_SENDER','icomms@unilab.com.ph','ICOMMS Email sender'
EXEC spInsertUpdateSmSettings'VAT_PERCENTAGE','12','VAT Amount percentage'

CREATE TABLE [dbo].[SSPMApiRequestLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RequestDate] [datetime] NOT NULL,
	[RequestedBy] [varchar](100) NULL,
	[ModuleName] [varchar](100) NOT NULL,
	[RequestDetails] [varchar](255) NULL,
	[ResponseCode] [varchar](50) NULL,
	[ResponseMessage] [varchar](255) NULL	
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [UQ_RequestDate_Module] UNIQUE NONCLUSTERED 
(
	[RequestDate] ASC,
	[ModuleName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

CREATE TABLE [dbo].[UserLogTransaction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RequestDate] [datetime] NOT NULL,
	[UserName] [varchar](100) NULL,
	[TransactionType] [varchar](100) NOT NULL,
	[RequestDetails] [varchar](255) NULL		
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY])



--CAB ID 3646 TFS 69186
EXEC spInsertUpdateSmSettings 'VAT_PERCENTAGE','12','VAT Amount percentage'

ALTER TABLE Billings
ADD [VATAmountFromGlobe] [money] NOT NULL DEFAULT 0.00;

ALTER TABLE BillingsError
ADD [VATAmountFromGlobe] [money] NOT NULL DEFAULT 0.00;

ALTER TABLE BillingsTemp
ADD [VATAmountFromGlobe] [money] NOT NULL DEFAULT 0.00;