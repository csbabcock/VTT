This file is authoritative. Changes must be additive or explicitly marked.



# DECISIONS

* Ability score inputs integrated directly into ability-scores-grid rows rather than separate input section
* Each ability stat row contains: Label (name), IntegerField (score input), Label (modifier)
* IntegerFields created dynamically in C# (CreateAbilityStatRow) rather than defined in UXML
* Used SetValueWithoutNotify when updating ability scores programmatically to prevent event loops
* Roll button kept below ability-scores-grid in stats-panel
* Background tab removed entirely; background selection UI deferred to future implementation
* Tab count reduced from 4 to 2 (Class, Race only)
