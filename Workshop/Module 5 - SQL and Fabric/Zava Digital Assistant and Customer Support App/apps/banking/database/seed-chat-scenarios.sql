-- Seeds ChatSessions, ChatMessages, and ChatEscalations with three demo scenarios.
--   Scenario 1 (supported):   sessions asking for detail about supported loan types
--                             (Auto, Personal, SmallBusiness, HomeImprovement) with a generic reply. State stays Open.
--   Scenario 2 (unsupported): sessions asking about loan types Zava does not offer. The reply declines and
--                             each session is escalated into dbo.ChatEscalations with a random fictional email.
--   Scenario 3 (generic):     ~10% of sessions asking general questions about Zava Lending (not product-specific),
--                             answered generically, then escalated into dbo.ChatEscalations with a random fictional email.
-- Session counts are controlled by the @SupportedCount / @UnsupportedCount / @GenericCount variables below.
-- Re-running this script appends another batch (every row uses NEWID()). All data is fictional.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
    OR OBJECT_ID(N'dbo.ChatSessions', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatEscalations', N'U') IS NULL
    THROW 51010, 'Connect to the workshop Fabric SQL database and run initialize.sql first.', 1;

BEGIN TRANSACTION;

/* ---------- Volume knobs ----------------------------------------------------- */

DECLARE @SupportedCount   int = 15;   -- Scenario 1: supported loan questions (Open)
DECLARE @UnsupportedCount int = 30;   -- Scenario 2: unsupported loan questions (Escalated)
DECLARE @GenericCount     int = 5;   -- Scenario 3: general Zava Lending questions (Escalated) ~10%

/* ---------- Reference pools -------------------------------------------------- */

DECLARE @ExistingQuestions TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, LoanType nvarchar(40), Question nvarchar(400));
INSERT @ExistingQuestions (LoanType, Question) VALUES
    (N'Auto',            N'Can you tell me more about the Zava Auto Loan repayment options?'),
    (N'Auto',            N'What documents do I need for an Auto Loan application?'),
    (N'Auto',            N'How long can I finance a car with the Auto Loan?'),
    (N'Auto',            N'Is the Auto Loan available for used vehicles as well?'),
    (N'Auto',            N'What is the maximum amount I can borrow with an Auto Loan?'),
    (N'Auto',            N'Can I refinance an existing car loan with Zava?'),
    (N'Auto',            N'Does the Auto Loan cover electric or hybrid vehicles?'),
    (N'Auto',            N'Are there any early settlement options on the Auto Loan?'),
    (N'Personal',        N'I''d like more details about the Personal Loan terms.'),
    (N'Personal',        N'Can the Personal Loan be used to consolidate other borrowing?'),
    (N'Personal',        N'What monthly repayment should I expect on a Personal Loan?'),
    (N'Personal',        N'Are there early repayment charges on the Personal Loan?'),
    (N'Personal',        N'How is eligibility assessed for a Personal Loan?'),
    (N'Personal',        N'What is the minimum and maximum term for a Personal Loan?'),
    (N'Personal',        N'Can I apply for a Personal Loan with a joint applicant?'),
    (N'Personal',        N'How quickly are Personal Loan funds released after approval?'),
    (N'SmallBusiness',   N'Tell me more about the Small Business Loan options.'),
    (N'SmallBusiness',   N'What can the Small Business Loan be used for?'),
    (N'SmallBusiness',   N'Does the Small Business Loan require collateral?'),
    (N'SmallBusiness',   N'How long is the term on a Small Business Loan?'),
    (N'SmallBusiness',   N'Can a newly formed business apply for the Small Business Loan?'),
    (N'SmallBusiness',   N'What paperwork does a Small Business Loan application need?'),
    (N'SmallBusiness',   N'Is there a limit on how much a business can borrow?'),
    (N'SmallBusiness',   N'Can I use the Small Business Loan to buy equipment?'),
    (N'HomeImprovement', N'I want to learn more about the Home Improvement Loan.'),
    (N'HomeImprovement', N'Can the Home Improvement Loan cover a kitchen renovation?'),
    (N'HomeImprovement', N'What is the borrowing limit on a Home Improvement Loan?'),
    (N'HomeImprovement', N'Are contractor quotes required for a Home Improvement Loan?'),
    (N'HomeImprovement', N'How quickly are Home Improvement Loans funded once approved?'),
    (N'HomeImprovement', N'Can I use a Home Improvement Loan for an extension?'),
    (N'HomeImprovement', N'Does the Home Improvement Loan cover energy-efficiency upgrades?'),
    (N'HomeImprovement', N'What repayment terms are available on a Home Improvement Loan?');

DECLARE @GenericAnswers TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Answer nvarchar(2000));
INSERT @GenericAnswers (Answer) VALUES
    (N'Thanks for your interest in our lending options. A Zava specialist can walk you through the details, eligibility, and next steps for this loan.'),
    (N'That''s a great question. Our lending team can share the specific terms and help you find the right fit for your needs.'),
    (N'I''d be happy to help. This loan has flexible options, and a Zava advisor can provide tailored guidance based on your situation.'),
    (N'Good question! There are several features available for this loan. A member of our lending team can review the details and answer any follow-up questions.'),
    (N'Zava offers competitive terms on this loan. For personalized figures and eligibility, our specialists are ready to assist you.');

DECLARE @MissingLoans TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Loan nvarchar(60));
INSERT @MissingLoans (Loan) VALUES
    (N'student loan'), (N'payday loan'), (N'boat loan'), (N'RV loan'), (N'motorcycle loan'),
    (N'jumbo mortgage'), (N'reverse mortgage'), (N'home equity line of credit'), (N'construction loan'),
    (N'bridge loan'), (N'farm loan'), (N'medical loan'), (N'wedding loan'), (N'travel loan'),
    (N'title loan'), (N'land loan'), (N'commercial real estate loan'), (N'equipment financing loan'),
    (N'invoice financing facility'), (N'solar energy loan'), (N'credit-builder loan'), (N'aircraft loan'),
    (N'franchise loan'), (N'working capital loan'), (N'livestock loan');

DECLARE @MissingTemplates TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Template nvarchar(200));
INSERT @MissingTemplates (Template) VALUES
    (N'Do you offer a {loan}?'),
    (N'I''m interested in a {loan}. Can Zava help me with that?'),
    (N'What are the current rates on your {loan}?'),
    (N'Can I apply for a {loan} through Zava Lending?'),
    (N'Does Zava provide {loan} options?'),
    (N'I''d like some information about a {loan}.'),
    (N'Is a {loan} something Zava offers?');

-- Scenario 3 questions are general enquiries that do not map to any product.
DECLARE @GenericQuestions TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Question nvarchar(400));
INSERT @GenericQuestions (Question) VALUES
    (N'What are Zava Lending''s opening hours?'),
    (N'How do I get in touch with Zava Lending support?'),
    (N'Is Zava Lending available in my region?'),
    (N'How long does a typical lending decision take at Zava?'),
    (N'What makes Zava Lending different from other lenders?'),
    (N'Can I manage my Zava loan online?'),
    (N'Does Zava Lending have a mobile app?'),
    (N'How do I raise a complaint with Zava Lending?'),
    (N'What security measures does Zava Lending use to protect my data?'),
    (N'Does Zava Lending offer support if I''m in financial difficulty?'),
    (N'How do I update my contact details with Zava Lending?'),
    (N'Can I book an appointment with a Zava Lending adviser?');

DECLARE @GenericLendingAnswers TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Answer nvarchar(2000));
INSERT @GenericLendingAnswers (Answer) VALUES
    (N'Thanks for reaching out! A Zava Lending adviser can help with that and point you to the right resources.'),
    (N'Great question. Our team is happy to help with general enquiries about Zava Lending and can share the details you need.'),
    (N'Happy to help! You can find general information about Zava Lending on our site, and our advisers are available for anything more specific.'),
    (N'Thanks for getting in touch. Zava Lending aims to keep things simple, and a specialist can assist with your enquiry.'),
    (N'Good question! Our support team can walk you through this and make sure you have what you need.');

DECLARE @FirstNames TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Name nvarchar(20));
INSERT @FirstNames (Name) VALUES
    (N'ava'), (N'liam'), (N'noah'), (N'emma'), (N'olivia'), (N'james'), (N'sophia'), (N'lucas'),
    (N'mia'), (N'ethan'), (N'isla'), (N'mason'), (N'aria'), (N'leo'), (N'zoe'), (N'harper'),
    (N'jack'), (N'nina'), (N'omar'), (N'priya'), (N'diego'), (N'yuki'), (N'sara'), (N'tom'),
    (N'grace'), (N'hugo'), (N'lena'), (N'marco'), (N'ruby'), (N'theo'), (N'chloe'), (N'amir');

DECLARE @LastNames TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Name nvarchar(20));
INSERT @LastNames (Name) VALUES
    (N'smith'), (N'jones'), (N'garcia'), (N'lee'), (N'patel'), (N'nguyen'), (N'brown'), (N'khan'),
    (N'rossi'), (N'kim'), (N'silva'), (N'muller'), (N'novak'), (N'haddad'), (N'okafor'), (N'costa'),
    (N'reed'), (N'walsh'), (N'ortiz'), (N'flynn'), (N'mccoy'), (N'bauer'), (N'dubois'), (N'ivanov');

DECLARE @Domains TABLE (Id int IDENTITY(1, 1) PRIMARY KEY, Domain nvarchar(40));
INSERT @Domains (Domain) VALUES
    (N'example.com'), (N'demomail.test'), (N'fictional.mail'), (N'sample.org'), (N'workshop.example'),
    (N'mailinator.test'), (N'inbox.example');

DECLARE @DeclineReply nvarchar(200) =
    N'I don''t have that information accessible. Do you want to chat with a real person instead?';

DECLARE @ExistingCount     int = (SELECT COUNT(*) FROM @ExistingQuestions);
DECLARE @GenericAnswerCount int = (SELECT COUNT(*) FROM @GenericAnswers);
DECLARE @LoanCount         int = (SELECT COUNT(*) FROM @MissingLoans);
DECLARE @TemplateCount     int = (SELECT COUNT(*) FROM @MissingTemplates);
DECLARE @GenericQCount     int = (SELECT COUNT(*) FROM @GenericQuestions);
DECLARE @GenericACount     int = (SELECT COUNT(*) FROM @GenericLendingAnswers);
DECLARE @FirstNameCount    int = (SELECT COUNT(*) FROM @FirstNames);
DECLARE @LastNameCount     int = (SELECT COUNT(*) FROM @LastNames);
DECLARE @DomainCount       int = (SELECT COUNT(*) FROM @Domains);

/* ---------- Staging ---------------------------------------------------------- */
-- Random indices are computed and MATERIALIZED here (one INSERT per scenario), so each
-- session gets its own independent pick. This avoids the non-correlated
-- "CROSS APPLY (SELECT TOP 1 ... ORDER BY NEWID())" pattern, which SQL Server can
-- evaluate once and reuse for every row (the cause of the earlier repeated values).

CREATE TABLE #Pick
(
    SessionId          uniqueidentifier NOT NULL,
    RequestId          uniqueidentifier NOT NULL,
    OwnerHash          binary(32)       NOT NULL,
    Scenario           int              NOT NULL,
    State              varchar(12)      NOT NULL,
    UserCreatedAt      datetime2(7)     NOT NULL,
    AssistantCreatedAt datetime2(7)     NOT NULL,
    QIdx               int              NULL,   -- @ExistingQuestions (scenario 1)
    AnswerIdx          int              NULL,   -- @GenericAnswers (1) / @GenericLendingAnswers (3)
    LoanIdx            int              NULL,   -- @MissingLoans (scenario 2)
    TemplateIdx        int              NULL,   -- @MissingTemplates (scenario 2)
    GenericQIdx        int              NULL,   -- @GenericQuestions (scenario 3)
    FirstNameIdx       int              NULL,   -- @FirstNames (scenario 2)
    LastNameIdx        int              NULL,   -- @LastNames (scenario 2)
    DomainIdx          int              NULL,   -- @Domains (scenario 2)
    EmailFormat        int              NULL,   -- email style selector (scenario 2)
    EmailNumber        int              NULL    -- optional numeric suffix (scenario 2)
);

-- Scenario 1: supported loan types (generic answer, stays Open).
INSERT #Pick (SessionId, RequestId, OwnerHash, Scenario, State, UserCreatedAt, AssistantCreatedAt, QIdx, AnswerIdx)
SELECT
    NEWID(), NEWID(),
    CONVERT(binary(32), HASHBYTES('SHA2_256', CONVERT(nvarchar(36), NEWID()))),
    1, 'Open',
    base.UserCreatedAt,
    DATEADD(second, (ABS(CHECKSUM(NEWID())) % 18) + 3, base.UserCreatedAt),
    (ABS(CHECKSUM(NEWID())) % @ExistingCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @GenericAnswerCount) + 1
FROM (SELECT TOP (@SupportedCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects) t
CROSS APPLY (SELECT DATEADD(second, -(ABS(CHECKSUM(NEWID())) % 2592000), SYSUTCDATETIME())) base(UserCreatedAt);

-- Scenario 2: unsupported loan types (declined + escalated).
INSERT #Pick (SessionId, RequestId, OwnerHash, Scenario, State, UserCreatedAt, AssistantCreatedAt,
              LoanIdx, TemplateIdx, FirstNameIdx, LastNameIdx, DomainIdx, EmailFormat, EmailNumber)
SELECT
    NEWID(), NEWID(),
    CONVERT(binary(32), HASHBYTES('SHA2_256', CONVERT(nvarchar(36), NEWID()))),
    2, 'Escalated',
    base.UserCreatedAt,
    DATEADD(second, (ABS(CHECKSUM(NEWID())) % 18) + 3, base.UserCreatedAt),
    (ABS(CHECKSUM(NEWID())) % @LoanCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @TemplateCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @FirstNameCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @LastNameCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @DomainCount) + 1,
    ABS(CHECKSUM(NEWID())) % 6,
    (ABS(CHECKSUM(NEWID())) % 90) + 10
FROM (SELECT TOP (@UnsupportedCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects) t
CROSS APPLY (SELECT DATEADD(second, -(ABS(CHECKSUM(NEWID())) % 2592000), SYSUTCDATETIME())) base(UserCreatedAt);

-- Scenario 3: general Zava Lending questions (generic answer, then escalated).
INSERT #Pick (SessionId, RequestId, OwnerHash, Scenario, State, UserCreatedAt, AssistantCreatedAt, GenericQIdx, AnswerIdx,
              FirstNameIdx, LastNameIdx, DomainIdx, EmailFormat, EmailNumber)
SELECT
    NEWID(), NEWID(),
    CONVERT(binary(32), HASHBYTES('SHA2_256', CONVERT(nvarchar(36), NEWID()))),
    3, 'Escalated',
    base.UserCreatedAt,
    DATEADD(second, (ABS(CHECKSUM(NEWID())) % 18) + 3, base.UserCreatedAt),
    (ABS(CHECKSUM(NEWID())) % @GenericQCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @GenericACount) + 1,
    (ABS(CHECKSUM(NEWID())) % @FirstNameCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @LastNameCount) + 1,
    (ABS(CHECKSUM(NEWID())) % @DomainCount) + 1,
    ABS(CHECKSUM(NEWID())) % 6,
    (ABS(CHECKSUM(NEWID())) % 90) + 10
FROM (SELECT TOP (@GenericCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects) t
CROSS APPLY (SELECT DATEADD(second, -(ABS(CHECKSUM(NEWID())) % 2592000), SYSUTCDATETIME())) base(UserCreatedAt);

/* ---------- Resolve content from the materialized picks ---------------------- */

CREATE TABLE #Seed
(
    SessionId         uniqueidentifier NOT NULL,
    RequestId         uniqueidentifier NOT NULL,
    OwnerHash         binary(32)       NOT NULL,
    Scenario          int              NOT NULL,
    State             varchar(12)      NOT NULL,
    UserContent       nvarchar(400)    NOT NULL,
    AssistantContent  nvarchar(2000)   NOT NULL,
    Email             nvarchar(254)    NULL,
    UserCreatedAt     datetime2(7)     NOT NULL,
    AssistantCreatedAt datetime2(7)    NOT NULL
);

INSERT #Seed (SessionId, RequestId, OwnerHash, Scenario, State, UserContent, AssistantContent, Email, UserCreatedAt, AssistantCreatedAt)
SELECT
    p.SessionId, p.RequestId, p.OwnerHash, p.Scenario, p.State,
    CASE p.Scenario
        WHEN 1 THEN eq.Question
        WHEN 2 THEN REPLACE(mt.Template, N'{loan}', ml.Loan)
        WHEN 3 THEN gq.Question
    END,
    CASE p.Scenario
        WHEN 1 THEN ga.Answer
        WHEN 2 THEN @DeclineReply
        WHEN 3 THEN gla.Answer
    END,
    CASE WHEN p.Scenario IN (2, 3) THEN
        LOWER(
            CASE p.EmailFormat
                WHEN 0 THEN fn.Name + N'.' + ln.Name
                WHEN 1 THEN fn.Name + ln.Name
                WHEN 2 THEN fn.Name + N'_' + ln.Name
                WHEN 3 THEN LEFT(fn.Name, 1) + ln.Name
                WHEN 4 THEN fn.Name + N'.' + ln.Name + CAST(p.EmailNumber AS nvarchar(2))
                ELSE ln.Name + N'.' + fn.Name
            END
            + N'@' + d.Domain)
    END,
    p.UserCreatedAt, p.AssistantCreatedAt
FROM #Pick p
LEFT JOIN @ExistingQuestions     eq  ON p.Scenario = 1 AND eq.Id  = p.QIdx
LEFT JOIN @GenericAnswers        ga  ON p.Scenario = 1 AND ga.Id  = p.AnswerIdx
LEFT JOIN @MissingLoans          ml  ON p.Scenario = 2 AND ml.Id  = p.LoanIdx
LEFT JOIN @MissingTemplates      mt  ON p.Scenario = 2 AND mt.Id  = p.TemplateIdx
LEFT JOIN @FirstNames            fn  ON p.Scenario IN (2, 3) AND fn.Id  = p.FirstNameIdx
LEFT JOIN @LastNames             ln  ON p.Scenario IN (2, 3) AND ln.Id  = p.LastNameIdx
LEFT JOIN @Domains               d   ON p.Scenario IN (2, 3) AND d.Id   = p.DomainIdx
LEFT JOIN @GenericQuestions      gq  ON p.Scenario = 3 AND gq.Id  = p.GenericQIdx
LEFT JOIN @GenericLendingAnswers gla ON p.Scenario = 3 AND gla.Id = p.AnswerIdx;

DROP TABLE #Pick;

/* ---------- Load target tables ---------------------------------------------- */

INSERT dbo.ChatSessions (SessionId, OwnerHash, CreatedAt, UpdatedAt, State, NextSequence)
SELECT SessionId, OwnerHash, UserCreatedAt, AssistantCreatedAt, State, 3
FROM #Seed;

INSERT dbo.ChatMessages (MessageId, SessionId, RequestId, Sequence, Role, Content, CreatedAt, Status, SourcesJson, AttemptsJson)
SELECT NEWID(), s.SessionId, s.RequestId, m.Sequence, m.Role, m.Content, m.CreatedAt, 'Completed', N'[]', N'[]'
FROM #Seed s
CROSS APPLY (VALUES
    (1, 'user',      s.UserContent,      s.UserCreatedAt),
    (2, 'assistant', s.AssistantContent, s.AssistantCreatedAt)
) m(Sequence, Role, Content, CreatedAt);

-- Escalations for scenarios 2 and 3. TranscriptJson matches the app's contract (version 1, camelCase).
INSERT dbo.ChatEscalations (EscalationId, SessionId, RequestId, Email, CreatedAt, ContractVersion, TranscriptJson)
SELECT
    NEWID(), s.SessionId, s.RequestId, s.Email, s.AssistantCreatedAt, 1,
    N'{"version":1,"sessionId":"' + LOWER(CONVERT(nvarchar(36), s.SessionId))
    + N'","messages":[{"sequence":1,"requestId":"' + LOWER(CONVERT(nvarchar(36), s.RequestId))
    + N'","role":"user","content":"' + STRING_ESCAPE(s.UserContent, 'json')
    + N'","createdAt":"' + CONVERT(nvarchar(27), s.UserCreatedAt, 127) + N'Z'
    + N'","status":"' + CASE WHEN s.Scenario = 2 THEN N'Failed' ELSE N'Completed' END + N'","sources":[]},{"sequence":2,"requestId":"' + LOWER(CONVERT(nvarchar(36), s.RequestId))
    + N'","role":"assistant","content":"' + STRING_ESCAPE(s.AssistantContent, 'json')
    + N'","createdAt":"' + CONVERT(nvarchar(27), s.AssistantCreatedAt, 127) + N'Z'
    + N'","status":"Completed","sources":[]}]}'
FROM #Seed s
WHERE s.Scenario IN (2, 3);

DROP TABLE #Seed;

COMMIT;

PRINT CONCAT('Seed complete: ', @SupportedCount, ' supported-loan sessions (Open), ',
             @UnsupportedCount, ' unsupported-loan sessions (Escalated), ',
             @GenericCount, ' general Zava Lending sessions (Escalated).');
