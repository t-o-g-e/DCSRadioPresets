# DCS Radio Presets

Manage radio presets for groups and units in DCS mission files. Define a plan of frequencies, templates for different aircraft types and assign the planned frequencies to units in mission files.

## Instructions

First, plan and label the needed frequencies in the **Plan** tab.

| Label | Frequency | Modulation |
|-------|-----------|------------|
| ATC   | 123 MHz   | AM         |
| AWACS | 255 MHz   | AM         |
| ...   | ...       | ...        |

Then, in the **Plan** tab, add **Templates** for different aircraft types, and assign
the planned labels to the aircraft's radio presets. The template shows the valid 
range of frequencies and modulations for each radio and preset. 

| Radio   | #     | Channel   | Label (Set these) | Ranges              |
|---------|-------|-----------|-------------------|---------------------|
| ARC-186 | 1     | Channel 1 | ATC               | 108.0 - 151.975 MHz |
| ...     | ...   | ...       | ...               | ...                 |

Finally, in the **Mission** tab, load your mission and apply the templates to the units
in the mission. Templates can be applied automatically, when the first template matching
each unit's type will be used to find the labeled frequencies from the plan and assign them
to the presets according to the template. Templates can also be assigned manually to individual
groups or units. Frequencies for each preset can also be edited manually for each unit.

A kneeboard .png file with the frequencies of each radio and preset will be created for each aircraft group.

Save your mission file and play it in DCS.

The **Settings** tab has the following settings:
* **DCS folder:** Used to import airfield frequencies to the plan
* **Missions default folder:** Which folder is opened by default when missions are loaded or saved
* **Create kneeboard files:** Whether kneeboard files should be generated and saved to the mission file 