-- TechQuest multiple-choice question hardening (PostgreSQL)
-- The current schema already stores choices in Question.AnswerChoices (jsonb),
-- so no new columns or data migration are required. Existing rows are preserved.
-- New and edited questions must contain four choices and an A-D answer key.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'CK_Question_MultipleChoiceFormat'
          AND conrelid = '"Question"'::regclass
    ) THEN
        ALTER TABLE "Question"
        ADD CONSTRAINT "CK_Question_MultipleChoiceFormat"
        CHECK (
            CASE
                WHEN jsonb_typeof("AnswerChoices") = 'array'
                THEN jsonb_array_length("AnswerChoices") = 4
                ELSE false
            END
            AND upper(trim("CorrectAnswer")) IN ('A', 'B', 'C', 'D')
        ) NOT VALID;
    END IF;
END $$;

-- Optional verification before/after applying the script:
-- SELECT "QuestionId", "AnswerChoices", "CorrectAnswer"
-- FROM "Question"
-- WHERE jsonb_typeof("AnswerChoices") <> 'array'
--    OR (jsonb_typeof("AnswerChoices") = 'array' AND jsonb_array_length("AnswerChoices") <> 4)
--    OR upper(trim("CorrectAnswer")) NOT IN ('A', 'B', 'C', 'D');
