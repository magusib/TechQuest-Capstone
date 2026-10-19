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
   subject, question, mini-game, correct answer, and answer tip. Choose
   **Match the Pairs**, **Falling Duck**,
   or **Quick Tap Challenge** in its mini-game list.
4. Save the NPC configuration. The game uses the NPC's configured question and
   answer tip; students do not choose a game themselves.
5. On a student map, click or tap the deployed NPC to open the assigned game as
   an overlay. Closing the overlay returns to the same map instance.

The backend creates missing mini-game records with the stable game types
`MatchThePairs`, `FallingDuck`, and `QuickTapChallenge`. The deployment-specific question stores the selected `MiniGameId` and a foreign
key to that deployed NPC instance. Existing active deployments receive their
previous NPC-level question configuration when the database migration runs.

## Edit scenes and assets

Open a game scene and select its root node to edit exported settings:

- **Match the Pairs:** `CharacterImages`, `CardBackTexture`,
  `BackgroundTexture` / `BackgroundColor`, `CardSize`, `CardSpacing`,
  `GridColumns`, `NumberOfPairs`, `MismatchDelay`, and
  `PointsPerMatchedPair`. The number of playable pairs is limited by the
  non-empty character images supplied.
- **Falling Duck:** `PlayerTexture`, `DuckTexture`, background, player and duck
  sizes, `PlayerSpeed`, `DuckSpeed`, `SpawnInterval`, and `DuckScene`.
- **Quick Tap Challenge:** `TargetTexture`, background, `TargetScore`,
  `PointsPerTap`, `TargetSize`, and `TapTargetScene`.

The provided pair cards use the existing character images under
`Items/Characters`. Replace them in the exported image array or assign other
`Texture2D` resources in the Inspector. Missing backgrounds use a solid color;
missing duck or target images use simple colored shapes/buttons so the game
remains visible and playable.

The attempt duration is intentionally fixed at 30 seconds. Keep the exported
settings for difficulty and assets; changing the duration requires changing
the timer logic in each game script.

## Result handling

The frontend sends each attempt with a unique GUID to
`POST /api/GameResults/mini-game-attempt`. The backend verifies that the
requested game type is assigned to the question and updates the authenticated
student's profile inside a database transaction. A unique database index on
the attempt GUID makes retries idempotent. Successful attempts return that
question's answer tip; failed attempts deduct up to 5 coins and return the
confirmed balance. If the balance is below 5, the deduction is limited to the
available balance so it cannot become negative.

The legacy game-result endpoint no longer accepts client-supplied coin or
point rewards. The mini-game result endpoint is the only path used by these
scenes. Student question-list responses omit the correct answer and answer tip;
the mini-game endpoint returns the tip only after a recorded win.

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

3. In Godot, run each mini-game scene directly to check its 30-second timer,
   gameplay, restart, and close buttons. These are practice attempts and do not
   make backend transactions.
4. Configure an NPC with each game in turn, then sign in as a student and start
   each game from its NPC. Verify a win displays the assigned answer tip and a
   failure displays the confirmed coin balance.
5. Retry a failed result request using **Retry result** if the network is
   unavailable. The retry reuses the same attempt GUID and cannot apply a
   second deduction.
