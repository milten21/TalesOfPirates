using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Top.Client.Tools.Debug;

public partial class DebugMenu : DebugWindow
{
    private readonly Dictionary<DebugFeature, CheckBox> _checkBoxes = [];

    public void Setup(IReadOnlyList<DebugFeature> features)
    {
        Name = nameof(DebugMenu);
        Title = "Debug";
        Position = new Vector2I(24, 160);
        Size = new Vector2I(280, 360);
        Visible = false;

        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

        foreach (var group in Enum.GetValues<DebugGroup>())
        {
            var groupFeatures = features.Where(feature => feature.Group == group).ToList();

            if (groupFeatures.Count == 0)
            {
                continue;
            }

            list.AddChild(new Label { Text = group.ToString() });

            foreach (var feature in groupFeatures)
            {
                var checkBox = new CheckBox { Text = feature.DisplayName };

                checkBox.Toggled += isOn => feature.IsOn = isOn;

                list.AddChild(checkBox);

                _checkBoxes.Add(feature, checkBox);
            }
        }

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };

        scroll.AddChild(list);

        AddContent(scroll);

        CloseRequested += Hide;
    }

    public void Refresh(Node? scene)
    {
        foreach (var (feature, checkBox) in _checkBoxes)
        {
            checkBox.Disabled = !feature.IsAvailable(scene);
            checkBox.SetPressedNoSignal(feature.IsOn);
        }
    }
}
