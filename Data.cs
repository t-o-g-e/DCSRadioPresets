using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace DCS_Radio_Presets;

public class DCS
{
    private const string TheatresPath = @"\Mods\terrains";
    
    private AircraftLoader aircraftLoader = new();

    public ObservableCollection<string> Theatres = new();
    public ObservableCollection<AircraftModel> Aircrafts = new();

    public bool Load(string dcspath)
    {
        LoadAircrafts();

        Theatres.Clear();
        if (!Directory.Exists(dcspath) || !Directory.Exists(dcspath + TheatresPath))
            return false;

        var theatres = Directory.GetDirectories(dcspath + TheatresPath).Select(x => x.Split('\\')[^1]);
        foreach (var theatre in theatres)
        {
            Theatres.Add(theatre);
        }

        return true;
    }

    public Theatre LoadTheatre(string dcspath, string theatreName)
    {
        var radioFile = File.ReadAllLines(dcspath + @"\Mods\terrains\" + theatreName + @"\Radio.lua");

        var theatre = new Theatre { Name = theatreName };

        var i = 0;
        while (i < radioFile.Length && !radioFile[i].StartsWith("radio = ")) ++i;
        for (; i < radioFile.Length; ++i)
        {
            var row = radioFile[i];
            if (row == "\t{")
            {
                var callsign = "";
                for (var j = i + 1; j < radioFile.Length && radioFile[j] != "\t};"; ++j)
                {
                    row = radioFile[j];

                    if (row.Contains("callsign"))
                    {
                        var reg = new Regex(@"_\(\""(\w*)\""");
                        var m = reg.Match(row);
                        callsign = m.Groups[1].Value;
                    }

                    if (row.Contains("frequency"))
                    {
                        //frequency = {[HF] = {MODULATIONTYPE_AM, 4200000.000000}, [UHF] = {MODULATIONTYPE_AM, 259000000.000000}, [VHF_HI] = {MODULATIONTYPE_AM, 130000000.000000}, [VHF_LOW] = {MODULATIONTYPE_AM, 40200000.000000}};
                        var reg = new Regex(@"\[(\w*)\] = \{MODULATIONTYPE_(\w*), ([-.\d]*)\}");
                        var m = reg.Matches(row);
                        foreach (Match match in m)
                        {
                            theatre.TheatreFrequencies.Add(new PlanFrequency
                            {
                                Label = $"{callsign} {match.Groups[1]}",
                                Frequency = decimal.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) / 1000000
                            });
                        }
                    }
                }
            }
        }

        return theatre;
    }

    private void LoadAircrafts()
    {
        Aircrafts.Clear();
        
        aircraftLoader.LoadAircrafts();
        
        foreach (var aircraft in aircraftLoader.AircraftDefinitions)
            Aircrafts.Add(new AircraftModel(aircraft));
    }
}

public class Theatre
{
    public List<PlanFrequency> TheatreFrequencies = new();
    public string Name { get; set; }
}

public class CommPlan
{
    public ObservableCollection<PlanFrequency> Frequencies { get; set; } = new();
    public ObservableCollection<Template> Templates { get; set; } = new();
    
    [JsonIgnore]
    public bool Dirty { get; private set; }

    public void AddTheatreFrequencies(Theatre theatre)
    {
        foreach (var frequency in theatre.TheatreFrequencies)
            Frequencies.Add(frequency);
    }

    public void Save(string fileName)
    {
        var json = JsonSerializer.Serialize(this);
        File.WriteAllLines(fileName, new[] { json });

        Dirty = false;
    }

    public static CommPlan Load(string fileName, IList<AircraftModel> aircraftDefinitions)
    {
        var plan = JsonSerializer.Deserialize<CommPlan>(File.ReadAllText(fileName)) ?? new CommPlan();
        foreach (var template in plan.Templates)
        foreach (var channel in template.Channels)
            channel.Initialize(plan.Frequencies, aircraftDefinitions.Where(x => x.Type == template.Type));

        return plan;
    }
}

public class PlanFrequency
{
    public string Label { get; set; }
    public decimal Frequency { get; set; }
    public string Modulation { get; set; } = "AM";

    public void Swap(PlanFrequency other)
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
}

public class ChannelModel
{
    public int RadioId { get; set; }
    public string RadioName { get; set; }
    public int Number { get; set; }
    public string Name { get; set; }
    public List<Range> Ranges { get; set; }
    public bool Primary { get; set; }
}

public class AircraftModel
{
    public AircraftModel(AircraftDefinition definition)
    {
        Type = definition.Type;
        Channels = definition.Radios.SelectMany(radio => radio.Channels.Select((channel, number) => new ChannelModel
        {
            RadioId = radio.Id,
            Ranges = radio.Ranges.ToList(),
            RadioName = radio.Name,
            Number = number,
            Name = channel,
            Primary = radio.Primary && number == 0
        })).ToList();
    }
    
    public string Type { get; set; }
    public List<ChannelModel> Channels { get; set; } = new();
}

public class TemplateChannel
{
    public string RadioName { get; set; }
    public long RadioId { get; set; }
    public int Number { get; set; }
    public string Channel { get; set; }
    private string label;
    public string Label
    {
        get => label;
        set
        {
            label = value;
            CheckRanges();
        }
    }
    public List<Range> Ranges { get; set; } = new();

    [JsonIgnore]
    public string RangeText => string.Join(", ", Ranges.Select(x => $"{x.Min.ToString(CultureInfo.InvariantCulture)} - {x.Max.ToString(CultureInfo.InvariantCulture)} ({x.Modulation})"));

    private bool valid = true;
    [JsonIgnore]
    public Brush Color => valid ? Brushes.Black : Brushes.Red;

    public ObservableCollection<PlanFrequency> Frequencies = new();

    public TemplateChannel()
    {
    }

    public TemplateChannel(TemplateChannel channel)
    {
        Frequencies = channel.Frequencies;
        RadioName = channel.RadioName;
        RadioId = channel.RadioId;
        Number = channel.Number;
        Channel = channel.Channel;
        label = channel.Label;
        Ranges = channel.Ranges.ToList();
    }
    
    public TemplateChannel(ChannelModel channel, ObservableCollection<PlanFrequency> frequencies)
    {
        Frequencies = frequencies;
        RadioName = channel.RadioName;
        RadioId = channel.RadioId;
        Number = channel.Number;
        Channel = channel.Name;
        label = "";
        Ranges = channel.Ranges.ToList();
    }

    public bool CheckRanges()
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            var changed = !valid;
            valid = true;
            return changed;
        }

        var orig = valid;
        valid = false;
        var freq = Frequencies.FirstOrDefault(x => x.Label == label);
        if (freq != null)
        {
            foreach (var range in Ranges)
            {
                if (range.Modulation.Contains(freq.Modulation) && range.Min <= freq.Frequency &&
                    range.Max >= freq.Frequency)
                {
                    valid = true;
                    break;
                }
            }
        }

        return valid != orig;
    }

    public void Initialize(ObservableCollection<PlanFrequency> frequencies, IEnumerable<AircraftModel> aircraftDefinitions)
    {
        Frequencies = frequencies;
        var channel = aircraftDefinitions.SelectMany(x => x.Channels).FirstOrDefault(c => c.RadioId == RadioId && c.Number == Number);
        Channel = channel?.Name ?? string.Empty;
        Ranges = channel?.Ranges ?? new List<Range>();
        CheckRanges();
    }
}

public class Template
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public override string ToString()
    {
        return Type + ": " + Name;
    }

    public Template()
    {
    }

    public Template(Template template)
    {
        Type = template.Type;
        Name = template.Name + " (copy)";
        Channels = new ObservableCollection<TemplateChannel>(template.Channels.Select(x =>
            new TemplateChannel(x)));
    }
    
    public ObservableCollection<TemplateChannel> Channels { get; set; } = new();
}