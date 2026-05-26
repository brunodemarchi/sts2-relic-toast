using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace RelicToast;

internal static class CompactRelicToast
{
    private const string HoverTipTexturePath = "res://images/ui/hover_tip.png";
    private const string FontBoldPath = "res://themes/kreon_bold_glyph_space_one.tres";
    private const string FontRegularPath = "res://themes/kreon_regular_glyph_space_one.tres";
    private const float DefaultPanelWidth = 780.0f;
    private const float PanelHeight = 230.0f;
    private const float MaxPanelHeight = 360.0f;

    private static readonly Queue<RelicModel> Queue = new();
    private static readonly Dictionary<string, string> TagColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aqua"] = "#2AEBBE",
        ["blue"] = "#87CEEB",
        ["gold"] = "#EFC851",
        ["green"] = "#7FFF00",
        ["orange"] = "#FFA518",
        ["pink"] = "#FF78A0",
        ["purple"] = "#EE82EE",
        ["red"] = "#FF5555",
        ["white"] = "#FFF6E2",
        ["cream"] = "#FFF6E2",
        ["gray"] = "#808080",
        ["grey"] = "#808080"
    };
    private static readonly Dictionary<RelicRarity, string> RarityColors = new()
    {
        [RelicRarity.None] = "#FFF6E2",
        [RelicRarity.Starter] = "#FFF6E2",
        [RelicRarity.Common] = "#FFF6E2",
        [RelicRarity.Uncommon] = "#87CEEB",
        [RelicRarity.Rare] = "#EFC851",
        [RelicRarity.Shop] = "#87CEEB",
        [RelicRarity.Event] = "#7FFF00",
        [RelicRarity.Ancient] = "#FF5555"
    };

    private static CanvasLayer? _layer;
    private static Control? _root;
    private static Texture2D? _tipTexture;
    private static Font? _fontBold;
    private static Font? _fontRegular;
    private static bool _showing;
    private static RelicModel? _lastQueuedRelic;
    private static DateTimeOffset _lastQueuedAt;

    public static void Show(RelicModel relic)
    {
        DispatchToMainThread(() => EnqueueOnMainThread(relic));
    }

    private static void EnqueueOnMainThread(RelicModel relic)
    {
        try
        {
            var game = NGame.Instance;
            if (game == null || !GodotObject.IsInstanceValid(game))
            {
                RelicToastMod.Logger.Warn("Relic toast skipped because NGame is not ready.");
                RelicToastDebugLog.Write("Relic toast skipped because NGame is not ready.");
                return;
            }

            EnsureLayer(game);
            if (IsDuplicateRecentToast(relic))
            {
                RelicToastDebugLog.Write($"Skipped duplicate compact toast for {relic.Id}.");
                return;
            }

            Queue.Enqueue(relic);
            _lastQueuedRelic = relic;
            _lastQueuedAt = DateTimeOffset.UtcNow;
            RelicToastMod.Logger.Info($"Queued relic toast for {relic.Title.GetFormattedText()}.");
            RelicToastDebugLog.Write($"Queued compact toast for {relic.Id} / {relic.Title.GetFormattedText()}. Queue count: {Queue.Count}.");

            if (!_showing)
            {
                _ = ShowNext();
            }
        }
        catch (Exception ex)
        {
            RelicToastMod.Logger.Warn($"Relic toast queue failed: {ex.Message}");
            RelicToastDebugLog.Write($"Relic toast queue failed: {ex}");
        }
    }

    private static bool IsDuplicateRecentToast(RelicModel relic)
    {
        if (_lastQueuedRelic == null)
        {
            return false;
        }

        if ((DateTimeOffset.UtcNow - _lastQueuedAt).TotalMilliseconds > 1000)
        {
            return false;
        }

        return ReferenceEquals(_lastQueuedRelic, relic) || _lastQueuedRelic.Id == relic.Id;
    }

    private static void EnsureLayer(NGame game)
    {
        EnsureResources();

        if (IsNodeUsable(_layer) && IsNodeUsable(_root))
        {
            return;
        }

        _layer = null;
        _root = null;

        _layer = new CanvasLayer
        {
            Name = "RelicToastLayer",
            Layer = 128,
            ProcessMode = Node.ProcessModeEnum.Always
        };

        _root = new Control
        {
            Name = "RelicToastRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        _layer.AddChild(_root);
        game.GetTree().Root.AddChild(_layer);
        RelicToastDebugLog.Write(
            $"Created toast layer. TipTexture={_tipTexture != null}; FontBold={_fontBold != null}; FontRegular={_fontRegular != null}; Viewport={game.GetViewport().GetVisibleRect().Size}."
        );
    }

    private static async Task ShowNext()
    {
        try
        {
            var root = _root;
            if (root == null || !IsNodeUsable(root) || Queue.Count == 0)
            {
                _showing = false;
                return;
            }

            var tree = root.GetTree();
            if (tree == null)
            {
                _showing = false;
                return;
            }

            _showing = true;

            var relic = Queue.Dequeue();
            RelicToastDebugLog.Write($"Displaying compact toast for {relic.Id}.");
            var view = CreatePanelWithResourceRetry(relic);
            view.Panel.Scale = Vector2.One * RelicToastConfig.ScaleFactor;
            view.Panel.Modulate = new Color(1, 1, 1, 0);
            view.Panel.Position = Vector2.Zero;
            root.AddChild(view.Panel);

            await root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            FitLayout(view);
            await root.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            FitLayout(view);

            var targetPosition = ToastPosition(view.Panel);
            RelicToastDebugLog.Write($"Toast panel ready. Size={view.Panel.Size}; Scale={view.Panel.Scale}; Position={targetPosition}; Children={view.Panel.GetChildCount()}.");
            await PlayInAnimation(root, view.Panel, targetPosition);

            await root.ToSignal(tree.CreateTimer(RelicToastConfig.DurationSeconds), SceneTreeTimer.SignalName.Timeout);

            if (GodotObject.IsInstanceValid(view.Panel))
            {
                await PlayOutAnimation(root, view.Panel, targetPosition);
                view.Panel.QueueFree();
                RelicToastDebugLog.Write("Compact toast closed.");
            }
        }
        catch (Exception ex)
        {
            RelicToastMod.Logger.Warn($"Relic toast display failed: {ex.Message}");
            RelicToastDebugLog.Write($"Relic toast display failed: {ex}");
        }
        finally
        {
            if (Queue.Count > 0 && IsNodeUsable(_root))
            {
                await DelayBeforeNextQueuedToast();
                _ = ShowNext();
            }
            else
            {
                if (Queue.Count > 0)
                {
                    RelicToastDebugLog.Write($"Cleared {Queue.Count} queued toast(s) because the toast layer is no longer usable.");
                    Queue.Clear();
                }

                _showing = false;
            }
        }
    }

    private static async Task DelayBeforeNextQueuedToast()
    {
        var root = _root;
        var delaySeconds = RelicToastConfig.QueueDelaySeconds;
        if (delaySeconds <= 0 || root == null || !IsNodeUsable(root))
        {
            return;
        }

        var tree = root.GetTree();
        if (tree == null)
        {
            return;
        }

        await root.ToSignal(tree.CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
    }

    private static void EnsureResources()
    {
        var reloaded = false;

        if (!IsGodotObjectUsable(_tipTexture))
        {
            _tipTexture = ResourceLoader.Load<Texture2D>(HoverTipTexturePath);
            reloaded = true;
        }

        if (!IsGodotObjectUsable(_fontBold))
        {
            _fontBold = ResourceLoader.Load<Font>(FontBoldPath);
            reloaded = true;
        }

        if (!IsGodotObjectUsable(_fontRegular))
        {
            _fontRegular = ResourceLoader.Load<Font>(FontRegularPath);
            reloaded = true;
        }

        if (reloaded)
        {
            RelicToastDebugLog.Write($"Loaded toast resources. TipTexture={_tipTexture != null}; FontBold={_fontBold != null}; FontRegular={_fontRegular != null}.");
        }
    }

    private static void ClearResourceCache()
    {
        _tipTexture = null;
        _fontBold = null;
        _fontRegular = null;
    }

    private static bool IsGodotObjectUsable(GodotObject? instance)
    {
        try
        {
            return instance != null && GodotObject.IsInstanceValid(instance);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static bool IsNodeUsable(Node? node)
    {
        try
        {
            if (node == null || !IsGodotObjectUsable(node))
            {
                return false;
            }

            return node.IsInsideTree() && node.GetTree() != null;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static ToastView CreatePanelWithResourceRetry(RelicModel relic)
    {
        try
        {
            return CreatePanel(relic);
        }
        catch (ObjectDisposedException ex)
        {
            RelicToastDebugLog.Write($"Toast resource was disposed while creating panel; reloading resources and retrying: {ex.ObjectName}.");
            ClearResourceCache();
            EnsureResources();
            return CreatePanel(relic);
        }
    }

    private static ToastView CreatePanel(RelicModel relic)
    {
        RelicToastDebugLog.Write($"Creating panel: title='{relic.Title.GetFormattedText()}', rarity={relic.Rarity}, descriptionLength={relic.DynamicDescription.GetFormattedText().Length}.");

        var panel = new Control
        {
            Name = "RelicToastPanel",
            Size = new Vector2(DefaultPanelWidth, PanelHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var shadow = MakeTipRect("Shadow", new Vector2(8, 8), panel.Size, new Color(0, 0, 0, 0.28f), shadow: true);
        var background = MakeTipRect("Bg", Vector2.Zero, panel.Size, Colors.White, shadow: false);
        panel.AddChild(shadow);
        panel.AddChild(background);
        panel.AddChild(MakeIconFrame(relic));

        var title = MakeTitle(panel.Size, relic.Title.GetFormattedText());
        panel.AddChild(title);
        panel.AddChild(MakeRarity(panel.Size, RarityLabel(relic.Rarity), RarityColor(relic.Rarity)));

        var description = MakeDescription(panel.Size, ConvertRichTags(relic.DynamicDescription.GetFormattedText()));
        panel.AddChild(description);

        return new ToastView(panel, shadow, background, title, description);
    }

    private static NinePatchRect MakeTipRect(string name, Vector2 position, Vector2 size, Color color, bool shadow)
    {
        return new NinePatchRect
        {
            Name = name,
            Position = position,
            Size = size,
            Modulate = color,
            Texture = _tipTexture,
            RegionRect = shadow ? new Rect2(-8, -8, 339, 107) : new Rect2(0, 0, 339, 107),
            PatchMarginLeft = 55,
            PatchMarginTop = 43,
            PatchMarginRight = 91,
            PatchMarginBottom = 32,
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Stretch,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Stretch,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    private static Control MakeIconFrame(RelicModel relic)
    {
        var rarityColor = RarityColor(relic.Rarity);
        var frame = new PanelContainer
        {
            Name = "IconFrame",
            Position = new Vector2(34, 42),
            Size = new Vector2(136, 136),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.045f, 0.082f, 0.092f, 0.78f),
            BorderColor = rarityColor.Lerp(Colors.White, 0.12f),
            ShadowColor = new Color(0, 0, 0, 0.4f),
            ShadowSize = 5
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        frame.AddThemeStyleboxOverride("panel", style);

        frame.AddChild(new TextureRect
        {
            Name = "RelicIcon",
            CustomMinimumSize = new Vector2(116, 116),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = relic.BigIcon ?? relic.Icon,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });

        return frame;
    }

    private static Label MakeTitle(Vector2 panelSize, string text)
    {
        var label = new Label
        {
            Name = "Title",
            Position = new Vector2(196, 28),
            Size = new Vector2(panelSize.X - 260, 48),
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", new Color("#EFC851"));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.35f));
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        if (_fontBold != null)
        {
            label.AddThemeFontOverride("font", _fontBold);
        }
        label.AddThemeFontSizeOverride("font_size", 34);
        return label;
    }

    private static Label MakeRarity(Vector2 panelSize, string text, Color color)
    {
        var label = new Label
        {
            Name = "Rarity",
            Position = new Vector2(198, 72),
            Size = new Vector2(panelSize.X - 260, 30),
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.32f));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        if (_fontBold != null)
        {
            label.AddThemeFontOverride("font", _fontBold);
        }
        label.AddThemeFontSizeOverride("font_size", 23);
        return label;
    }

    private static RichTextLabel MakeDescription(Vector2 panelSize, string text)
    {
        var label = new RichTextLabel
        {
            Name = "Description",
            Position = new Vector2(198, 111),
            Size = new Vector2(panelSize.X - 245, 88),
            BbcodeEnabled = true,
            ScrollActive = false,
            FitContent = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("default_color", new Color("#FFF6E2"));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.32f));
        label.AddThemeConstantOverride("line_separation", -2);
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        if (_fontRegular != null)
        {
            label.AddThemeFontOverride("normal_font", _fontRegular);
        }
        if (_fontBold != null)
        {
            label.AddThemeFontOverride("bold_font", _fontBold);
        }
        SetRichSize(label, 27);
        return label;
    }

    private static void FitLayout(ToastView view)
    {
        FitTitle(view);
        FitDescription(view.Description);
        GrowPanelForDescription(view);
        FitDescription(view.Description);
    }

    private static void FitTitle(ToastView view)
    {
        if (_fontBold == null)
        {
            return;
        }

        var title = view.Title;
        var availableWidth = view.Panel.Size.X - 260.0f;
        title.Size = new Vector2(availableWidth, title.Size.Y);

        var size = 34;
        while (size > 16 && _fontBold.GetStringSize(title.Text, HorizontalAlignment.Left, -1, size).X > availableWidth)
        {
            size--;
        }

        title.AddThemeFontSizeOverride("font_size", size);
        if (size < 34)
        {
            RelicToastDebugLog.Write($"Fit title '{title.Text}' to font size {size}; availableWidth={availableWidth:0.##}.");
        }
    }

    private static void FitDescription(RichTextLabel description)
    {
        var size = 27;
        while (size > 15 && (description.GetContentHeight() > description.Size.Y || description.GetContentWidth() > description.Size.X))
        {
            size--;
            SetRichSize(description, size);
        }
    }

    private static void GrowPanelForDescription(ToastView view)
    {
        var needed = view.Description.GetContentHeight() - view.Description.Size.Y;
        if (needed <= 0)
        {
            return;
        }

        var growth = MathF.Min(needed + 18.0f, MaxPanelHeight - view.Panel.Size.Y);
        if (growth <= 0)
        {
            return;
        }

        view.Panel.Size = new Vector2(view.Panel.Size.X, view.Panel.Size.Y + growth);
        view.Background.Size = new Vector2(view.Background.Size.X, view.Panel.Size.Y);
        view.Shadow.Size = new Vector2(view.Shadow.Size.X, view.Panel.Size.Y);
        view.Description.Size = new Vector2(view.Description.Size.X, view.Description.Size.Y + growth);
    }

    private static void SetRichSize(RichTextLabel label, int size)
    {
        label.AddThemeFontSizeOverride("normal_font_size", size);
        label.AddThemeFontSizeOverride("bold_font_size", size);
        label.AddThemeFontSizeOverride("italics_font_size", size);
        label.AddThemeFontSizeOverride("bold_italics_font_size", size);
        label.AddThemeFontSizeOverride("mono_font_size", size);
    }

    private static async Task PlayInAnimation(Control signalOwner, Control panel, Vector2 targetPosition)
    {
        var animation = RelicToastConfig.AnimationIn;
        switch (animation)
        {
            case RelicToastAnimation.None:
                panel.Position = targetPosition;
                panel.Modulate = Colors.White;
                return;
            case RelicToastAnimation.Fade:
                panel.Position = targetPosition;
                panel.Modulate = new Color(1, 1, 1, 0);
                await FadePanel(signalOwner, panel, 1.0f, RelicToastConfig.AnimationInSeconds, Tween.EaseType.Out);
                return;
            case RelicToastAnimation.SlideLeftRight:
            case RelicToastAnimation.SlideRightLeft:
            case RelicToastAnimation.SlideTopBottom:
            case RelicToastAnimation.SlideBottomTop:
                panel.Position = OffscreenSlidePosition(panel, targetPosition, -SlideDirection(animation));
                panel.Modulate = Colors.White;
                await SlidePanel(signalOwner, panel, targetPosition, RelicToastConfig.AnimationInSeconds, Tween.EaseType.Out);
                return;
        }
    }

    private static async Task PlayOutAnimation(Control signalOwner, Control panel, Vector2 targetPosition)
    {
        var animation = RelicToastConfig.AnimationOut;
        switch (animation)
        {
            case RelicToastAnimation.None:
                return;
            case RelicToastAnimation.Fade:
                await FadePanel(signalOwner, panel, 0.0f, RelicToastConfig.AnimationOutSeconds, Tween.EaseType.In);
                return;
            case RelicToastAnimation.SlideLeftRight:
            case RelicToastAnimation.SlideRightLeft:
            case RelicToastAnimation.SlideTopBottom:
            case RelicToastAnimation.SlideBottomTop:
                panel.Modulate = Colors.White;
                await SlidePanel(signalOwner, panel, OffscreenSlidePosition(panel, targetPosition, SlideDirection(animation)), RelicToastConfig.AnimationOutSeconds, Tween.EaseType.In);
                return;
        }
    }

    private static async Task FadePanel(Control signalOwner, Control panel, float alpha, float durationSeconds, Tween.EaseType ease)
    {
        if (durationSeconds <= 0)
        {
            panel.Modulate = new Color(1, 1, 1, alpha);
            return;
        }

        var tween = signalOwner.CreateTween();
        tween.TweenProperty(panel, "modulate:a", alpha, durationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(ease);
        await signalOwner.ToSignal(tween, Tween.SignalName.Finished);
    }

    private static async Task SlidePanel(Control signalOwner, Control panel, Vector2 position, float durationSeconds, Tween.EaseType ease)
    {
        if (durationSeconds <= 0)
        {
            panel.Position = position;
            return;
        }

        var tween = signalOwner.CreateTween();
        tween.TweenProperty(panel, "position", position, durationSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(ease);
        await signalOwner.ToSignal(tween, Tween.SignalName.Finished);
    }

    private static Vector2 SlideDirection(RelicToastAnimation animation)
    {
        return animation switch
        {
            RelicToastAnimation.SlideLeftRight => Vector2.Right,
            RelicToastAnimation.SlideRightLeft => Vector2.Left,
            RelicToastAnimation.SlideTopBottom => Vector2.Down,
            RelicToastAnimation.SlideBottomTop => Vector2.Up,
            _ => Vector2.Zero
        };
    }

    private static Vector2 OffscreenSlidePosition(Control panel, Vector2 targetPosition, Vector2 direction)
    {
        const float padding = 32.0f;

        if (_root == null)
        {
            return targetPosition;
        }

        var viewportSize = _root.GetViewport().GetVisibleRect().Size;
        if (direction.X > 0)
        {
            return new Vector2(viewportSize.X + padding, targetPosition.Y);
        }

        if (direction.X < 0)
        {
            return new Vector2(-VisualSize(panel).X - padding, targetPosition.Y);
        }

        if (direction.Y > 0)
        {
            return new Vector2(targetPosition.X, viewportSize.Y + padding);
        }

        if (direction.Y < 0)
        {
            return new Vector2(targetPosition.X, -VisualSize(panel).Y - padding);
        }

        return targetPosition;
    }

    private static Vector2 VisualSize(Control panel)
    {
        return new Vector2(panel.Size.X * panel.Scale.X, panel.Size.Y * panel.Scale.Y);
    }

    private static Vector2 ToastPosition(Control panel)
    {
        if (_root == null)
        {
            return new Vector2(RelicToastConfig.OffsetX, RelicToastConfig.OffsetY);
        }

        var viewportSize = _root.GetViewport().GetVisibleRect().Size;
        var visualSize = VisualSize(panel);
        var centeredX = MathF.Round((viewportSize.X - visualSize.X) / 2.0f);
        var bottomY = viewportSize.Y - visualSize.Y;
        var position = RelicToastConfig.Position switch
        {
            RelicToastPosition.TopLeft => Vector2.Zero,
            RelicToastPosition.TopRight => new Vector2(viewportSize.X - visualSize.X, 0),
            RelicToastPosition.BottomLeft => new Vector2(0, bottomY),
            RelicToastPosition.BottomRight => new Vector2(viewportSize.X - visualSize.X, bottomY),
            RelicToastPosition.BottomCenter => new Vector2(centeredX, bottomY),
            _ => new Vector2(centeredX, 0)
        };

        return position + new Vector2(RelicToastConfig.OffsetX, RelicToastConfig.OffsetY);
    }

    private static string ConvertRichTags(string text)
    {
        foreach (var (tag, color) in TagColors)
        {
            text = Regex.Replace(text, $@"\[{tag}\]", $"[color={color}]", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, $@"\[/{tag}\]", "[/color]", RegexOptions.IgnoreCase);
        }

        return Regex.Replace(text, @"\[(/?)(?:plain|tip|keyword|i)\]", "", RegexOptions.IgnoreCase);
    }

    private static Color RarityColor(RelicRarity rarity)
    {
        return new Color(RarityColors.GetValueOrDefault(rarity, "#87CEEB"));
    }

    private static string RarityLabel(RelicRarity rarity)
    {
        var key = $"RELIC_RARITY.{rarity.ToString().ToUpperInvariant()}";
        try
        {
            var localized = LocString.GetIfExists("gameplay_ui", key)?.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }
        }
        catch (Exception ex)
        {
            RelicToastDebugLog.Write($"Rarity label localization failed for {key}: {ex.GetType().Name}: {ex.Message}");
        }

        return rarity switch
        {
            RelicRarity.None => "Relíquia",
            RelicRarity.Starter => "Relíquia Inicial",
            RelicRarity.Common => "Relíquia Comum",
            RelicRarity.Uncommon => "Relíquia Incomum",
            RelicRarity.Rare => "Relíquia Rara",
            RelicRarity.Shop => "Relíquia de Loja",
            RelicRarity.Event => "Relíquia de Evento",
            RelicRarity.Ancient => "Relíquia Ancestral",
            _ => $"Relíquia {rarity}"
        };
    }

    private static void DispatchToMainThread(Action action)
    {
        if (NGame.Instance != null && NGame.IsMainThread())
        {
            action();
            return;
        }

        Callable.From(action).CallDeferred();
    }

    private sealed record ToastView(
        Control Panel,
        NinePatchRect Shadow,
        NinePatchRect Background,
        Label Title,
        RichTextLabel Description
    );
}
