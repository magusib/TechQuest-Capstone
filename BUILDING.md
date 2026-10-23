# Complete Step-by-Step Guide: Turn Your TechQuest Project into a Web Application and Android APK

This guide is for this repository's current structure:

- `TechQuestFrontend/` is a Godot 4.7.2 **C#/.NET** game.
- `TechQuestBackend/` is an ASP.NET Core **.NET 10** API backed by PostgreSQL.

The backend can serve both browser and mobile clients. The Godot C# game can be exported to Android, but **Godot 4 does not export C# projects to HTML5/Web**. A browser version therefore needs a separate web client (or a substantial port of the Godot game to GDScript). There is no one-click setting that turns this C# project into a browser game.

## 1. Install the tools

On Windows, install:

1. **Godot 4.7.2 .NET edition**. Use the .NET build, not the standard build.
2. The **Godot 4.7.2 export templates**. In Godot, open **Editor > Manage Export Templates** and install the matching templates.
3. The .NET SDKs needed by the projects:
   - .NET 8 for the Godot desktop target.
   - .NET 9 for the Godot Android target.
   - .NET 10 for the backend.

   Install SDKs side by side from the [.NET download page](https://dotnet.microsoft.com/download/dotnet). The target frameworks are declared in [techquest.csproj](TechQuestFrontend/techquest.csproj) and [TechQuestBackend.csproj](TechQuestBackend/TechQuestBackend.csproj).
4. **PostgreSQL**, with a local database created for TechQuest.
5. For Android exports: **OpenJDK 17** and Android Studio's Android SDK. In Android Studio's SDK Manager, install Android SDK Platform 35 or later, Android SDK Build-Tools, Platform-Tools, and Command-line Tools. See [Godot's Android export requirements](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html).
6. For the browser client: **Node.js LTS**.

Confirm the .NET SDKs are available in PowerShell:

```powershell
dotnet --list-sdks
```

## 2. Configure and run the backend

1. Create a PostgreSQL database (for example, `techquestdb`) and note its host, port, database name, username, and password.
2. From the repository root, initialize local .NET user secrets and set the database connection string and a private JWT signing key:

   ```powershell
   dotnet user-secrets init --project TechQuestBackend
   dotnet user-secrets set --project TechQuestBackend "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=techquestdb;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
   dotnet user-secrets set --project TechQuestBackend "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET"
   ```

   Keep real credentials out of source control. The application reads these values through configuration; user secrets override the checked-in settings for local development.
3. Start the API:

   ```powershell
   dotnet run --project TechQuestBackend --launch-profile http
   ```

   On first startup, the backend applies its EF Core migrations and seeds initial game data. With the included launch profile, the API listens on `http://localhost:5000`; Swagger is available at `http://localhost:5000/swagger` while running in Development.
4. Verify that the database connection succeeded and the API responds before starting either client.

## 3. Create a browser version

### Choose the web-client approach

For a browser application, create a separate web frontend (for example, React with TypeScript and Vite) and use the existing backend API. The Godot game's scenes and C# scripts do not automatically become web pages; recreate the required login, account, quest, leaderboard, shop, and game UI in the web frontend. Use Swagger and the existing API controllers to understand available operations.

If the goal is to run the same game inside a browser, the alternative is to create a separate Godot project using GDScript and port the C# gameplay code to GDScript. Godot 4's HTML5 export supports GDScript, not Godot C#.

### Scaffold and run a web frontend

1. From the repository root, scaffold a React + TypeScript app:

   ```powershell
   npm create vite@latest TechQuestWeb -- --template react-ts
   cd TechQuestWeb
   npm install
   ```

2. Add an environment variable file named `.env.local` in `TechQuestWeb`:

   ```text
   VITE_API_BASE_URL=http://localhost:5000/api
   ```

   Read this value from the web app instead of scattering API URLs through components. Do not put secrets in `VITE_` variables; browser variables are public.
3. Implement the browser UI and call the API at the base URL above. Confirm the endpoints and request/response shapes in Swagger (`http://localhost:5000/swagger`). Store authentication tokens safely and handle expired sessions and API errors.
4. Start the frontend while the API and PostgreSQL are running:

   ```powershell
   npm run dev
   ```

   Open the local URL printed by Vite and test the complete browser flow, including API requests.
5. Configure CORS before browser testing or deployment. The current backend policy in [Program.cs](TechQuestBackend/Program.cs) only allows `http://localhost` and `https://localhost`; it does not automatically allow Vite's development origin (usually `http://localhost:5173`) or your production website. Add the exact trusted origins your web app uses; do not use an unrestricted wildcard for an authenticated API.
6. For production, deploy the web frontend as a static site and deploy the API separately (or configure a reverse proxy). Set `VITE_API_BASE_URL` to the deployed HTTPS API URL, add the deployed website origin to the API's CORS policy, and use a managed PostgreSQL database. Set the production connection string and JWT key in the hosting provider's secret/environment configuration, not in source files.

   Before making the API public, replace the development JWT key and review the backend's startup seed accounts in [Program.cs](TechQuestBackend/Program.cs). Change or remove any sample credentials and ensure production secrets are configured; do not rely on the fallback JWT key in code.

## 4. Prepare the Godot game for Android

### Make API addresses reachable

The Godot clients currently use hard-coded `http://localhost:5000` addresses. On an Android device, `localhost` refers to the device itself, not your development computer. Update the API base URL constants before building:

- [AccountApiClient.cs](TechQuestFrontend/scene/Core/AccountApiClient.cs)
- [QuestApiClient.cs](TechQuestFrontend/scene/Core/QuestApiClient.cs)
- [NpcApiClient.cs](TechQuestFrontend/scene/Core/NpcApiClient.cs)
- [LeaderboardBase.cs](TechQuestFrontend/scene/Core/LeaderboardBase.cs)
- [ShopApiClient.cs](TechQuestFrontend/scene/Core/ShopApiClient.cs)
- [RoomApiClient.cs](TechQuestFrontend/scene/Core/RoomApiClient.cs)
- [MiniGameApiClient.cs](TechQuestFrontend/scene/Minigame/MiniGameApiClient.cs)
- The authentication/login scripts under `TechQuestFrontend/scene/UserLogin/` and registration script at `TechQuestFrontend/scene/UserManagement/ProfessorRegister.cs`.

Use the correct address for the test setup:

- Android Emulator: `http://10.0.2.2:5000/api` (the emulator's route to the host computer).
- Physical Android device: `http://YOUR_COMPUTER_LAN_IP:5000/api`. Put the device and computer on the same network, bind the backend to a network interface, and allow the port through the computer's firewall. For example:

  ```powershell
  dotnet run --project TechQuestBackend --urls http://0.0.0.0:5000
  ```

For a released APK, use the API's public **HTTPS** URL instead. Do not ship an APK pointed at a development machine or an unsecured public HTTP endpoint. Android may block cleartext HTTP depending on the export configuration and Android version.

The API's current CORS policy is relevant to web browsers, not native Godot Android HTTP requests. Still, production CORS must be configured for the browser app's exact origin as described above.

### Configure Android export

1. Open `TechQuestFrontend/project.godot` in Godot 4.7.2 .NET and let the editor import the project. Build the C# project in the editor and resolve any build errors before exporting.
2. In Godot, open **Editor > Editor Settings > Export > Android** and set:
   - **Java SDK Path** to the OpenJDK 17 installation directory.
   - **Android SDK Path** to the Android SDK directory (commonly `%LOCALAPPDATA%\Android\Sdk`).
3. Connect an Android device with USB debugging enabled, or start an Android emulator. Confirm the device is detected:

   ```powershell
   adb devices
   ```

4. Open **Project > Export**, add an **Android** preset, and set a unique package identifier such as `com.yourteam.techquest`. Set the app name, version, icon, and screen orientation as required.
5. Enable the **Internet** permission in the Android preset so the game can contact the API. Keep the API HTTPS for production; only enable cleartext HTTP for local testing if necessary.
6. Select the Android export preset and export a debug APK to a known location, for example `builds/techquest-debug.apk`. Create the `builds` directory first if it does not exist. Export templates must match the installed Godot version.
7. Install the APK on the emulator/device and test gameplay as well as account, quest, leaderboard, and other API-backed features. Check the Godot output and Android logs if requests fail. Verify the API address, that the backend is listening/reachable, and that the device has internet/network access.

Godot's C# Android export support is available but has platform-specific limitations; review the [C# platform support notes](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html#platforms) if an export fails.

### Prepare a release build

1. Switch the export preset from debug to release and create a private release keystore using the Android/Godot signing workflow.
2. Configure the release keystore credentials in the Android preset or CI secret store. Never commit the keystore or its passwords; keep a secure backup because updates must be signed with the same key.
3. Export and install a release APK, then test it on a clean device with the production HTTPS API.
4. Keep the package identifier stable for updates and increment the version for each release.

An APK is suitable for direct installation/testing. Google Play generally expects an **AAB (Android App Bundle)**; follow Godot's [Gradle build and Android publishing guide](https://docs.godotengine.org/en/stable/tutorials/export/android_gradle_build.html) if publishing through Google Play.

## 5. Common problems

| Symptom | Check |
|---|---|
| API fails at startup | PostgreSQL is running, the database exists, and `ConnectionStrings:DefaultConnection` is correct. Review backend console output for migration errors. |
| Android says connection refused | Do not use `localhost` on-device. Use `10.0.2.2` for the emulator or the computer's LAN IP for a physical device; ensure the backend is bound to the network and the firewall permits the port. |
| Browser reports a CORS error | Add the exact browser origin (including scheme and port) to the backend CORS policy. CORS is separate from whether the API is reachable. |
| Godot Android export fails | Confirm the .NET Godot build and export templates are version 4.7.2, Java is JDK 17, Android SDK paths are correct, Android build-tools are installed, and the C# project builds first. |
| Web browser cannot run the Godot game export | This project uses Godot C#. Godot 4 currently does not export C# projects to HTML5; build a separate web client or port the game to GDScript. |
| API works locally but not in a release APK | Check the release API URL, HTTPS certificate, Internet permission, and that no client constant still points to localhost. |

## References

- [Godot: Exporting for the Web](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html)
- [Godot: Exporting for Android](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Godot: C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html#platforms)