using System.Collections.Generic;
using System.Linq;

namespace DCS_Radio_Presets;

public class Breadcrumb
{
    public string Coalition { get; set; }
    public int Country { get; set; }
    public string Type { get; set; }
    public int Group { get; set; }
    public int Unit { get; set; }
    public int Radio { get; set; }
    public List<string> Path { get; set; } = new() { "mission" };

    public bool FoundRadio()
    {
        return Path.Count > 10 && Radio != -1 && Path[^2] == "Radio" && int.TryParse(Path[^1], out _);
    }

    public bool InGroup()
    {
        return Group != -1 && Unit == -1;
    }
            
    public void Add(string block)
    {
        if (Path.Any())
        {
            if (Path[^1] == "coalition")
                Coalition = block;
            else if (Path[^1] == "country")
                Country = int.Parse(block);
            else if ((block == "helicopter" || block == "plane") && Path[^2] == "country")
                Type = block;
            else if (Path[^1] == "group")
                Group = int.Parse(block);
            else if (Path[^1] == "units")
                Unit = int.Parse(block);
            else if (Path[^1] == "Radio")
                Radio = int.Parse(block);
        }

        Path.Add(block);
    }

    public void Remove()
    {
        if (Path.Count <= 2)
        {
            Path.RemoveAt(Path.Count - 1);
            return;
        }

        if (Path[^2] == "coalition")
            Coalition = "";
        else if (Path[^2] == "country")
            Country = -1;
        else if (Path[^1] == "helicopter" || Path[^1] == "plane")
            Type = "";
        else if (Path[^2] == "group")
            Group = -1;
        else if (Path[^2] == "units")
            Unit = -1;
        else if (Path[^2] == "Radio")
            Radio = -1;

        Path.RemoveAt(Path.Count - 1);
    }
}