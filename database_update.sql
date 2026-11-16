-- TechQuest multiple-choice question hardening (PostgreSQL)
-- The current schema already stores choices in Question.AnswerChoices (jsonb),
-- so no new columns or data migration are required. Existing rows are preserved.
-- New and edited questions must contain four choices and a complete answer text.
-- This replaces the earlier A-D answer-key constraint without changing any rows.

ALTER TABLE "Question"
DROP CONSTRAINT IF EXISTS "CK_Question_MultipleChoiceFormat";

ALTER TABLE "Question"
ADD CONSTRAINT "CK_Question_MultipleChoiceFormat"
CHECK (
    CASE
        WHEN jsonb_typeof("AnswerChoices") = 'array'
        THEN jsonb_array_length("AnswerChoices") = 4
        ELSE false
    END
    AND length(trim("CorrectAnswer")) > 0
) NOT VALID;

-- Optional verification before/after applying the script:
-- SELECT "QuestionId", "AnswerChoices", "CorrectAnswer"
-- FROM "Question"
-- WHERE jsonb_typeof("AnswerChoices") <> 'array'
--    OR (jsonb_typeof("AnswerChoices") = 'array' AND jsonb_array_length("AnswerChoices") <> 4)
--    OR length(trim("CorrectAnswer")) = 0;
