using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(DemeoTuner.BuildInfo.Description)]
[assembly: AssemblyDescription(DemeoTuner.BuildInfo.Description)]
[assembly: AssemblyCompany(DemeoTuner.BuildInfo.Company)]
[assembly: AssemblyProduct(DemeoTuner.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + DemeoTuner.BuildInfo.Author)]
[assembly: AssemblyTrademark(DemeoTuner.BuildInfo.Company)]
[assembly: AssemblyVersion(DemeoTuner.BuildInfo.Version)]
[assembly: AssemblyFileVersion(DemeoTuner.BuildInfo.Version)]
[assembly: MelonInfo(typeof(DemeoTuner.DemeoTunerMod), DemeoTuner.BuildInfo.Name, DemeoTuner.BuildInfo.Version, DemeoTuner.BuildInfo.Author, DemeoTuner.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame("Resolution Games", null)]