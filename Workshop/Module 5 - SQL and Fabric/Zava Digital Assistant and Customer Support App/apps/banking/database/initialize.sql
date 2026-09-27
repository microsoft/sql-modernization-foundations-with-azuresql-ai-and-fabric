SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.FaqEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FaqEntries (
        Id nvarchar(40) NOT NULL CONSTRAINT PK_FaqEntries PRIMARY KEY,
        Topic nvarchar(60) NOT NULL,
        Question nvarchar(300) NOT NULL,
        Answer nvarchar(2000) NOT NULL,
        Source nvarchar(120) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.ChatSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatSessions (
        SessionId uniqueidentifier NOT NULL CONSTRAINT PK_ChatSessions PRIMARY KEY,
        SessionOrdinalId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT UQ_ChatSessions_Ordinal UNIQUE,
        OwnerHash binary(32) NOT NULL,
        CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ChatSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ChatSessions_UpdatedAt DEFAULT SYSUTCDATETIME(),
        State varchar(12) NOT NULL CONSTRAINT DF_ChatSessions_State DEFAULT 'Open',
        NextSequence int NOT NULL CONSTRAINT DF_ChatSessions_NextSequence DEFAULT 1,
        CONSTRAINT CK_ChatSessions_State CHECK (State IN ('Open', 'Escalated'))
    );
END;

IF COL_LENGTH(N'dbo.ChatSessions', N'SessionOrdinalId') IS NULL
BEGIN
    ALTER TABLE dbo.ChatSessions ADD SessionOrdinalId bigint IDENTITY(1, 1) NOT NULL;
    ALTER TABLE dbo.ChatSessions ADD CONSTRAINT UQ_ChatSessions_Ordinal UNIQUE (SessionOrdinalId);
END;

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages (
        MessageId uniqueidentifier NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        MessageOrdinalId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT UQ_ChatMessages_Ordinal UNIQUE,
        SessionId uniqueidentifier NOT NULL,
        RequestId uniqueidentifier NOT NULL,
        Sequence int NOT NULL,
        Role varchar(10) NOT NULL,
        Content nvarchar(3200) NOT NULL,
        CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
        Status varchar(12) NOT NULL,
        SourcesJson nvarchar(max) NOT NULL CONSTRAINT DF_ChatMessages_Sources DEFAULT N'[]',
        Model nvarchar(200) NULL,
        InputTokens int NULL,
        OutputTokens int NULL,
        FinishReason nvarchar(40) NULL,
        AttemptId uniqueidentifier NULL,
        LeaseUntil datetime2(7) NULL,
        AttemptsJson nvarchar(max) NOT NULL CONSTRAINT DF_ChatMessages_Attempts DEFAULT N'[]',
        CONSTRAINT FK_ChatMessages_Session FOREIGN KEY (SessionId) REFERENCES dbo.ChatSessions(SessionId),
        CONSTRAINT UQ_ChatMessages_Sequence UNIQUE (SessionId, Sequence),
        CONSTRAINT UQ_ChatMessages_Request UNIQUE (SessionId, RequestId, Role),
        CONSTRAINT CK_ChatMessages_Role CHECK (Role IN ('user', 'assistant')),
        CONSTRAINT CK_ChatMessages_Status CHECK (Status IN ('Pending', 'Completed', 'Failed')),
        CONSTRAINT CK_ChatMessages_Sources CHECK (ISJSON(SourcesJson) = 1),
        CONSTRAINT CK_ChatMessages_Attempts CHECK (ISJSON(AttemptsJson) = 1)
    );
END;

IF COL_LENGTH(N'dbo.ChatMessages', N'MessageOrdinalId') IS NULL
BEGIN
    ALTER TABLE dbo.ChatMessages ADD MessageOrdinalId bigint IDENTITY(1, 1) NOT NULL;
    ALTER TABLE dbo.ChatMessages ADD CONSTRAINT UQ_ChatMessages_Ordinal UNIQUE (MessageOrdinalId);
END;

IF OBJECT_ID(N'dbo.ChatEscalations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatEscalations (
        EscalationId uniqueidentifier NOT NULL CONSTRAINT PK_ChatEscalations PRIMARY KEY,
        EscalationOrdinalId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT UQ_ChatEscalations_Ordinal UNIQUE,
        SessionId uniqueidentifier NOT NULL,
        RequestId uniqueidentifier NOT NULL,
        Email nvarchar(254) NOT NULL,
        CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ChatEscalations_CreatedAt DEFAULT SYSUTCDATETIME(),
        ContractVersion int NOT NULL CONSTRAINT DF_ChatEscalations_Version DEFAULT 1,
        TranscriptJson nvarchar(max) NOT NULL,
        CONSTRAINT FK_ChatEscalations_Session FOREIGN KEY (SessionId) REFERENCES dbo.ChatSessions(SessionId),
        CONSTRAINT UQ_ChatEscalations_Session UNIQUE (SessionId),
        CONSTRAINT CK_ChatEscalations_Email CHECK (LEN(Email) BETWEEN 3 AND 254),
        CONSTRAINT CK_ChatEscalations_Transcript CHECK (ISJSON(TranscriptJson) = 1 AND DATALENGTH(TranscriptJson) <= 61440)
    );
END;

IF COL_LENGTH(N'dbo.ChatEscalations', N'EscalationOrdinalId') IS NULL
BEGIN
    ALTER TABLE dbo.ChatEscalations ADD EscalationOrdinalId bigint IDENTITY(1, 1) NOT NULL;
    ALTER TABLE dbo.ChatEscalations ADD CONSTRAINT UQ_ChatEscalations_Ordinal UNIQUE (EscalationOrdinalId);
END;

INSERT dbo.FaqEntries (Id, Topic, Question, Answer, Source)
SELECT seed.Id, seed.Topic, seed.Question, seed.Answer, seed.Source
FROM (VALUES
    (N'zava-about', N'About Zava', N'What is Zava?',
     N'Zava is a fictional bank used for a workshop. No real accounts, applications, money transfers, approvals or financial advice are provided. Use fictional information only.', N'/#about'),
    (N'personal-loans', N'Personal lending', N'What personal lending options are available?',
     N'The fictional Zava Personal Loan is an unsecured loan for a planned purchase or consolidating eligible borrowing. It has scheduled monthly repayments. This demo does not publish interest rates, fees, loan amounts, loan terms or eligibility criteria and cannot quote, assess eligibility or accept applications.', N'/#lending'),
    (N'loan-preparation', N'Personal lending', N'What should I prepare before discussing a loan?',
     N'Consider the purpose of borrowing, a comfortable monthly repayment and existing commitments. A real lender may ask for identity, income and expenditure information, but never submit those documents, account numbers or sensitive financial information in this workshop chat.', N'/#lending'),
    (N'mortgages', N'Mortgages', N'Can Zava help with a first home or a move?',
     N'The fictional Zava Home Mortgage illustrates first-home and home-move conversations. A real mortgage discussion considers the property, deposit, income and affordability. This demo provides no mortgage rates, deposit minimums, lending limits, offers, decisions or advice.', N'/#mortgages'),
    (N'mortgage-types', N'Mortgages', N'What is the difference between fixed and variable rates?',
     N'In general, a fixed interest rate stays the same for an agreed period. A variable rate may change, which can affect payments. This is general information, not a Zava offer or a recommendation. Product availability and terms must be confirmed with a real lender.', N'/#mortgages'),
    (N'savings', N'Savings', N'What savings options does Zava illustrate?',
     N'The fictional Zava Everyday Saver illustrates flexible saving and the fictional Zava Goal Saver illustrates saving towards a goal. No interest rates, balances, minimum deposits, withdrawal rules, protection guarantees or tax treatment are specified. No real savings account can be opened here.', N'/#savings'),
    (N'savings-planning', N'Savings', N'How can I think about a savings goal?',
     N'For general planning, identify the goal, a target date and an affordable regular contribution. Keep unexpected costs in mind. The workshop does not calculate projections, recommend investments or provide personal financial advice.', N'/#savings'),
    (N'follow-up', N'Contact', N'Can I speak with a person?',
     N'Choose Chat with a real person, provide a fictional email address and select Request follow-up. A request is recorded only after the email is submitted. The request contains the full stored conversation and closes that chat. This workshop does not connect a live agent or send email, and no response time is promised.', N'/#contact'),
    (N'privacy', N'Privacy', N'What happens to this conversation?',
     N'Visitor messages, AI replies, session state and available model metadata are stored in the workshop Fabric SQL database. An email follow-up request stores the email and a full conversation snapshot, which can be sent to Azure Event Hubs by Change Event Streaming. Refreshing or starting a new chat does not delete SQL history. Use only fictional data.', N'/#privacy'),
    (N'limitations', N'Help', N'Can the assistant access my accounts or make decisions?',
     N'No. The assistant can explain the prepared fictional Zava FAQ. It cannot access accounts, process transactions, submit applications, approve lending, verify identity or give personalized financial, legal or tax advice. If information is not in the FAQ, it should say so.', N'/#about')
) AS seed(Id, Topic, Question, Answer, Source)
WHERE NOT EXISTS (SELECT 1 FROM dbo.FaqEntries AS existing WITH (UPDLOCK, HOLDLOCK) WHERE existing.Id = seed.Id);

COMMIT;


-- Expands dbo.FaqEntries with entries matching the supported-loan-type questions
-- generated by seed-chat-scenarios.sql (Auto, Personal, SmallBusiness, HomeImprovement).
-- Idempotent: existing Ids are skipped, so it is safe to re-run. All content is fictional
-- and follows the demo rule of publishing no rates, fees, amounts or eligibility criteria.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
    OR OBJECT_ID(N'dbo.FaqEntries', N'U') IS NULL
    THROW 51030, 'Connect to the workshop Fabric SQL database and run initialize.sql first.', 1;

BEGIN TRANSACTION;

INSERT dbo.FaqEntries (Id, Topic, Question, Answer, Source)
SELECT seed.Id, seed.Topic, seed.Question, seed.Answer, seed.Source
FROM (VALUES
    (N'auto-repayment-options', N'Auto lending', N'Can you tell me more about the Zava Auto Loan repayment options?',
     N'The fictional Zava Auto Loan is repaid in scheduled monthly instalments over an agreed term. This demo does not publish interest rates, fees or specific repayment figures. A Zava specialist can outline the options for your situation.', N'/#lending'),
    (N'auto-documents', N'Auto lending', N'What documents do I need for an Auto Loan application?',
     N'A real Auto Loan discussion may consider identity, income and vehicle details, but never submit documents, account numbers or sensitive financial information in this workshop. A specialist can explain what a genuine application would require.', N'/#lending'),
    (N'auto-term-length', N'Auto lending', N'How long can I finance a car with the Auto Loan?',
     N'The Auto Loan term is agreed with the lender and sets the length of the repayment schedule. This demo does not state minimum or maximum terms. A Zava specialist can walk you through the available choices.', N'/#lending'),
    (N'auto-used-vehicles', N'Auto lending', N'Is the Auto Loan available for used vehicles as well?',
     N'The fictional Zava Auto Loan illustrates financing conversations for cars. Whether a specific vehicle, new or used, would qualify must be confirmed with a real lender. This workshop makes no eligibility decisions.', N'/#lending'),
    (N'auto-max-amount', N'Auto lending', N'What is the maximum amount I can borrow with an Auto Loan?',
     N'This demo does not publish loan amounts, limits or eligibility criteria and cannot quote figures. A Zava specialist can discuss what an actual Auto Loan might offer based on your circumstances.', N'/#lending'),
    (N'personal-terms', N'Personal lending', N'I would like more details about the Personal Loan terms.',
     N'The fictional Zava Personal Loan is an unsecured loan with scheduled monthly repayments over an agreed term. Rates, fees, amounts and eligibility are not published here. A specialist can share how the terms are structured.', N'/#lending'),
    (N'personal-consolidation', N'Personal lending', N'Can the Personal Loan be used to consolidate other borrowing?',
     N'A Personal Loan is sometimes used to consolidate eligible borrowing into one scheduled repayment. Whether that suits you depends on your circumstances, which a real lender would review. This demo does not provide financial advice.', N'/#lending'),
    (N'personal-monthly-repayment', N'Personal lending', N'What monthly repayment should I expect on a Personal Loan?',
     N'Monthly repayments depend on the amount, term and rate agreed with a real lender. This workshop does not calculate quotes or projections. A Zava specialist can explain how repayments are generally structured.', N'/#lending'),
    (N'personal-early-repayment', N'Personal lending', N'Are there early repayment charges on the Personal Loan?',
     N'Whether early repayment charges apply is a product term confirmed with a real lender. This demo does not publish fees or charges. A specialist can clarify the details of a genuine Personal Loan.', N'/#lending'),
    (N'personal-eligibility', N'Personal lending', N'How is eligibility assessed for a Personal Loan?',
     N'A real lender assesses a Personal Loan using identity, income and affordability information. This workshop cannot assess eligibility or accept applications. A specialist can explain the general process.', N'/#lending'),
    (N'smallbusiness-options', N'Small business lending', N'Tell me more about the Small Business Loan options.',
     N'The fictional Zava Small Business Loan illustrates lending conversations for businesses. This demo does not publish rates, amounts, terms or eligibility. A specialist can outline what an actual product might involve.', N'/#lending'),
    (N'smallbusiness-uses', N'Small business lending', N'What can the Small Business Loan be used for?',
     N'A Small Business Loan is generally used for business purposes such as growth or working capital. Specific eligible uses are confirmed with a real lender. This workshop provides general information only.', N'/#lending'),
    (N'smallbusiness-collateral', N'Small business lending', N'Does the Small Business Loan require collateral?',
     N'Whether security or collateral is needed is a product term set by a real lender. This demo does not state lending conditions. A Zava specialist can explain how a genuine Small Business Loan works.', N'/#lending'),
    (N'smallbusiness-term', N'Small business lending', N'How long is the term on a Small Business Loan?',
     N'The term sets the length of the repayment schedule and is agreed with the lender. This workshop does not publish minimum or maximum terms. A specialist can discuss the available options.', N'/#lending'),
    (N'smallbusiness-new-business', N'Small business lending', N'Can a newly formed business apply for the Small Business Loan?',
     N'Whether a new business qualifies depends on criteria a real lender would review. This demo cannot assess eligibility or accept applications. A specialist can explain the general requirements.', N'/#lending'),
    (N'homeimprovement-overview', N'Home improvement lending', N'I want to learn more about the Home Improvement Loan.',
     N'The fictional Zava Home Improvement Loan illustrates borrowing for home projects with scheduled monthly repayments. This demo does not publish rates, amounts or eligibility. A specialist can share the details.', N'/#lending'),
    (N'homeimprovement-kitchen', N'Home improvement lending', N'Can the Home Improvement Loan cover a kitchen renovation?',
     N'A Home Improvement Loan is generally intended for home projects such as renovations. Whether a specific project qualifies is confirmed with a real lender. This workshop offers general information only.', N'/#lending'),
    (N'homeimprovement-limit', N'Home improvement lending', N'What is the borrowing limit on a Home Improvement Loan?',
     N'This demo does not publish loan amounts, limits or eligibility criteria and cannot quote figures. A Zava specialist can discuss what an actual Home Improvement Loan might offer.', N'/#lending'),
    (N'homeimprovement-quotes', N'Home improvement lending', N'Are contractor quotes required for a Home Improvement Loan?',
     N'A real lender may consider project details, but never submit quotes, documents or sensitive information in this workshop. A specialist can explain what a genuine application would involve.', N'/#lending'),
    (N'homeimprovement-funding', N'Home improvement lending', N'How quickly are Home Improvement Loans funded once approved?',
     N'Funding timelines depend on a real lender''s process and are not promised here. This demo makes no approvals or time commitments. A specialist can outline how funding generally works.', N'/#lending')
) AS seed(Id, Topic, Question, Answer, Source)
WHERE NOT EXISTS (SELECT 1 FROM dbo.FaqEntries AS existing WITH (UPDLOCK, HOLDLOCK) WHERE existing.Id = seed.Id);

DECLARE @added int = @@ROWCOUNT;

COMMIT;

PRINT CONCAT('FAQ expansion complete. New entries added: ', @added, '.');
