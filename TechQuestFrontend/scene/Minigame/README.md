# TechQuest mini-games

The three editable mini-games live in this folder:

- `MatchThePairs.tscn` and `MatchThePairs.cs`
- `FallingDuck.tscn` and `FallingDuck.cs`
- `QuickTapChallenge.tscn` and `QuickTapChallenge.cs`

Reusable pieces are `MemoryCard.tscn`, `FallingDuckTarget.tscn`, and
`TapTarget.tscn`. Each game starts a 30-second attempt and can also be opened
directly in the editor for a practice round. To receive a tip or record a
failure penalty, launch the game by clicking or tapping an assigned NPC on a
student map.

## Assign a game to an NPC

1. Start the TechQuest backend and sign in as an administrator.
2. Deploy an NPC, then open **Manage Deployed NPCs** for that map.
3. Use **Manage** on the specific deployed NPC card. Each deployment has its own
   subject, question, mini-game, correct answer, answer tip, and question time
   limit. Students type their answer; answer choices are not used for now. Choose
   **Match the Pairs**, **Falling Duck**,
   or **Quick Tap Challenge** in its mini-game list.
4. Save the NPC configuration. The game uses the NPC's configured question and
   answer tip; students do not choose a game themselves.
5. On a student map, click or tap the deployed NPC to review its subject and
   accept the challenge. The assigned mini-game runs as a 30-second overlay;
   its result gives the answer tip whether the student wins or loses. Proceeding
   opens that NPC's configured question and timer.

The backend creates missing mini-game records with the stable game types
`MatchThePairs`, `FallingDuck`, and `QuickTapChallenge`. The deployment-specific question stores the selected `MiniGameId` and a foreign
key to that deployed NPC instance. Existing active deployments receive their
previous NPC-level question configuration when the database migration runs.

## Edit scenes and assets

Open the main scenes directly in Godot:

- `MatchThePairs.tscn`
- `FallingDuck.tscn`
- `QuickTapChallenge.tscn`

The complete backgrounds, panels, labels, buttons, containers, result overlays,
player, collision shapes, and gameplay areas are scene nodes. Select them in
the Scene panel and change their layout, anchors, fonts, colors, textures,
styles, and sizes in the Inspector. Backgrounds use editable `GradientTexture2D`
resources by default; replace the `Background` texture to use an image.

The reusable scenes are `MemoryCard.tscn`, `FallingDuckTarget.tscn`, and
`TapTarget.tscn`. Their contents and visual style are editable independently.
The memory-card back and target image are texture resources; the quick-tap
target currently uses `Items/Characters/Character 1.png` as its default image.
Replace that `CharacterImage` texture in `TapTarget.tscn`, or set the main
scene's `TargetTexture` override. The main scene also exports gameplay tuning
such as card-pair count, grid columns, points, movement speed, duck spawn
interval, target score, target size, and target display duration.

Each game uses a 30-second timer for both practice and assigned attempts. The
question timer is separate and uses the duration configured by the administrator.
The client sends the attempt identifier returned by the backend; the server
remains authoritative for balances and question rewards.

## Result handling

`POST /api/Npcs/deployments/{id}/challenge-start` creates a student-specific,
single-use attempt. `POST /api/GameResults/mini-game-attempt` records the
mini-game outcome and returns the assigned answer tip on both win and loss.
Losses deduct up to 5 coins once. `GET /api/Npcs/deployments/{id}/student-question`
returns the configured question without the correct answer. Students submit a
typed answer, which the backend compares with the configured correct answer.
The backend can support configured answer choices later if the question format
changes.
The backend starts the configured question timer and
`POST /api/GameResults/question-attempt` evaluates the submitted answer or
server-detected timeout, applies difficulty-specific rewards or penalties, and
returns the correct answer and confirmed balances. Database transactions and
unique per-student deployment records make retries idempotent and hide completed
NPC deployments only from the student who completed them.

The question result screen returns to the map scene that initiated the
challenge. The map remains loaded while both overlays are shown, so the player
and world position are preserved without creating a second player instance.

The legacy game-result endpoint no longer accepts client-supplied coin or
point rewards. The mini-game result endpoint is the only path used by these
scenes. Student question-list and student-question responses omit the correct answer;
the question-result endpoint reveals it only after finalization.

The backend applies pending Entity Framework migrations at startup. To apply
the database change manually from the repository root, run:

```powershell
dotnet ef database update --project .\TechQuestBackend\TechQuestBackend.csproj
```

## Test locally

1. Build the frontend:

   ```powershell
   dotnet build .\TechQuestFrontend\techquest.csproj
   ```

2. Build and start the backend with its configured PostgreSQL connection:

   ```powershell
   dotnet build .\TechQuestBackend\TechQuestBackend.csproj
   dotnet run --project .\TechQuestBackend\TechQuestBackend.csproj
   ```

3. In Godot, open and run each main scene directly to check the timer,
   gameplay, restart, close buttons, and editable scene hierarchy. These are
   practice attempts and do not make backend transactions.
4. Configure a deployed NPC with a question and correct answer. Test typed
   answers from a student session on each difficulty map.
5. Verify mini-game wins and losses both show the tip, the question timer uses
   its admin-configured duration, and correct, incorrect, and timed-out answers
   show the expected result and server-confirmed balances.
6. Return to the map and confirm the player stays at the challenge position and
   only that student's completed NPC disappears. Retry network requests and
   confirm rewards and deductions are not applied twice.
