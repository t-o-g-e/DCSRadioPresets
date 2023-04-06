# DCS Radio Presets

Manage radio presets for groups and units in DCS mission files. Define a plan of frequencies, templates for different aircraft types and assign the planned frequencies to units in mission files.

## Instructions

First, create a general plan of all needed frequencies in the **Plan** tab, such as ATC (for different airfields), AWACS, JTACs, different groups, etc. Give the frequencies a unique and descriptive label.

| Label | Frequency | Modulation |
|-------|-----------|------------|
| ATC   | 123 MHz   | AM         |
| AWACS | 255 MHz   | AM         |
| ...   | ...       | ...        |

Then, in the **Plan** tab, add **Templates** for different aircraft types, and assign the planned labels to the aircraft's radio presets. The template shows the valid range of frequencies and modulations for each radio and preset, and will warn with red color if the planned frequency isn't valid for the radio/preset. 

| Radio   | #     | Channel   | Label (Set these) | Ranges              |
|---------|-------|-----------|-------------------|---------------------|
| ARC-186 | 1     | Channel 1 | ATC               | 108.0 - 151.975 MHz |
| ...     | ...   | ...       | ...               | ...                 |

At minimum, you'll need one template for each aircraft *type* in your missions. To have different set of frequencies for same type of aircraft who are e.g. performing different tasks, you can have multiple templates of the same type too.

To automatically assign different frequencies for different groups, you can use "G#" labels. You should plan frequencies for labels such as "Viper G1" and "Viper G2", and then in the aircraft template you can simply assign a label "Viper G#" to a preset. When the plans are applied to a mission, the group number 1 will get the first (Viper G1) frequency, and the group number 2 will get the second (Viper G2) frequency. The group number is read from the group name, e.g. "Enfield-1", where 1 is the number (the number following the first dash is read as the group number).

Finally, in the **Mission** tab, load your mission and apply the templates to the units in the mission. Templates can be applied automatically, when the first template matching each unit's type will be used to find the labeled frequencies from the plan and assign them to the presets according to the template. Templates can also be assigned manually to individual groups or units. Frequencies for each preset can also be edited manually for each unit.

One plan and related templates can be applied to multiple missions, so plan your frequencies and templates to be general enough to work with different missions.

Save your mission file and play it in DCS.

A kneeboard .png file with the frequencies of each radio and preset will be created for each aircraft group (if the option is selected in the **Settings** tab).

The **Settings** tab has the following settings:
* **DCS folder:** Used to import airfield frequencies to the plan. Not needed otherwise.
* **Missions default folder:** Which folder is opened by default when missions are loaded or saved.
* **Create kneeboard files:** Whether kneeboard files should be generated and saved to the mission file.

## Radio definitions

The **radios.json** file contains definitions for aircraft radios and presets. The list doesn't yet contain all aircraft available in DCS, but more will be added (and can be added by the user too as needed).

## Kneeboard pictures

When saving a mission, the frequencies for each aircraft type are saved to a kneeboard picture in the mission file. The **kbtemplate.png** file is used as a background image, and the **knebo.json** contains simple definitions for font sizes, margins, etc to place the frequency lists to the kneeboard picture.
