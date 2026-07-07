CREATE OR ALTER PROCEDURE dbo.sp_allocate_receipt_number
    @tenantid       UNIQUEIDENTIFIER,
    @receiptnumber  NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @seq INT;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.receipt_sequences WITH (UPDLOCK, ROWLOCK)
        WHERE tenantid = @tenantid)
    BEGIN
        INSERT INTO dbo.receipt_sequences (tenantid, nextsequencenumber)
        VALUES (@tenantid, 1);
    END

    SELECT @seq = nextsequencenumber
    FROM dbo.receipt_sequences WITH (UPDLOCK, ROWLOCK)
    WHERE tenantid = @tenantid;

    UPDATE dbo.receipt_sequences
    SET nextsequencenumber = @seq + 1,
        updatedat = SYSUTCDATETIME()
    WHERE tenantid = @tenantid;

    SET @receiptnumber = N'RCP-' + RIGHT(REPLICATE(N'0', 7) + CAST(@seq AS NVARCHAR(10)), 7);
END
GO
