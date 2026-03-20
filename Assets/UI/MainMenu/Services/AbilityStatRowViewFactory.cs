using System;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Commands raised by controls inside an ability stat tile. Injected so the factory
    /// does not reference <see cref="CharacterCreationView"/> (dependency inversion).
    /// </summary>
    public readonly struct AbilityStatRowUiBinding
    {
        public AbilityStatRowUiBinding(
            Action<int> onPointBuyDecrement,
            Action<int> onPointBuyIncrement,
            Action<int, string> onManualAbilityEntryChanged,
            Func<bool> shouldSuppressManualAbilityEntryEvents)
        {
            OnPointBuyDecrement = onPointBuyDecrement ?? throw new ArgumentNullException(nameof(onPointBuyDecrement));
            OnPointBuyIncrement = onPointBuyIncrement ?? throw new ArgumentNullException(nameof(onPointBuyIncrement));
            OnManualAbilityEntryChanged = onManualAbilityEntryChanged ?? throw new ArgumentNullException(nameof(onManualAbilityEntryChanged));
            ShouldSuppressManualAbilityEntryEvents = shouldSuppressManualAbilityEntryEvents ?? throw new ArgumentNullException(nameof(shouldSuppressManualAbilityEntryEvents));
        }

        public Action<int> OnPointBuyDecrement { get; }
        public Action<int> OnPointBuyIncrement { get; }
        public Action<int, string> OnManualAbilityEntryChanged { get; }
        public Func<bool> ShouldSuppressManualAbilityEntryEvents { get; }
    }

    /// <summary>
    /// Sole responsibility: construct the UI Toolkit hierarchy for one ability score tile
    /// (headers, point-buy rails, drop zone / manual field, modifier). Open for extension
    /// via new factory methods if layouts diverge; closed to change in the view.
    /// </summary>
    public static class AbilityStatRowViewFactory
    {
        public static VisualElement CreateRow(string abilityDisplayName, int abilityIndex, AbilityStatRowUiBinding binding)
        {
            if (string.IsNullOrEmpty(abilityDisplayName))
                throw new ArgumentException("Display name required.", nameof(abilityDisplayName));

            string key = abilityDisplayName.ToLowerInvariant();

            VisualElement row = new VisualElement();
            row.AddToClassList("character-creation-ability-stat-row");
            row.name = $"ability-stat-{key}";
            row.userData = abilityDisplayName;

            row.Add(CreateTitle(abilityDisplayName));
            row.Add(CreateValuesColumn(abilityDisplayName, key, abilityIndex, binding));
            return row;
        }

        private static Label CreateTitle(string abilityDisplayName)
        {
            var nameLabel = new Label(abilityDisplayName);
            nameLabel.AddToClassList("character-creation-ability-stat-name");
            return nameLabel;
        }

        private static VisualElement CreateValuesColumn(
            string abilityDisplayName,
            string elementKey,
            int abilityIndex,
            AbilityStatRowUiBinding binding)
        {
            VisualElement values = new VisualElement();
            values.AddToClassList("character-creation-ability-stat-values");

            VisualElement inlineBlock = new VisualElement();
            inlineBlock.AddToClassList("character-creation-ability-stat-inline-block");

            inlineBlock.Add(CreateHeaderLabelRow());
            inlineBlock.Add(CreateScoreValueRow(abilityDisplayName, elementKey, abilityIndex, binding));

            values.Add(inlineBlock);
            return values;
        }

        private static VisualElement CreateHeaderLabelRow()
        {
            VisualElement labelRow = new VisualElement();
            labelRow.AddToClassList("character-creation-ability-stat-inline-label-row");

            VisualElement scoreHeaderTrack = new VisualElement();
            scoreHeaderTrack.AddToClassList("character-creation-ability-stat-header-track--score");
            var scoreLabel = new Label("Score");
            scoreLabel.AddToClassList("character-creation-ability-stat-label");
            scoreHeaderTrack.Add(scoreLabel);

            VisualElement modHeaderTrack = new VisualElement();
            modHeaderTrack.AddToClassList("character-creation-ability-stat-header-track--mod");
            var modLabel = new Label("Mod");
            modLabel.AddToClassList("character-creation-ability-stat-label");
            modHeaderTrack.Add(modLabel);

            labelRow.Add(scoreHeaderTrack);
            labelRow.Add(modHeaderTrack);
            return labelRow;
        }

        private static VisualElement CreateScoreValueRow(
            string abilityDisplayName,
            string elementKey,
            int abilityIndex,
            AbilityStatRowUiBinding binding)
        {
            VisualElement scoreValueRow = new VisualElement();
            scoreValueRow.AddToClassList("character-creation-ability-score-value-row");

            VisualElement pointBuyMinus = CreatePointBuyRail(
                $"ability-{elementKey}-point-buy-minus",
                () => binding.OnPointBuyDecrement(abilityIndex));

            (VisualElement slot, _) = CreateScoreInputSlot(abilityDisplayName, elementKey, abilityIndex, binding);

            VisualElement pointBuyPlus = CreatePointBuyRail(
                $"ability-{elementKey}-point-buy-plus",
                () => binding.OnPointBuyIncrement(abilityIndex),
                isPlusRail: true);

            VisualElement modValueBox = CreateModifierBox(elementKey);

            VisualElement scoreValueHalf = new VisualElement();
            scoreValueHalf.AddToClassList("character-creation-ability-stat-value-half--score");
            scoreValueHalf.Add(pointBuyMinus);
            scoreValueHalf.Add(slot);
            scoreValueHalf.Add(pointBuyPlus);

            VisualElement modValueHalf = new VisualElement();
            modValueHalf.AddToClassList("character-creation-ability-stat-value-half--mod");
            modValueHalf.Add(modValueBox);

            scoreValueRow.Add(scoreValueHalf);
            scoreValueRow.Add(modValueHalf);
            return scoreValueRow;
        }

        private static VisualElement CreatePointBuyRail(string elementName, Action onClick, bool isPlusRail = false)
        {
            VisualElement rail = new VisualElement();
            rail.AddToClassList("character-creation-point-buy-controls");
            if (isPlusRail)
                rail.AddToClassList("character-creation-point-buy-controls--before-mod");
            rail.name = elementName;
            rail.style.display = DisplayStyle.Flex;
            rail.style.visibility = Visibility.Hidden;

            var btn = new Button(onClick) { text = isPlusRail ? "+" : "−" };
            btn.AddToClassList("character-creation-point-buy-btn");
            rail.Add(btn);
            return rail;
        }

        private static (VisualElement slot, TextField entry) CreateScoreInputSlot(
            string abilityDisplayName,
            string elementKey,
            int abilityIndex,
            AbilityStatRowUiBinding binding)
        {
            VisualElement scoreDropZone = new VisualElement();
            scoreDropZone.AddToClassList("character-creation-ability-score-drop-zone");
            scoreDropZone.name = $"ability-{elementKey}-drop-zone";
            scoreDropZone.userData = abilityDisplayName;

            var scoreValueLabel = new Label("");
            scoreValueLabel.AddToClassList("character-creation-ability-score-value");
            scoreValueLabel.name = $"ability-{elementKey}-score-label";
            scoreDropZone.Add(scoreValueLabel);
            scoreDropZone.pickingMode = PickingMode.Position;

            TextField scoreEntry = new TextField();
            scoreEntry.name = $"ability-{elementKey}-score-entry";
            scoreEntry.AddToClassList("character-creation-ability-score-entry-field");
            scoreEntry.style.display = DisplayStyle.Flex;
            scoreEntry.style.visibility = Visibility.Hidden;
            scoreEntry.maxLength = 2;
            scoreEntry.isDelayed = true;

            scoreEntry.RegisterValueChangedCallback(evt =>
            {
                if (binding.ShouldSuppressManualAbilityEntryEvents())
                    return;
                string raw = evt.newValue ?? "";
                char[] digits = Array.FindAll(raw.ToCharArray(), char.IsDigit);
                string digitsOnly = new string(digits);
                if (digitsOnly != raw)
                {
                    scoreEntry.SetValueWithoutNotify(digitsOnly);
                    binding.OnManualAbilityEntryChanged(abilityIndex, digitsOnly);
                    return;
                }
                binding.OnManualAbilityEntryChanged(abilityIndex, digitsOnly);
            });
            scoreEntry.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (binding.ShouldSuppressManualAbilityEntryEvents())
                    return;
                binding.OnManualAbilityEntryChanged(abilityIndex, scoreEntry.value ?? "");
            });

            VisualElement scoreInputSlot = new VisualElement();
            scoreInputSlot.AddToClassList("character-creation-ability-score-input-slot");
            scoreInputSlot.Add(scoreDropZone);
            scoreInputSlot.Add(scoreEntry);
            return (scoreInputSlot, scoreEntry);
        }

        private static VisualElement CreateModifierBox(string elementKey)
        {
            VisualElement modValueBox = new VisualElement();
            modValueBox.AddToClassList("character-creation-ability-mod-box");
            var modValue = new Label("—");
            modValue.AddToClassList("character-creation-ability-modifier-value");
            modValue.name = $"ability-mod-{elementKey}";
            modValueBox.Add(modValue);
            return modValueBox;
        }
    }
}
