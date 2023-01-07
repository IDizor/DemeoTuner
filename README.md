# DemeoTuner

Demeo game mod. Requires [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) installed to work.

Configurable mod features:
- Disable auto-delete for saved checkpoints;
- Mass effect for Courage Shanty bard ability;
- No penalty for Healing Potion for downed allies;
- and more.

## Installation

Download zip from [releases page](https://github.com/IDizor/DemeoTuner/releases).

For PC: Unzip and place the `Mods` folder into the Demeo game root folder for all players.

For VR: I had no chance to test it with VR devices. Most likely the mod is not compatible with VR game version. And I'm not sure about MelonLoader VR compatibility as well.

## Configuration

To configure mod features edit the file `DemeoTuner.json` with any text editor.

*** In case you are going to play with friends using this mod - make sure the json file is the same for all.

The `DemeoTuner.json` file example:
```
{
  "MenuQuitGameWithNoConfirmation": true,
  "AutoDeleteSavedCheckpoints": false,
  "HealingPotion_NoPenaltyForDowned": true,
  "Bard_CourageShanty_MassEffect": true,
  "Bard_CourageShanty_MassEffect_Radius": 4,
  "Bard_CourageShanty_MassEffect_VisibleAreaOnly": false,
  "Bard_SongOfRecovery_Radius": 2,
  "Bard_SongOfResilience_Radius": 2
}
```
