/*
    Module 3 | Script 07 of 09 | Establish the Human Review baseline
    Purpose: Remove automated scoring so loan officers make decisions manually.
    Run after script 06, against your assigned ZavaLendingDB-No database.
*/

-- Step 7: Remove the optional scoring contract used by the application.
DROP PROCEDURE IF EXISTS dbo.usp_ScoreLoanApplication;
GO

PRINT 'Human Review baseline is active. dbo.usp_ScoreLoanApplication is not deployed.';
GO

SELECT
    OBJECT_ID(N'dbo.usp_ScoreLoanApplication', N'P') AS ScoringProcedureObjectId,
    N'Human Review & Scoring' AS ApplicationMode;
GO
