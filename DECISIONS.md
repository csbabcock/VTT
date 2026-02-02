This file is authoritative. Changes must be additive or explicitly marked.



\# DECISIONS



\## Architecture policy

\* SOLID is guidance, not dogma. Prefer clarity over purity.

\* MVP is the standard for complex UI flows. Simple UI stays View only until it needs a Presenter.

\* No new abstractions unless they remove duplication, isolate a volatile dependency, or improve testability.

\* Prefer composition over inheritance.

\* Any new pattern must state the concrete problem it solves.



\## Product and UI decisions



\* Ability score inputs integrated directly into ability-scores-grid rows rather than separate input section

\* Each ability stat row contains: Label (name), IntegerField (score input), Label (modifier)

\* IntegerFields created dynamically in C# (CreateAbilityStatRow) rather than defined in UXML

\* Used SetValueWithoutNotify when updating ability scores programmatically to prevent event loops

\* Roll button kept below ability-scores-grid in stats-panel

\* Background tab removed entirely; background selection UI deferred to future implementation

\* Tab count reduced from 4 to 2 (Class, Race only)



\## Corrections / reversals (explicit)



\* UPDATED: Ability score interaction is currently drag-and-drop driven; ability rows act as drop zones for rolled scores. (This is now the primary UX.)

\* REVERSED (if applicable): Editable IntegerField based assignment is not the current display mechanism. Current display uses Labels in drop zones rather than IntegerFields.

&nbsp; - If/when point-buy is implemented, decide whether that reintroduces IntegerFields or uses an alternative control.



