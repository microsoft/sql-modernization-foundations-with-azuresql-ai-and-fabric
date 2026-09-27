-- Removes ONLY the rows created by seed-chat-scenarios.sql.
-- Seeded rows use NEWID() keys, so they are identified by their known content:
-- a session is treated as seeded when its user message and assistant reply both
-- match the reference pools used by the seed script. Real conversations that do not
-- match these exact strings are left untouched.
-- Deletes in FK order: ChatEscalations -> ChatMessages -> ChatSessions.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
    OR OBJECT_ID(N'dbo.ChatSessions', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
    OR OBJECT_ID(N'dbo.ChatEscalations', N'U') IS NULL
    THROW 51020, 'Connect to the workshop Fabric SQL database first.', 1;

BEGIN TRANSACTION;

/* ---------- Reference pools (must mirror seed-chat-scenarios.sql) ------------- */

DECLARE @KnownUser TABLE (Content nvarchar(400) NOT NULL);
INSERT @KnownUser (Content) VALUES
    (N'Can you tell me more about the Zava Auto Loan repayment options?'),
    (N'What documents do I need for an Auto Loan application?'),
    (N'How long can I finance a car with the Auto Loan?'),
    (N'Is the Auto Loan available for used vehicles as well?'),
    (N'What is the maximum amount I can borrow with an Auto Loan?'),
    (N'Can I refinance an existing car loan with Zava?'),
    (N'Does the Auto Loan cover electric or hybrid vehicles?'),
    (N'Are there any early settlement options on the Auto Loan?'),
    (N'I''d like more details about the Personal Loan terms.'),
    (N'Can the Personal Loan be used to consolidate other borrowing?'),
    (N'What monthly repayment should I expect on a Personal Loan?'),
    (N'Are there early repayment charges on the Personal Loan?'),
    (N'How is eligibility assessed for a Personal Loan?'),
    (N'What is the minimum and maximum term for a Personal Loan?'),
    (N'Can I apply for a Personal Loan with a joint applicant?'),
    (N'How quickly are Personal Loan funds released after approval?'),
    (N'Tell me more about the Small Business Loan options.'),
    (N'What can the Small Business Loan be used for?'),
    (N'Does the Small Business Loan require collateral?'),
    (N'How long is the term on a Small Business Loan?'),
    (N'Can a newly formed business apply for the Small Business Loan?'),
    (N'What paperwork does a Small Business Loan application need?'),
    (N'Is there a limit on how much a business can borrow?'),
    (N'Can I use the Small Business Loan to buy equipment?'),
    (N'I want to learn more about the Home Improvement Loan.'),
    (N'Can the Home Improvement Loan cover a kitchen renovation?'),
    (N'What is the borrowing limit on a Home Improvement Loan?'),
    (N'Are contractor quotes required for a Home Improvement Loan?'),
    (N'How quickly are Home Improvement Loans funded once approved?'),
    (N'Can I use a Home Improvement Loan for an extension?'),
    (N'Does the Home Improvement Loan cover energy-efficiency upgrades?'),
    (N'What repayment terms are available on a Home Improvement Loan?');

-- Scenario 2 user content = every template x loan combination the seed script can produce.
DECLARE @MissingLoans TABLE (Loan nvarchar(60));
INSERT @MissingLoans (Loan) VALUES
    (N'student loan'), (N'payday loan'), (N'boat loan'), (N'RV loan'), (N'motorcycle loan'),
    (N'jumbo mortgage'), (N'reverse mortgage'), (N'home equity line of credit'), (N'construction loan'),
    (N'bridge loan'), (N'farm loan'), (N'medical loan'), (N'wedding loan'), (N'travel loan'),
    (N'title loan'), (N'land loan'), (N'commercial real estate loan'), (N'equipment financing loan'),
    (N'invoice financing facility'), (N'solar energy loan'), (N'credit-builder loan'), (N'aircraft loan'),
    (N'franchise loan'), (N'working capital loan'), (N'livestock loan');

DECLARE @MissingTemplates TABLE (Template nvarchar(200));
INSERT @MissingTemplates (Template) VALUES
    (N'Do you offer a {loan}?'),
    (N'I''m interested in a {loan}. Can Zava help me with that?'),
    (N'What are the current rates on your {loan}?'),
    (N'Can I apply for a {loan} through Zava Lending?'),
    (N'Does Zava provide {loan} options?'),
    (N'I''d like some information about a {loan}.'),
    (N'Is a {loan} something Zava offers?');

INSERT @KnownUser (Content)
SELECT REPLACE(tpl.Template, N'{loan}', l.Loan)
FROM @MissingTemplates tpl
CROSS JOIN @MissingLoans l;

-- Scenario 3 user content = general Zava Lending questions (not product-specific).
INSERT @KnownUser (Content) VALUES
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

DECLARE @KnownAssistant TABLE (Content nvarchar(2000) NOT NULL);
INSERT @KnownAssistant (Content) VALUES
    (N'Thanks for your interest in our lending options. A Zava specialist can walk you through the details, eligibility, and next steps for this loan.'),
    (N'That''s a great question. Our lending team can share the specific terms and help you find the right fit for your needs.'),
    (N'I''d be happy to help. This loan has flexible options, and a Zava advisor can provide tailored guidance based on your situation.'),
    (N'Good question! There are several features available for this loan. A member of our lending team can review the details and answer any follow-up questions.'),
    (N'Zava offers competitive terms on this loan. For personalized figures and eligibility, our specialists are ready to assist you.'),
    (N'I don''t have that information accessible. Do you want to chat with a real person instead?'),
    (N'Thanks for reaching out! A Zava Lending adviser can help with that and point you to the right resources.'),
    (N'Great question. Our team is happy to help with general enquiries about Zava Lending and can share the details you need.'),
    (N'Happy to help! You can find general information about Zava Lending on our site, and our advisers are available for anything more specific.'),
    (N'Thanks for getting in touch. Zava Lending aims to keep things simple, and a specialist can assist with your enquiry.'),
    (N'Good question! Our support team can walk you through this and make sure you have what you need.');

/* ---------- Identify seeded sessions ----------------------------------------- */

CREATE TABLE #ToDelete (SessionId uniqueidentifier NOT NULL PRIMARY KEY);

INSERT #ToDelete (SessionId)
SELECT s.SessionId
FROM dbo.ChatSessions s
WHERE EXISTS (
        SELECT 1 FROM dbo.ChatMessages um
        JOIN @KnownUser ku ON ku.Content = um.Content
        WHERE um.SessionId = s.SessionId AND um.Role = 'user')
  AND EXISTS (
        SELECT 1 FROM dbo.ChatMessages am
        JOIN @KnownAssistant ka ON ka.Content = am.Content
        WHERE am.SessionId = s.SessionId AND am.Role = 'assistant');

/* ---------- Delete in FK order ----------------------------------------------- */

DELETE e
FROM dbo.ChatEscalations e
JOIN #ToDelete d ON d.SessionId = e.SessionId;
DECLARE @escalations int = @@ROWCOUNT;

DELETE m
FROM dbo.ChatMessages m
JOIN #ToDelete d ON d.SessionId = m.SessionId;
DECLARE @messages int = @@ROWCOUNT;

DELETE s
FROM dbo.ChatSessions s
JOIN #ToDelete d ON d.SessionId = s.SessionId;
DECLARE @sessions int = @@ROWCOUNT;

DROP TABLE #ToDelete;

COMMIT;

PRINT CONCAT('Cleanup complete. Sessions removed: ', @sessions,
             ', messages removed: ', @messages,
             ', escalations removed: ', @escalations, '.');
