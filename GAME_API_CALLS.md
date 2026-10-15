# HS_TraderProtection — game calls (from Source)

Not extracted from the game DLL. This is `HS_TraderProtection/Source`. Unity / System / Harmony / our types skipped.

3.2 vs 3.3: `Constants.cVersionInformation` Minor 20 = 3.2, Minor 30 = 3.3. Same DLL. Net packages use compiled `GetLength` on 3.2 and an emitted subclass on 3.3. Trade lock is `EntityTrader.CanLockOnServer` (3.2) vs `OnLockRequestServer` (3.3). Ammo tracking reads `ItemInventoryData.holdingEntity` as a field (3.2) or property (3.3).

## Inherits
- `ConsoleCmdAbstract`
- `IModApi`
- `NetPackage`
- `XUiController`

## HarmonyPatch / typeof game types
- `EAIApproachAndAttackTarget`
- `EntityAlive`
- `EntityTrader`
- `ItemActionRanged`
- `NetPackageManager`
- `ReflectionHelpers`
- `World` (`IsWithinTraderArea`, `IsWithinTraderPlacingProtection`)

## Manual Harmony (not PatchAll)
- `EntityTrader.CanLockOnServer` (3.2)
- `EntityTrader.OnLockRequestServer` (3.3)

## Calls
- `Constants.cVersionInformation` (`VersionInformation.LongString`, `IsValid`, `ReleaseType`, `Major`, `Minor`)
- `GameIO.GetSaveGameDir` / `GetUserGameDataDir`
- `GameManager.Instance.World` / `StartCoroutine` / `ShowTooltip` / `IsDedicatedServer`
- `ConnectionManager.Instance.IsServer` / `SendPackage` / `SendToClientsOrServer`
- `ClientInfo.SendPackage` / `entityId`
- `NetPackageManager` `knownPackageTypes` / `packageClassToPackageId` / `packageIdToClass` / `GetPackage` / `SetupBaseMapping` / `StartServer` / `StartClient`
- `World.worldTime` / `GetEntity` / `GetPlayers` / `GetLocalPlayers` / `GetPrimaryPlayer` / `GetTraderAreaAt` / `IsWithinTraderArea` / `IsWithinTraderPlacingProtection` / `SpawnEntityInWorld` / `RemoveEntity` / `ResetPOIS`
- `World.worldToBlockPos`
- `PrefabInstance.ResetBlocksAndRebuild` / `id` / bounding box
- `TraderArea.GetTrader` / `IsWithinProtectArea` / `AreaBounds`
- `EntityTrader.NPCInfo.TraderID` / `IsGodMode` / `Health` / `GetMaxHealth` / `DamageEntity` / `Kill` / `CanDamageEntity`
- `EntityFactory.CreateEntity`
- `EntityClass.list` / entity group spawn names
- `EntityEnemy` (source check)
- `DamageSourceEntity` / `DamageResponse.New` / `EnumDamageSource` / `EnumDamageTypes`
- `EAIApproachAndAttackTarget.CanExecute`
- `EntityAlive.checkForTeleportOutOfTraderArea`
- `ModEvents.GameStartDone` / `WorldShuttingDown` / `PlayerSpawnedInWorld` / `GameUpdate`
- `Localization.Get`
- `SdtdConsole.Instance.Output`
- `CommandSenderInfo.RemoteClientInfo`
- `XUiC_SimpleButton.OnPressed`
- `GUIWindowManager.Open` / `Close` / `IsWindowOpen`
- `ReflectionHelpers.GetTypeWithPrefix`
- `Log.Out` / `Log.Error` / `Log.Warning`
