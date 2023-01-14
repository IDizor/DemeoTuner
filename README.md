# DemeoTuner

Demeo game mod. Requires [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) installed to work.

Configurable mod features:
- Disable auto-delete for saved checkpoints;
- Mass effect for Courage Shanty bard ability;
- No penalty for Healing Potion for downed allies;
- Disconnected player can make a turn when reconnected;
- Added limit for spawning extra enemies on a level.
- and more.

## Limitations

- Mod is for private games with friends and solo skirmish;
- For Steam game version on Windows only;
- For VR you need to connect your VR device to a PC with Windows OS and play using Steam;

## Installation

1. Install mod loader [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) for your Demeo game.
2. Download mod zip from [releases page](https://github.com/IDizor/DemeoTuner/releases).
3. Unzip and place the `Mods` folder into the Demeo game root folder.

## Configuration

To configure mod features edit the file `DemeoTuner.json` with any text editor.

*** In case you are going to play with friends using this mod - make sure the json file is the same for all.

The `DemeoTuner.json` file example:
```
{
  "MenuQuitGameWithNoConfirmation": true,
  "AutoDeleteSavedCheckpoints": false,
  "AllowReconnectedPlayersToPlayCurrentTurn": true,
  "HealingPotion_NoPenaltyForDowned": true,
  "ExtraEnemiesSpawnLimit": 20,
          
  "Guardian_Health": 10,
  "Guardian_MoveRange": 4,
  "Guardian_AttackDamage": 3,
  "Guardian_CritDamage": 6,
  
  "Hunter_Health": 10,
  "Hunter_MoveRange": 4,
  "Hunter_AttackDamage": 3,
  "Hunter_CritDamage": 5,
  "Hunter_Arrow_TargetDamage": 3,
  "Hunter_Arrow_CritDamage": 6,
  
  "Rogue_Health": 10,
  "Rogue_MoveRange": 4,
  "Rogue_AttackDamage": 3,
  "Rogue_CritDamage": 8,
  
  "Sorcerer_Health": 10,
  "Sorcerer_MoveRange": 4,
  "Sorcerer_AttackDamage": 2,
  "Sorcerer_CritDamage": 5,
  
  "Bard_Health": 10,
  "Bard_MoveRange": 4,
  "Bard_AttackDamage": 3,
  "Bard_CritDamage": 6,
  "Bard_CourageShanty_MassEffect": true,
  "Bard_CourageShanty_MassEffect_VisibleAreaOnly": false,
  "Bard_CourageShanty_MassEffect_Radius": 4,
  "Bard_SongOfRecovery_Radius": 2,
  "Bard_SongOfResilience_Radius": 2,
  
  "Warlock_Health": 10,
  "Warlock_MoveRange": 4,
  "Warlock_AttackDamage": 2,
  "Warlock_CritDamage": 5,
  
  "Barbarian_Health": 10,
  "Barbarian_MoveRange": 4,
  "Barbarian_AttackDamage": 4,
  "Barbarian_CritDamage": 8
}
```
