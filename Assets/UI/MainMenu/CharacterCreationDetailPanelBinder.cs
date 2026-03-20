using System;
using System.Collections.Generic;
using GameCore.UI.MainMenu.Services;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// Builds structured rules text blocks (D&amp;D Beyond–style) into UI Toolkit elements for the detail panel.
    /// </summary>
    public static class CharacterCreationDetailPanelBinder
    {
        public static void PopulateSectionsHost(
            VisualElement host,
            IReadOnlyList<CharacterDetailSection> sections,
            Action<Label, bool> configureRulesRichTextLabel)
        {
            if (host == null || sections == null || configureRulesRichTextLabel == null)
                return;

            host.Clear();

            foreach (CharacterDetailSection s in sections)
            {
                if (string.IsNullOrEmpty(s.Heading) && string.IsNullOrEmpty(s.Body))
                    continue;

                bool quickBuild = CharacterCreationClassContentBuilder.IsQuickBuildHeading(s.Heading);
                if (quickBuild)
                {
                    var box = new VisualElement();
                    box.AddToClassList("character-creation-detail-quick-build");
                    if (s.HasLiveAbilityHints)
                        box.AddToClassList("character-creation-detail-quick-build--live-stats");
                    if (!string.IsNullOrEmpty(s.Heading))
                    {
                        var ht = new Label(s.Heading);
                        ht.AddToClassList("character-creation-detail-quick-build-title");
                        configureRulesRichTextLabel(ht, false);
                        box.Add(ht);
                    }

                    if (!string.IsNullOrEmpty(s.Body))
                    {
                        var bt = new Label(s.Body);
                        bt.AddToClassList("character-creation-detail-section-body");
                        configureRulesRichTextLabel(bt, false);
                        box.Add(bt);
                    }

                    host.Add(box);
                }
                else
                {
                    if (!string.IsNullOrEmpty(s.Heading))
                    {
                        var h = new Label(s.Heading);
                        h.AddToClassList("character-creation-detail-section-heading");
                        configureRulesRichTextLabel(h, false);
                        if (s.HasLiveAbilityHints)
                            h.AddToClassList("character-creation-detail-section-heading--live-stats");
                        host.Add(h);
                    }

                    if (!string.IsNullOrEmpty(s.Body))
                    {
                        var b = new Label(s.Body);
                        b.AddToClassList("character-creation-detail-section-body");
                        configureRulesRichTextLabel(b, s.HasLiveAbilityHints);
                        host.Add(b);
                    }
                }
            }
        }
    }
}
