-- Phone number and emergency contact on users (staff / doctors).

IF COL_LENGTH('dbo.users', 'phonenumber') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD phonenumber NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('dbo.users', 'emergencycontactnumber') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD emergencycontactnumber NVARCHAR(20) NULL;
END
GO
