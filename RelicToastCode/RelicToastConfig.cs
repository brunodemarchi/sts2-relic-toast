using System;
using System.Reflection;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace RelicToast;

internal enum RelicToastAnimation
{
    None,
    Fade,
    SlideLeftRight,
    SlideRightLeft,
    SlideTopBottom,
    SlideBottomTop
}

internal enum RelicToastPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

internal sealed class RelicToastConfig : SimpleModConfig
{
    private const string RandomTestRelicId = "__RANDOM__";
    public const int MinDurationMs = 500;

    public RelicToastConfig()
    {
        ModId = RelicToastMod.ModId;
    }

    public static bool ShowInModSettings { get; set; } = true;

    public static bool Enabled { get; set; } = true;

    public static string TestRelicId { get; set; } = RandomTestRelicId;

    [ConfigSlider(MinDurationMs, 10000, 250, Format = "{0} ms")]
    public static int DurationMs { get; set; } = 3000;

    public static RelicToastAnimation AnimationIn { get; set; } = RelicToastAnimation.SlideLeftRight;

    public static RelicToastAnimation AnimationOut { get; set; } = RelicToastAnimation.SlideRightLeft;

    public static RelicToastPosition Position { get; set; } = RelicToastPosition.BottomLeft;

    [ConfigSlider(0.25, 2.0, 0.05, Format = "{0:0.00}x")]
    public static double Scale { get; set; } = 1.0;

    [ConfigSlider(-500, 500, 5, Format = "{0} px")]
    public static int OffsetX { get; set; }

    [ConfigSlider(-500, 500, 5, Format = "{0} px")]
    public static int OffsetY { get; set; }

    [ConfigSlider(0, 2000, 25, Format = "{0} ms")]
    public static int AnimationInMs { get; set; } = 400;

    [ConfigSlider(0, 2000, 25, Format = "{0} ms")]
    public static int AnimationOutMs { get; set; } = 400;

    [ConfigSlider(0, 2000, 25, Format = "{0} ms")]
    public static int QueueDelayMs { get; set; } = 100;

    public static float DurationSeconds => Math.Max(MinDurationMs, DurationMs) / 1000.0f;

    public static float AnimationInSeconds => Math.Max(0, AnimationInMs) / 1000.0f;

    public static float AnimationOutSeconds => Math.Max(0, AnimationOutMs) / 1000.0f;

    public static float QueueDelaySeconds => Math.Max(0, QueueDelayMs) / 1000.0f;

    public static float ScaleFactor => Math.Clamp((float)Scale, 0.25f, 2.0f);

    public override void SetupConfigUI(Control optionContainer)
    {
        optionContainer.AddChild(CreateSectionHeader("Relic Toast", alignToTop: true));
        AddToggle(optionContainer, "Enabled", nameof(Enabled));
        AddDropdown(optionContainer, "Animation In", nameof(AnimationIn));
        AddDropdown(optionContainer, "Animation Out", nameof(AnimationOut));
        AddDropdown(optionContainer, "Position", nameof(Position));
        AddSlider(optionContainer, "Scale", nameof(Scale));
        AddSlider(optionContainer, "Offset X", nameof(OffsetX));
        AddSlider(optionContainer, "Offset Y", nameof(OffsetY));
        AddSlider(optionContainer, "Time On Screen", nameof(DurationMs));
        AddSlider(optionContainer, "In Duration", nameof(AnimationInMs));
        AddSlider(optionContainer, "Out Duration", nameof(AnimationOutMs));
        AddSlider(optionContainer, "Queue Delay", nameof(QueueDelayMs));
        AddTestRelicDropdown(optionContainer);
        optionContainer.AddChild(CreateButton("Test Toast", "Show", SendTestToast, addHoverTip: false));
        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }

    public static void SendTestToast()
    {
        try
        {
            var relic = ResolveTestRelic();
            RelicToastDebugLog.Write("Settings test toast requested.");
            CompactRelicToast.Show(relic);
        }
        catch (Exception ex)
        {
            RelicToastMod.Logger.Warn($"Test relic toast failed: {ex.Message}");
            RelicToastDebugLog.Write($"Test relic toast failed: {ex}");
        }
    }

    private static RelicModel ResolveTestRelic()
    {
        var relics = GetAllTestRelics();
        if (relics.Count == 0)
        {
            return ModelDb.Relic<Akabeko>();
        }

        if (TestRelicId == RandomTestRelicId)
        {
            return relics[Random.Shared.Next(relics.Count)];
        }

        var relic = relics.FirstOrDefault(candidate => candidate.Id.Entry == TestRelicId);
        if (relic != null)
        {
            return relic;
        }

        RelicToastDebugLog.Write($"Configured test relic '{TestRelicId}' was not found; using a random relic.");
        return relics[Random.Shared.Next(relics.Count)];
    }

    private static List<RelicModel> GetAllTestRelics()
    {
        return ModelDb.AllRelics
            .OrderBy(relic => SafeRelicTitle(relic), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(relic => relic.Id.Entry, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string SafeRelicTitle(RelicModel relic)
    {
        try
        {
            var title = relic.Title.GetFormattedText();
            return string.IsNullOrWhiteSpace(title) ? relic.Id.Entry : title;
        }
        catch (Exception ex)
        {
            RelicToastDebugLog.Write($"Failed to format test relic title for {relic.Id}: {ex.GetType().Name}: {ex.Message}");
            return relic.Id.Entry;
        }
    }

    private void AddSlider(Control optionContainer, string label, string propertyName)
    {
        var property = GetConfigProperty(propertyName);
        var labelControl = ModConfig.CreateRawLabelControl(label, 28);
        var sliderControl = CreateRawSliderControl(property);
        var row = new NConfigOptionRow(ModPrefix, propertyName, labelControl, sliderControl);

        optionContainer.AddChild(row);
    }

    private void AddTestRelicDropdown(Control optionContainer)
    {
        var labelControl = ModConfig.CreateRawLabelControl("Test Relic", 28);
        var dropdownControl = CreateTestRelicDropdownControl();
        var row = new NConfigOptionRow(ModPrefix, nameof(TestRelicId), labelControl, dropdownControl);

        optionContainer.AddChild(row);
    }

    private Control CreateTestRelicDropdownControl()
    {
        var dropdown = new NConfigDropdown();
        var items = CreateTestRelicDropdownItems();

        SetInstanceField(dropdown, "_items", items);
        SetInstanceField(dropdown, "_currentDisplayIndex", CurrentTestRelicIndex(items));

        var positioner = new NDropdownPositioner
        {
            CustomMinimumSize = new Vector2(324, 64),
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Control.SizeFlags.Fill
        };
        SetInstanceField(positioner, "_dropdownNode", dropdown);
        positioner.AddChild(dropdown);

        return positioner;
    }

    private List<NConfigDropdownItem.ItemData> CreateTestRelicDropdownItems()
    {
        var items = new List<NConfigDropdownItem.ItemData>
        {
            new("Random", RandomTestRelicId, () =>
            {
                TestRelicId = RandomTestRelicId;
                Changed();
            })
        };

        foreach (var relic in GetAllTestRelics())
        {
            var relicId = relic.Id.Entry;
            var label = SafeRelicTitle(relic);
            items.Add(new NConfigDropdownItem.ItemData(label, relicId, () =>
            {
                TestRelicId = relicId;
                Changed();
            }));
        }

        return items;
    }

    private static int CurrentTestRelicIndex(List<NConfigDropdownItem.ItemData> items)
    {
        var index = items.FindIndex(item => item.Value is string value && value == TestRelicId);
        return Math.Max(0, index);
    }

    private static void SetInstanceField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        field.SetValue(target, value);
    }

    private void AddDropdown(Control optionContainer, string label, string propertyName)
    {
        var property = GetConfigProperty(propertyName);
        var labelControl = ModConfig.CreateRawLabelControl(label, 28);
        var dropdownControl = CreateRawDropdownControl(property);
        var row = new NConfigOptionRow(ModPrefix, propertyName, labelControl, dropdownControl);

        optionContainer.AddChild(row);
    }

    private void AddToggle(Control optionContainer, string label, string propertyName)
    {
        var property = GetConfigProperty(propertyName);
        var labelControl = ModConfig.CreateRawLabelControl(label, 28);
        var toggleControl = CreateRawTickboxControl(property);
        var row = new NConfigOptionRow(ModPrefix, propertyName, labelControl, toggleControl);

        optionContainer.AddChild(row);
    }

    private static PropertyInfo GetConfigProperty(string propertyName)
    {
        return typeof(RelicToastConfig).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Static
        ) ?? throw new InvalidOperationException($"Missing config property '{propertyName}'.");
    }
}
