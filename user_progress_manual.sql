-- User Progress reuses the existing Adventure Game Npc_challenges table.
-- Run manually only after confirming these indexes do not already exist.
-- This script is intentionally not executed by the application.

CREATE INDEX IF NOT EXISTS "IX_Npc_challenges_StudentId_Difficulty_CompletedAt"
    ON "Npc_challenges" ("StudentId", "Difficulty", "CompletedAt" DESC)
    WHERE "State" = 'Completed';

-- AttemptId is already unique in the existing EF model. This optional covering
-- index makes authenticated-session lookups explicit for database tooling.
CREATE INDEX IF NOT EXISTS "IX_Npc_challenges_StudentId_AttemptId"
    ON "Npc_challenges" ("StudentId", "AttemptId");
