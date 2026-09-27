/*
    Module 3 | Utility 00 | Reset workshop data
    Purpose: Remove generated decisions, added history rows, and completed statuses.
    Run only when instructed, and only against your assigned ZavaLendingDB-No database.
*/

DELETE FROM dbo.LoanNarrativeEmbeddings WHERE LoanId > 100;
DELETE FROM dbo.LoanHistory WHERE LoanId > 100;
GO

SELECT COUNT(*) AS Decisions FROM dbo.LoanDecisions;
DELETE FROM dbo.LoanDecisions;
GO

SELECT COUNT(*) AS Applications FROM dbo.LoanApplications WHERE Status in ('Approved', 'Denied');
UPDATE dbo.LoanApplications SET Status = 'Pending' WHERE Status in ('Approved', 'Denied');
GO

SELECT COUNT(*) AS Embeddings FROM dbo.LoanNarrativeEmbeddings;
SELECT COUNT(*) AS LoanHistory FROM dbo.LoanHistory;
GO
