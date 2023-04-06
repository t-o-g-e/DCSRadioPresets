using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NLua;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace DCS_Radio_Presets;

public class Mission
{
    private string MissionFilePath { get; set; }
    
    public List<UnitGroup> Groups { get; set; } = new();

    public void Save(string fileName, bool createKneeboard)
    {
        var missionFile = File.ReadAllLines(@"mission\mission").ToList();

        var startBlockRegex = new Regex(@"\s*\[\""?(.*?)\""?\] =\s*{?\s*$");
        var breadcrumb = new Breadcrumb();

        for (var i = 0; i < missionFile.Count; ++i)
        {
            var row = missionFile[i];
            var blockMatch = startBlockRegex.Match(row);
            if (blockMatch.Success)
            {
                var block = blockMatch.Groups[1].Value;
                breadcrumb.Add(block);
                
                if (breadcrumb.FoundRadio())
                {
                    var cl = breadcrumb.Path.Contains("helicopter") ? "helicopter" : "plane";
                    var presets = Groups
                        .Where(x => x.Id == breadcrumb.Group)
                        .SelectMany(x => x.Units)
                        .FirstOrDefault(x => x.Id == breadcrumb.Unit && x.Class == cl)?.Presets
                        .Where(x => x.RadioId == breadcrumb.Radio)
                        .ToList();
                    if (presets != null)
                    {
                        i += 2;
                        if (missionFile[i].Contains("modulations"))
                        {
                            i += 2;
                            for (var j = 0; j < presets.Count; ++j)
                            {
                                var mod = presets[j].ModulationOptions.Length > 1 && presets[j].Modulation == "FM"  ? 1 : 0;
                                var str = $"                                                    [{presets[j].Id}] = {mod},";
                                if (missionFile[i + j].Contains("modulations"))
                                    missionFile.Insert(i + j, str);
                                else
                                    missionFile[i + j] = str;
                            }

                            i += presets.Count + 1;
                        }

                        if (missionFile[i].Contains("channels"))
                        {
                            i += 2;
                            for (var j = 0; j < presets.Count; ++j)
                            {
                                var freq = presets[j].Frequency.ToString(CultureInfo.InvariantCulture);
                                missionFile[i + j] = $"                                                    [{presets[j].Id}] = {freq},";
                            }

                            i += presets.Count + 1;
                        }
                    }

                    row = missionFile[i];
                }
            }
            
            if (breadcrumb.InGroup() && row.Contains("frequency"))
            {
                var cl = breadcrumb.Path.Contains("helicopter") ? "helicopter" : "plane";
                var primaryPreset = Groups.Where(x => x.Id == breadcrumb.Group).SelectMany(x => x.Units)
                    .FirstOrDefault(x => x.Class == cl)?.Presets.FirstOrDefault(x => x.Primary);
                if (primaryPreset != null)
                    missionFile[i] = $"                                [\"frequency\"] = {primaryPreset.Frequency},";
            }

            if (row.Contains("},"))
                breadcrumb.Remove();
        }
            
        File.WriteAllLines(@"mission\mission", missionFile);

        if(createKneeboard)
            CreateKneeboards();
        
        File.Delete(fileName);
        ZipFile.CreateFromDirectory("mission", fileName);
    }

    private void CreateKneeboards()
    {
        var layout = JsonSerializer.Deserialize<KneboLayout>(File.ReadAllText("knebo.json"));
        if (layout == null)
            return;
        
        var fc = new FontCollection();
        var ff = fc.Add("OpenSans-Semibold.ttf");
        var groupNameFont = ff.CreateFont(layout.FontSizes.GroupName);
        var radioFont = ff.CreateFont(layout.FontSizes.Radio);
        var presetFont = ff.CreateFont(layout.FontSizes.Preset);

        foreach (var group in Groups)
        {
            if(!group.Units.Any()) //unit => unit.Presets.Any(p => !string.IsNullOrWhiteSpace(p.Label))
                continue;
            var unit = group.Units.First();
            
            using var image = Image.Load("kbtemplate.png");

            var y = layout.Margins.Y;
            image.Mutate(x => x.DrawText(group.Name, groupNameFont, Color.Black, new PointF(layout.Margins.X, y)));
            y += layout.Margins.BelowGroupName;
            
            var radios = unit.Presets.ToLookup(x => x.RadioName).Distinct();
            var radioIndex = 0;
            foreach (var radio in radios)
            {
                var x = radioIndex < 2 ? layout.Margins.X : layout.Margins.X2;
                if (radioIndex == 2)
                    y = layout.Margins.Y + layout.Margins.BelowGroupName;

                image.Mutate(c => c.DrawText(radio.Key, radioFont, Color.Black, new PointF(x, y)));
                y += layout.Margins.BelowRadioName;
                
                var presets = radio.ToArray(); //.Where(r => !string.IsNullOrWhiteSpace(r.Label)
                for(var pi = 0; pi < presets.Length; ++pi)
                {
                    var preset = presets[pi];
                    if (pi > 0)
                        y += layout.Margins.BetweenPresets;
                    image.Mutate(c => c.DrawText($"{preset.Id}: {preset.Label}", presetFont, Color.Black, new PointF(x + layout.Margins.IndentPreset, y)));
                    image.Mutate(c => c.DrawText($"{preset.Frequency}", presetFont, Color.Black, new PointF(x + layout.Margins.IndentFrequency, y)));
                    image.Mutate(c => c.DrawText($"{preset.Modulation}", presetFont, Color.Black, new PointF(x + layout.Margins.IndentModulation, y)));
                    if(pi == presets.Length -1)
                        y += layout.Margins.BelowLastPreset;
                }

                radioIndex++;
            }
        
            Directory.CreateDirectory("mission\\KNEEBOARD\\" + unit.Type + "\\IMAGES");
            image.Save("mission\\KNEEBOARD\\" + unit.Type + "\\IMAGES\\" + group.Name + ".png");
        }
    }

    private void ReadUnits(LuaTable countryName, IList<AircraftModel> aircrafts)
    {
        var groupNumberRegex = new Regex(@".*?-(\d*).*");
        foreach (var countryKey in countryName.Keys)
        {
            foreach (var type in new[] { "plane", "helicopter" })
            {
                var country = (LuaTable)countryName[countryKey];
                if (!country.Keys.Cast<string>().Contains(type))
                    continue;

                var groups = (LuaTable)country[type + ".group"];
                foreach (KeyValuePair<object, object> table in groups)
                {
                    var group = new UnitGroup();
                    var unitGroup = (LuaTable)table.Value;
                    // group.GroupId = (long)unitGroup["groupId"];
                    group.Id = (long)table.Key;
                    group.Name = (string)unitGroup["name"];
                    var m = groupNumberRegex.Match(group.Name);
                    group.Number =
                        m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value) &&
                        int.TryParse(m.Groups[1].Value, out var number)
                            ? number
                            : -1;

                    var units = (LuaTable)unitGroup["units"];
                    foreach (KeyValuePair<object, object> u in units)
                    {
                        var unit = new Unit
                        {
                            Name = (string)((LuaTable)u.Value)["name"],
                            Type = (string)((LuaTable)u.Value)["type"],
                            Class = type,
                            Id = (long)u.Key,
                            GroupNumber = group.Number
                        };

                        var unitDef = aircrafts.FirstOrDefault(x => x.Type == unit.Type);

                        var radios = (LuaTable?)((LuaTable)u.Value)["Radio"];
                        if(radios == null)
                            continue;
                        
                        var presets = new List<Preset>();
                        foreach (KeyValuePair<object, object> r in radios)
                        {
                            var radioDef = unitDef?.Channels.FirstOrDefault(x => x.RadioId == (long)r.Key);

                            var modulations = (LuaTable)((LuaTable)r.Value)["modulations"];
                            var modKeys = new HashSet<long>(modulations.Keys.Cast<long>());
                            var channels = (LuaTable)((LuaTable)r.Value)["channels"];
                            foreach (var key in channels.Keys)
                            {
                                var freq = (decimal)(channels[key] as long? ?? channels[key] as double? ?? 0);
                                var modOptions =
                                    radioDef?.Ranges.Select(x => x.Modulation).Where(x => x != "AMFM").Distinct().OrderBy(x => x).ToArray() ??
                                    new[] { "AM" };
                                var mod = modOptions.Length == 1
                                    ? modOptions[0]
                                    : modKeys.Contains((long)key) && modOptions.Length > (long)modulations[key]
                                        ? modOptions[(long)modulations[key]]
                                        : "AM";
                                presets.Add(new Preset
                                {
                                    RadioId = (long)r.Key,
                                    RadioName = (long)r.Key + ": " + radioDef?.RadioName,
                                    Id = (long)key,
                                    Frequency = freq,
                                    OriginalFrequency = freq,
                                    Modulation = mod,
                                    ModulationOptions = modOptions,
                                    OriginalModulation = mod,
                                    Primary = radioDef?.Primary == true && (long)key == 1 
                                });
                            }
                        }

                        presets.Sort((x, y) =>
                            x.RadioId == y.RadioId ? x.Id.CompareTo(y.Id) : x.RadioId.CompareTo(y.RadioId));
                        foreach (var preset in presets)
                        {
                            unit.Presets.Add(preset);    
                        }
                            
                        if (unit.Presets.Any())
                            group.Units.Add(unit);
                    }

                    if (group.Units.Any())
                        Groups.Add(group);
                }
            }
        }
    }

    public static Mission Load(string filePath, IList<AircraftModel> aircrafts)
    {
        if(Directory.Exists("mission"))
            Directory.Delete("mission", true);
        ZipFile.ExtractToDirectory(filePath, "mission", true);

        var mission = new Mission();

        mission.MissionFilePath = filePath;

        var lua = new Lua();
        lua.DoFile(@"mission\mission");

        var blue = (LuaTable)lua["mission.coalition.blue.country"];
        var red = (LuaTable)lua["mission.coalition.red.country"];

        mission.ReadUnits(blue, aircrafts);
        mission.ReadUnits(red, aircrafts);

        return mission;
    }

    public IEnumerable<string?> ApplyMatchingTemplates(CommPlan plan)
    {
        var duplicates = new HashSet<string>();
        foreach (var group in Groups)
        {
            var type = group.Units.FirstOrDefault()?.Type;
            if(string.IsNullOrWhiteSpace(type))
                continue;
            
            var templates = plan.Templates.Where(x => x.Type == type).ToList();
            if(templates.Count == 0)
                continue;
            if (templates.Count > 1)
            {
                duplicates.Add(type);
                continue;
            }

            var template = templates[0];
            group.ApplyTemplate(template, plan.Frequencies);
        }

        return duplicates;
    }
}

public class KneboLayout
{
    public FontSizes FontSizes { get; set; }
    public Margins Margins { get; set; }
}

public class FontSizes
{
    public int GroupName { get; set; }
    public int Radio { get; set; }
    public int Preset { get; set; }
}

public class Margins
{
    public int X { get; set; }
    public int X2 { get; set; }
    public int Y { get; set; }
    public int BelowGroupName { get; set; }
    public int BelowRadioName { get; set; }
    public int BetweenPresets { get; set; }
    public int BelowLastPreset { get; set; }
    public int IndentPreset { get; set; }
    public int IndentFrequency { get; set; }
    public int IndentModulation { get; set; }
}

public class Preset
{
    public long RadioId { get; set; }
    public string RadioName { get; set; }
    public long Id { get; set; }
    public decimal Frequency { get; set; }
    public decimal OriginalFrequency { get; set; }
    public string Modulation { get; set; }
    public string[] ModulationOptions { get; set; }
    public string OriginalModulation { get; set; }
    public string Label { get; set; }
    public bool Primary { get; set; }

    public void Reset()
    {
        Frequency = OriginalFrequency;
        Modulation = OriginalModulation;
        Label = "";
    }

    public void Swap(Preset other)
    {
        var label = Label;
        var freq = Frequency;
        var mod = Modulation;

        Label = other.Label;
        Frequency = other.Frequency;
        Modulation = other.Modulation;

        other.Label = label;
        other.Frequency = freq;
        other.Modulation = mod;
    }

    public bool Apply(PlanFrequency freq, string label)
    {
        var changed = Frequency != freq.Frequency || Modulation == freq.Modulation || Label != label;
        Frequency = freq.Frequency;
        Modulation = freq.Modulation;
        Label = label;
        return changed;
    }

    public void Apply(Preset preset)
    {
        Frequency = preset.Frequency;
        Modulation = preset.Modulation;
        Label = preset.Label;
    }
}

public class Unit
{
    public long Id { get; set; }
    public string Type { get; set; }
    public string Class { get; set; }
    public string Name { get; set; }
    public int GroupNumber { get; set; }
    public ObservableCollection<Preset> Presets { get; set; } = new();

    public void ApplyTemplate(Template template, IList<PlanFrequency> planFrequencies)
    {
        foreach (var preset in Presets)
        {
            var templateChannel = template.Channels.FirstOrDefault(x => x.RadioId == preset.RadioId && x.Number == preset.Id - 1);
            if(templateChannel == null || string.IsNullOrWhiteSpace(templateChannel.Label))
                continue;

            var label = GroupNumber != -1 ? templateChannel.Label.Replace("G#", "G" + GroupNumber) : templateChannel.Label;
            var freq = planFrequencies.FirstOrDefault(x => x.Label == label);
            if(freq != null)
                preset.Apply(freq, label);
        }
    }

    public void ApplyUnit(Unit unit)
    {
        for(var i = 0; i < Presets.Count; ++i)
            Presets[i].Apply(unit.Presets[i]);
    }
}

public class UnitGroup
{
    //public long GroupId { get; set; } = -1;
    public long Id { get; set; }
    public string Name { get; set; }
    public int Number { get; set; }
    public List<Unit> Units { get; set; } = new();

    public void ApplyTemplate(Template template, IList<PlanFrequency> planFrequencies)
    {
        foreach (var unit in Units)
            unit.ApplyTemplate(template, planFrequencies);
    }

    public void ApplyUnit(Unit unit)
    {
        foreach (var u in Units)
            u.ApplyUnit(unit);
    }
}