using System;
using System.IO;
using System.Text.Json;

namespace DCS_Radio_Presets;

public class AircraftLoader
{
    //private const string AircraftsPath = @"\CoreMods\aircraft";
    public AircraftDefinition[] AircraftDefinitions = Array.Empty<AircraftDefinition>();
    
    public void LoadAircrafts()
    {
        AircraftDefinitions = JsonSerializer.Deserialize<AircraftDefinition[]>(File.ReadAllText("radios.json")) ?? Array.Empty<AircraftDefinition>();
    }
    
    // private void LoadAircrafts(string dcspath)
    // {
    //     var dirs = Directory.GetDirectories(dcspath + AircraftsPath);
    //
    //     foreach (var aircraftDir in dirs.Where(x => !x.Contains("Pack")))
    //     {
    //         var entryFile = File.ReadAllLines(aircraftDir + @"\entry.lua");
    //         for (var j = 0; j < entryFile.Length; ++j)
    //         {
    //             if (entryFile[j].Contains("dofile"))
    //             {
    //                 var parts = entryFile[j].Split('\'');
    //                 if (parts.Length > 2)
    //                 {
    //                     ReadAircraftDefinition(aircraftDir, parts[1]);
    //                     continue;
    //                 }
    //
    //                 parts = entryFile[j].Split('\"');
    //                 if(parts.Length > 2)
    //                 {
    //                     ReadAircraftDefinition(aircraftDir, parts[1]);
    //                 }
    //             }
    //         }
    //     }
    // }
    //
    // private void ReadAircraftDefinition(string aircraftDir, string fileName)
    // {
    //     var nameRegex = new Regex(@"^\s*name\s?=\s*_\(""(.*)""\),");
    //     var rangeRegex =
    //         new Regex(@".*{min =\s*([\d\.]*), max =\s*([\d\.]*)(?:, modulation\t= MODULATION_(\w*)|\s?)}.*");
    //     var channelRegex = new Regex(@".*\[(\d*)\].*default = ([\d\.]*).*");
    //     var radioIdRegex = new Regex(@"\s*\[(\d*)\] =.*");
    //     var typeRegex = new Regex(@"^\s*Name\s*=.*['""](.*)['""],");
    //
    //     var file = File.ReadAllLines(aircraftDir + "\\" + fileName);
    //
    //     var i = 0;
    //
    //     var type = "";
    //     for (; i < file.Length - 1; ++i)
    //     {
    //         var typeMatch = typeRegex.Match(file[i + 1]);
    //         if (typeMatch.Success)
    //         {
    //             type = typeMatch.Groups[1].Value;
    //             break;
    //         }
    //     }
    //
    //     if (type == "")
    //         return;
    //
    //     while (i < file.Length && !file[i].Contains("panelRadio"))
    //         ++i;
    //     if (i >= file.Length)
    //         return;
    //
    //     var aircraftDefinition = new AircraftDefinition { Type = type };
    //     AircraftDefinitions.Add(aircraftDefinition);
    //
    //     var radio = "";
    //     var radioId = -1;
    //     var ranges = new List<Range>();
    //     for (; i < file.Length && file[i] != "\t},"; ++i)
    //     {
    //         var row = file[i];
    //         var nameMatch = nameRegex.Match(row);
    //         if (nameMatch.Success)
    //         {
    //             radio = nameMatch.Groups[1].Value;
    //             radioId = int.Parse(radioIdRegex.Match(file[i - 1]).Groups[1].Value);
    //             ranges.Clear();
    //         }
    //
    //         var rangeMatch = rangeRegex.Match(row);
    //         if (rangeMatch.Success)
    //         {
    //             ranges.Add(new Range
    //             {
    //                 Min = decimal.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture),
    //                 Max = decimal.Parse(rangeMatch.Groups[2].Value, CultureInfo.InvariantCulture),
    //                 Modulation = string.IsNullOrEmpty(rangeMatch.Groups[3].Value) ? "AM" : rangeMatch.Groups[3].Value
    //             });
    //         }
    //
    //         var channelMatch = channelRegex.Match(row);
    //         if (channelMatch.Success)
    //         {
    //             aircraftDefinition.Channels.Add(new Channel
    //             {
    //                 RadioId = radioId,
    //                 Radio = radio,
    //                 Number = int.Parse(channelMatch.Groups[1].Value),
    //                 Default = decimal.Parse(channelMatch.Groups[2].Value, CultureInfo.InvariantCulture),
    //                 Ranges = ranges.ToList()
    //             });
    //         }
    //     }
    // }
}

public class AircraftDefinition
{
    public string Type { get; set; }
    public RadioDefinition[] Radios { get; set; }
}

public class RadioDefinition
{
    public int Id { get; set; }
    public string Name { get; set; }

    public Range[] Ranges { get; set; }
    public string[] Channels { get; set; }
    public bool Primary { get; set; }
}

public class Range
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public string Modulation { get; set; }
}