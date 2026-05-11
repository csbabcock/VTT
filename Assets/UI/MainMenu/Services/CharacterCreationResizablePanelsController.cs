using UnityEngine;
using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Services
{
    /// <summary>
    /// Attaches character-creation panel resize handles and keeps responsive panel state in USS.
    /// </summary>
    public static class CharacterCreationResizablePanelsController
    {
        private const string BoundClass = "character-creation-resizable-panels-bound";

        private const float LeftColumnMinWidth = 240f;
        private const float LeftColumnMaxWidth = 10000f;
        private const float DetailColumnMinWidth = 360f;
        private const float DetailColumnMaxWidth = 10000f;
        private const float StatsColumnMinWidth = 300f;
        private const float StatsColumnMaxWidth = 10000f;
        private const float ColumnGapTotal = 32f;

        public static void Attach(VisualElement root)
        {
            if (root == null || root.ClassListContains(BoundClass))
                return;

            VisualElement container = root.Q<VisualElement>("container");
            VisualElement leftColumn = root.Q<VisualElement>("left-panel");
            VisualElement tabsContainer = root.Q<VisualElement>("tabs-container");
            VisualElement centerColumn = root.Q<VisualElement>("center-panel");
            VisualElement detailPanel = root.Q<VisualElement>("detail-panel");
            VisualElement rightColumn = root.Q<VisualElement>("right-panel");
            VisualElement statsPanel = root.Q<VisualElement>("stats-panel");
            VisualElement proficiencyPanel = root.Q<VisualElement>("proficiency-panel");

            if (container == null || leftColumn == null || tabsContainer == null ||
                centerColumn == null || detailPanel == null || rightColumn == null)
            {
                return;
            }

            root.AddToClassList(BoundClass);
            rightColumn.AddToClassList("character-creation-stats-proficiency-column");
            statsPanel?.AddToClassList("resizable-column-member");
            proficiencyPanel?.AddToClassList("resizable-column-member");

            AttachHorizontalHandles(
                tabsContainer,
                "tabs-container",
                leftColumn,
                LeftColumnMinWidth,
                LeftColumnMaxWidth,
                container,
                resize =>
                {
                    PrepareFixedWidthColumn(leftColumn);
                    RedistributeColumnsToFill(container, leftColumn, centerColumn, rightColumn, leftColumn, resize.Width);
                    UpdateResponsiveClasses(tabsContainer, detailPanel, rightColumn, statsPanel, proficiencyPanel);
                });

            AttachHorizontalHandles(
                detailPanel,
                "detail-panel",
                centerColumn,
                DetailColumnMinWidth,
                DetailColumnMaxWidth,
                container,
                resize =>
                {
                    PrepareFixedWidthColumn(centerColumn);
                    RedistributeColumnsToFill(container, leftColumn, centerColumn, rightColumn, centerColumn, resize.Width);
                    UpdateResponsiveClasses(tabsContainer, detailPanel, rightColumn, statsPanel, proficiencyPanel);
                });

            AttachHorizontalHandles(
                rightColumn,
                "stats-proficiency-column",
                rightColumn,
                StatsColumnMinWidth,
                StatsColumnMaxWidth,
                container,
                resize =>
                {
                    PrepareFixedWidthColumn(rightColumn);
                    RedistributeColumnsToFill(container, leftColumn, centerColumn, rightColumn, rightColumn, resize.Width);
                    UpdateResponsiveClasses(tabsContainer, detailPanel, rightColumn, statsPanel, proficiencyPanel);
                });

            EventCallback<GeometryChangedEvent> onGeometryChanged = _ =>
            {
                RedistributeColumnsToFill(container, leftColumn, centerColumn, rightColumn, null, 0f);
                UpdateResponsiveClasses(tabsContainer, detailPanel, rightColumn, statsPanel, proficiencyPanel);
            };

            root.RegisterCallback(onGeometryChanged);
            container.RegisterCallback(onGeometryChanged);
            tabsContainer.RegisterCallback(onGeometryChanged);
            detailPanel.RegisterCallback(onGeometryChanged);
            rightColumn.RegisterCallback(onGeometryChanged);

            UpdateResponsiveClasses(tabsContainer, detailPanel, rightColumn, statsPanel, proficiencyPanel);
        }

        private static void AttachHorizontalHandles(
            VisualElement handleHost,
            string namePrefix,
            VisualElement resizeTarget,
            float minWidth,
            float maxWidth,
            VisualElement boundsContainer,
            System.Action<PanelResizeChangedEvent> onResizeChanged)
        {
            VisualElement leftHandle = EnsureHandle(
                handleHost,
                $"{namePrefix}-resize-handle-left",
                "resize-handle",
                "resize-handle-left");
            leftHandle.AddManipulator(new ResizablePanelManipulator(
                resizeTarget,
                ResizeDirection.Left,
                minWidth,
                maxWidth,
                boundsContainer: boundsContainer,
                onResizeChanged: resize => onResizeChanged?.Invoke(resize)));

            VisualElement rightHandle = EnsureHandle(
                handleHost,
                $"{namePrefix}-resize-handle-right",
                "resize-handle",
                "resize-handle-right");
            rightHandle.AddManipulator(new ResizablePanelManipulator(
                resizeTarget,
                ResizeDirection.Right,
                minWidth,
                maxWidth,
                boundsContainer: boundsContainer,
                onResizeChanged: resize => onResizeChanged?.Invoke(resize)));
        }

        private static VisualElement EnsureHandle(VisualElement host, string name, params string[] classes)
        {
            VisualElement handle = host.Q<VisualElement>(name);
            if (handle == null)
            {
                handle = new VisualElement { name = name };
                host.Add(handle);
            }

            foreach (string className in classes)
                handle.AddToClassList(className);

            handle.pickingMode = PickingMode.Position;
            handle.BringToFront();
            return handle;
        }

        private static void PrepareFixedWidthColumn(VisualElement column)
        {
            if (column == null)
                return;

            column.style.flexGrow = 0f;
            column.style.flexShrink = 1f;
        }

        private static void RedistributeColumnsToFill(
            VisualElement container,
            VisualElement leftColumn,
            VisualElement centerColumn,
            VisualElement rightColumn,
            VisualElement resizedColumn,
            float resizedWidth)
        {
            if (container == null || container.resolvedStyle.width <= 0f)
                return;

            float available = container.resolvedStyle.width
                - container.resolvedStyle.paddingLeft
                - container.resolvedStyle.paddingRight
                - ColumnGapTotal;
            if (available <= 0f)
                return;

            float[] widths =
            {
                Mathf.Clamp(leftColumn.resolvedStyle.width, LeftColumnMinWidth, LeftColumnMaxWidth),
                Mathf.Clamp(centerColumn.resolvedStyle.width, DetailColumnMinWidth, DetailColumnMaxWidth),
                Mathf.Clamp(rightColumn.resolvedStyle.width, StatsColumnMinWidth, StatsColumnMaxWidth)
            };
            float[] minWidths = { LeftColumnMinWidth, DetailColumnMinWidth, StatsColumnMinWidth };
            float[] maxWidths = CreateEffectiveMaxWidths(
                available,
                minWidths,
                new[] { LeftColumnMaxWidth, DetailColumnMaxWidth, StatsColumnMaxWidth });
            ApplyResizedWidthOverride(widths, minWidths, maxWidths, leftColumn, centerColumn, rightColumn, resizedColumn, resizedWidth);
            bool[] adjustable =
            {
                resizedColumn != leftColumn,
                resizedColumn != centerColumn,
                resizedColumn != rightColumn
            };

            float delta = available - (widths[0] + widths[1] + widths[2]);
            if (Mathf.Abs(delta) > 0.5f)
            {
                bool grow = delta > 0f;
                float leftover = DistributeEvenly(widths, minWidths, maxWidths, adjustable, Mathf.Abs(delta), grow);

                // If the non-dragged columns hit their clamps, let the dragged column absorb the rest
                // so the row still fills the container whenever the configured clamps allow it.
                if (leftover > 0.5f && resizedColumn != null)
                {
                    bool[] fallbackAdjustable =
                    {
                        resizedColumn == leftColumn,
                        resizedColumn == centerColumn,
                        resizedColumn == rightColumn
                    };
                    DistributeEvenly(widths, minWidths, maxWidths, fallbackAdjustable, leftover, grow);
                }
            }

            ApplyWidthIfChanged(leftColumn, widths[0]);
            ApplyWidthIfChanged(centerColumn, widths[1]);
            ApplyWidthIfChanged(rightColumn, widths[2]);
        }

        private static float[] CreateEffectiveMaxWidths(float available, float[] minWidths, float[] configuredMaxWidths)
        {
            float minTotal = minWidths[0] + minWidths[1] + minWidths[2];
            var result = new float[minWidths.Length];
            for (int i = 0; i < minWidths.Length; i++)
            {
                float maxBySiblingMinimums = available - (minTotal - minWidths[i]);
                result[i] = Mathf.Max(minWidths[i], Mathf.Min(configuredMaxWidths[i], maxBySiblingMinimums));
            }

            return result;
        }

        private static void ApplyResizedWidthOverride(
            float[] widths,
            float[] minWidths,
            float[] maxWidths,
            VisualElement leftColumn,
            VisualElement centerColumn,
            VisualElement rightColumn,
            VisualElement resizedColumn,
            float resizedWidth)
        {
            if (resizedColumn == null || resizedWidth <= 0f)
                return;

            if (resizedColumn == leftColumn)
                widths[0] = Mathf.Clamp(resizedWidth, minWidths[0], maxWidths[0]);
            else if (resizedColumn == centerColumn)
                widths[1] = Mathf.Clamp(resizedWidth, minWidths[1], maxWidths[1]);
            else if (resizedColumn == rightColumn)
                widths[2] = Mathf.Clamp(resizedWidth, minWidths[2], maxWidths[2]);
        }

        private static float DistributeEvenly(
            float[] widths,
            float[] minWidths,
            float[] maxWidths,
            bool[] adjustable,
            float amount,
            bool grow)
        {
            float remaining = amount;
            while (remaining > 0.5f)
            {
                int adjustableCount = 0;
                for (int i = 0; i < widths.Length; i++)
                {
                    if (!adjustable[i])
                        continue;

                    float capacity = grow ? maxWidths[i] - widths[i] : widths[i] - minWidths[i];
                    if (capacity > 0.5f)
                        adjustableCount++;
                }

                if (adjustableCount == 0)
                    break;

                float share = remaining / adjustableCount;
                float distributed = 0f;
                for (int i = 0; i < widths.Length; i++)
                {
                    if (!adjustable[i])
                        continue;

                    float capacity = grow ? maxWidths[i] - widths[i] : widths[i] - minWidths[i];
                    if (capacity <= 0.5f)
                        continue;

                    float change = Mathf.Min(share, capacity);
                    widths[i] += grow ? change : -change;
                    distributed += change;
                }

                if (distributed <= 0.5f)
                    break;

                remaining -= distributed;
            }

            return remaining;
        }

        private static void ApplyWidthIfChanged(VisualElement element, float width)
        {
            if (element == null || Mathf.Abs(element.resolvedStyle.width - width) <= 0.5f)
                return;

            element.style.width = width;
            element.style.flexBasis = width;
        }

        private static void UpdateResponsiveClasses(
            VisualElement tabsContainer,
            VisualElement detailPanel,
            VisualElement rightColumn,
            VisualElement statsPanel,
            VisualElement proficiencyPanel)
        {
            SetWidthState(tabsContainer, tabsContainer?.resolvedStyle.width ?? 0f, 280f, 430f);
            SetWidthState(detailPanel, detailPanel?.resolvedStyle.width ?? 0f, 360f, 620f);
            SetWidthState(rightColumn, rightColumn?.resolvedStyle.width ?? 0f, 300f, 500f);
            SetWidthState(statsPanel, rightColumn?.resolvedStyle.width ?? 0f, 300f, 500f);
            SetWidthState(proficiencyPanel, rightColumn?.resolvedStyle.width ?? 0f, 300f, 500f);

            tabsContainer?.AddToClassList("scrollable-content");
            detailPanel?.AddToClassList("scrollable-content");
            statsPanel?.AddToClassList("scrollable-content");
            proficiencyPanel?.AddToClassList("scrollable-content");
            proficiencyPanel?.AddToClassList("chips-container--wrapped");
        }

        private static void SetWidthState(VisualElement element, float width, float compactMax, float wideMin)
        {
            if (element == null || width <= 0f)
                return;

            element.RemoveFromClassList("panel--compact");
            element.RemoveFromClassList("panel--normal");
            element.RemoveFromClassList("panel--wide");

            if (width < compactMax)
                element.AddToClassList("panel--compact");
            else if (width >= wideMin)
                element.AddToClassList("panel--wide");
            else
                element.AddToClassList("panel--normal");
        }
    }
}
