// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Online.API.Huismetbenen;
using osu.Game.Online.Chat;
using osu.Game.Rulesets;

namespace PerformanceCalculatorGUI.Components
{

    public struct ReworkDropdownSelect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ReworkDropdownSelect(string id, string name) {
            Id = id;
            Name = name;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    internal class SettingsPopover : OsuPopover
    {
        private SettingsManager configManager;

        private LinkFlowContainer linkContainer;

        private Bindable<string> clientIdBindable;
        private Bindable<string> clientSecretBindable;
        private Bindable<string> pathBindable;
        private Bindable<string> cacheBindable;
        private Bindable<float> scaleBindable;
        private Bindable<string> reworkIdBindable;
        private Bindable<ReworkDropdownSelect> reworkBindable;
        private Bindable<string> lazerPathBindable;

        private const string api_key_link = "https://osu.ppy.sh/home/account/edit#new-oauth-application";

        [BackgroundDependencyLoader]
        private async void load(SettingsManager configManager, APIManager apiManager, OsuConfigManager osuConfig, Bindable<RulesetInfo> ruleset)
        {
            this.configManager = configManager;
            clientIdBindable = configManager.GetBindable<string>(Settings.ClientId);
            clientSecretBindable = configManager.GetBindable<string>(Settings.ClientSecret);
            pathBindable = configManager.GetBindable<string>(Settings.DefaultPath);
            cacheBindable = configManager.GetBindable<string>(Settings.CachePath);
            scaleBindable = osuConfig.GetBindable<float>(OsuSetting.UIScale);
            reworkIdBindable = configManager.GetBindable<string>(Settings.ReworkId);
            lazerPathBindable = configManager.GetBindable<string>(Settings.LazerPath);
            
            List<APIRework> reworks = await apiManager.GetJsonFromHuismetbenenApi<List<APIRework>>("/reworks/list");
            ReworkDropdownSelect[] reworkItems = reworks.FindAll((r) => r.Gamemode == ruleset.Value.OnlineID)
                .Select((r) => new ReworkDropdownSelect(r.Id.ToString(), r.Name))
                .Prepend(new ReworkDropdownSelect(string.Empty, "None"))
                .ToArray();

            ReworkDropdownSelect reworkItem = reworkItems.FirstOrDefault((r) => r.Id == reworkIdBindable.Value, reworkItems[0]);
            reworkBindable = new Bindable<ReworkDropdownSelect>(reworkItem);
            reworkBindable.ValueChanged += (v) => reworkIdBindable.Value = v.NewValue.Id;

            Add(new Container
            {
                AutoSizeAxes = Axes.Y,
                Width = 600,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(18),
                        Children = new Drawable[]
                        {
                            linkContainer = new LinkFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Text = "You can get API key from "
                            },
                            new LabelledNumberBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Client ID",
                                Current = { BindTarget = clientIdBindable }
                            },
                            new LabelledPasswordTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Client Secret",
                                Current = { BindTarget = clientSecretBindable }
                            },
                            new ExtendedLabelledDropdown<ReworkDropdownSelect>{
                                Label = "Replace Live PP with Rework PP",
                                RelativeSizeAxes = Axes.X,
                                Items = reworkItems,
                                Current = { BindTarget = reworkBindable }
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Size = new Vector2(0.8f, 3f),
                                Colour = OsuColour.Gray(0.5f)
                            },
                            new LabelledTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Default file path",
                                Current = { BindTarget = pathBindable }
                            },
                            new LabelledTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Beatmap cache path",
                                Current = { BindTarget = cacheBindable }
                            },
                            new LabelledTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Label = "Lazer Directory",
                                Current = { BindTarget = lazerPathBindable }
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Size = new Vector2(0.8f, 3f),
                                Colour = OsuColour.Gray(0.5f)
                            },
                            new LabelledSliderBar<float>
                            {
                                RelativeSizeAxes = Axes.X,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Label = "UI Scale",
                                Current = { BindTarget = scaleBindable }
                            },
                            new OsuButton
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Width = 150,
                                Height = 40,
                                Text = "Save",
                                Action = saveConfig
                            }
                        }
                    }
                }
            });

            linkContainer.AddLink("here", LinkAction.External, api_key_link, api_key_link);
        }

        private void saveConfig()
        {
            configManager.Save();

            this.HidePopover();
        }
    }
}
