-- =========================================
-- Create table template SQL Azure Database 
-- =========================================

IF OBJECT_ID('dbo.Pairs', 'U') IS NOT NULL
  DROP TABLE dbo.Pairs
GO

CREATE TABLE dbo.Pairs
(
	ID UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY, 
	Date datetime2 not null,
	PriceUSD decimal(18, 5) not null,
	Exchange nvarchar(50) not null,
	Code nvarchar(50) not null,
	Volume decimal(18, 5) not null
)
GO

--Insert into dbo.pairs (Name)
--Values ('Hi')

--Select * from Pairs