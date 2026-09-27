/*
    Module 3 | Script 09 of 09 | Validate AI-assisted scoring
    Purpose: Run one assessment and audit the procedure's no-write boundary.
    Run after script 08, against your assigned ZavaLendingDB-No database.
*/

-- Step 9a: Score one representative application
EXEC dbo.usp_ScoreLoanApplication @ApplicationId = 1;
GO

-- Step 9b: Audit the procedure for model access and persistence statements
DECLARE @ProcedureDefinition NVARCHAR(MAX) =
    OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ScoreLoanApplication'));

SELECT
    CASE
        WHEN @ProcedureDefinition IS NULL
            THEN N'Scoring procedure not found'
        WHEN @ProcedureDefinition LIKE N'%sp_invoke_external_rest_endpoint%'
            THEN N'AI-Assisted Scoring'
        ELSE N'External AI call not detected'
    END AS ScoringMode,
    CASE
        WHEN @ProcedureDefinition IS NULL
            THEN N'Audit unavailable'
        WHEN @ProcedureDefinition LIKE N'%INSERT%LoanDecisions%'
          OR @ProcedureDefinition LIKE N'%UPDATE%LoanApplications%'
          OR @ProcedureDefinition LIKE N'%INSERT%AIOperationsLedger%'
            THEN N'Review required: persistence statement detected'
        ELSE N'Advisory only: no persistence statements detected'
    END AS SafetyBoundary;
GO
