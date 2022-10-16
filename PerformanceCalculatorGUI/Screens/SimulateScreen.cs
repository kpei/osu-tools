// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.IO;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Select;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.ObjectInspection;

namespace PerformanceCalculatorGUI.Screens
{
    public class SimulateScreen : PerformanceCalculatorScreen
    {
        private ProcessorWorkingBeatmap working;

        private ExtendedUserModSelectOverlay userModsSelectOverlay;

        private GridContainer beatmapImportContainer;
        private LabelledTextBox beatmapIdTextBox;

        private LimitedLabelledNumberBox missesTextBox;
        private LimitedLabelledNumberBox comboTextBox;
        private LimitedLabelledNumberBox scoreTextBox;

        private GridContainer accuracyContainer;
        private LimitedLabelledFractionalNumberBox accuracyTextBox;
        private LimitedLabelledNumberBox goodsTextBox;
        private LimitedLabelledNumberBox mehsTextBox;
        private SwitchButton fullScoreDataSwitch;

        private DifficultyAttributes difficultyAttributes;
        private FillFlowContainer difficultyAttributesContainer;
        private FillFlowContainer performanceAttributesContainer;

        private PerformanceCalculator performanceCalculator;

        [Cached]
        private Bindable<DifficultyCalculator> difficultyCalculator = new();

        private FillFlowContainer beatmapDataContainer;
        private OsuSpriteText beatmapTitle;

        private ModDisplay modDisplay;

        private StrainVisualizer strainVisualizer;

        private ObjectInspector objectInspector;

        private BufferedContainer background;

        private ScheduledDelegate debouncedPerformanceUpdate;

        private BeatmapCarousel Carousel { get; set; }

        private Container carouselContainer;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; }

        [Resolved]
        private AudioManager audio { get; set; }

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> appliedMods { get; set; }

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved]
        private LargeTextureStore textures { get; set; }

        [Resolved]
        private SettingsManager configManager { get; set; }

        [Resolved]
        private RealmAccess? realmAccess { get; set; }

        [Resolved]
        private RealmFileStore? realmFileStore { get; set; }

        [Resolved]
        private BeatmapManager? beatmapManager { get; set; }

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        public override bool ShouldShowConfirmationDialogOnSwitch => working != null;

        private const int file_selection_container_height = 40;
        private const int map_title_container_height = 20;

        public SimulateScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(true)]
        private void load()
        {

            LoadComponentAsync(Carousel = new BeatmapCarousel
            {
                AllowSelection = false, // delay any selection until our bindables are ready to make a good choice.
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                RelativeSizeAxes = Axes.Both,
                SelectionChanged = updateSelectedBeatmap,
                BeatmapSetsChanged = carouselBeatmapsLoaded,
            }, c => carouselContainer.Child = c);

            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension() },
                    RowDimensions = new[] { 
                        new Dimension(GridSizeMode.Absolute, file_selection_container_height),
                        new Dimension(GridSizeMode.Absolute, map_title_container_height),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            beatmapImportContainer = new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(),
                                    new Dimension(GridSizeMode.Absolute),
                                    new Dimension(GridSizeMode.AutoSize)
                                },
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        beatmapIdTextBox = new LimitedLabelledNumberBox
                                        {
                                            Label = "Beatmap ID",
                                            FixedLabelWidth = 120f,
                                            PlaceholderText = "Enter beatmap ID",
                                            CommitOnFocusLoss = false
                                        }
                                    }
                                }
                            }
                        },
                    }
                },
                beatmapDataContainer = new FillFlowContainer
                {
                    Name = "Beatmap data",
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Margin = new MarginPadding { Top = file_selection_container_height },
                    Children = new Drawable[]
                    {
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Score params",
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Child = new FillFlowContainer
                            {
                                Padding = new MarginPadding(15.0f),
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 2f),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Score params"
                                    },
                                    accuracyContainer = new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        ColumnDimensions = new[]
                                        {
                                            new Dimension(),
                                            new Dimension(GridSizeMode.Absolute),
                                            new Dimension(GridSizeMode.Absolute),
                                            new Dimension(GridSizeMode.AutoSize)
                                        },
                                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                accuracyTextBox = new LimitedLabelledFractionalNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Label = "Accuracy",
                                                    PlaceholderText = "100",
                                                    MaxValue = 100.0,
                                                    MinValue = 0.0,
                                                    Value = { Value = 100.0 }
                                                },
                                                goodsTextBox = new LimitedLabelledNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Label = "Goods",
                                                    PlaceholderText = "0",
                                                    MinValue = 0
                                                },
                                                mehsTextBox = new LimitedLabelledNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Anchor = Anchor.TopLeft,
                                                    Label = "Mehs",
                                                    PlaceholderText = "0",
                                                    MinValue = 0
                                                },
                                                fullScoreDataSwitch = new SwitchButton
                                                {
                                                    Width = 80,
                                                    Height = 40
                                                }
                                            }
                                        }
                                    },
                                    missesTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Misses",
                                        PlaceholderText = "0",
                                        MinValue = 0
                                    },
                                    comboTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Combo",
                                        PlaceholderText = "0",
                                        MinValue = 0
                                    },
                                    scoreTextBox = new LimitedLabelledNumberBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        Label = "Score",
                                        PlaceholderText = "1000000",
                                        MinValue = 0,
                                        MaxValue = 1000000,
                                        Value = { Value = 1000000 }
                                    },
                                    new FillFlowContainer
                                    {
                                        Name = "Mods container",
                                        Height = 40,
                                        Direction = FillDirection.Horizontal,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y,
                                        Children = new Drawable[]
                                        {
                                            new OsuButton
                                            {
                                                Width = 100,
                                                Margin = new MarginPadding { Top = 4.0f, Right = 5.0f },
                                                Action = () => { userModsSelectOverlay.Show(); },
                                                BackgroundColour = colourProvider.Background1,
                                                Text = "Mods"
                                            },
                                            modDisplay = new ModDisplay()
                                        }
                                    },
                                    new ScalingContainer(ScalingMode.Everything)
                                    {
                                        Name = "Mod selection overlay",
                                        RelativeSizeAxes = Axes.X,
                                        Height = 300,
                                        Width = 0.75f,
                                        Scale = new Vector2(1.5f),
                                        Child = userModsSelectOverlay = new ExtendedUserModSelectOverlay
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Anchor = Anchor.TopLeft,
                                            Origin = Anchor.TopLeft,
                                            IsValidMod = mod => mod.HasImplementation && ModUtils.FlattenMod(mod).All(m => m.UserPlayable),
                                            SelectedMods = { BindTarget = appliedMods }
                                        }
                                    }
                                }
                            }
                        },
                        new OsuScrollContainer(Direction.Vertical)
                        {
                            Name = "Difficulty calculation results",
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Child = new FillFlowContainer
                            {
                                Padding = new MarginPadding(15.0f),
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 5f),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Difficulty Attributes"
                                    },
                                    difficultyAttributesContainer = new FillFlowContainer
                                    {
                                        Direction = FillDirection.Vertical,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y,
                                        Spacing = new Vector2(0, 2f)
                                    },
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Performance Attributes"
                                    },
                                    performanceAttributesContainer = new FillFlowContainer
                                    {
                                        Direction = FillDirection.Vertical,
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y,
                                        Spacing = new Vector2(0, 2f)
                                    },
                                    new OsuSpriteText
                                    {
                                        Margin = new MarginPadding(10.0f),
                                        Origin = Anchor.TopLeft,
                                        Height = 20,
                                        Text = "Strain graph (alt+scroll to zoom)"
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Anchor = Anchor.TopLeft,
                                        AutoSizeAxes = Axes.Y,
                                        Child = strainVisualizer = new StrainVisualizer()
                                    },
                                    new OsuButton
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        Width = 250,
                                        BackgroundColour = colourProvider.Background1,
                                        Text = "Inspect Object Difficulty Data",
                                        Action = () =>
                                        {
                                            if (objectInspector is not null)
                                                RemoveInternal(objectInspector);

                                            AddInternal(objectInspector = new ObjectInspector(working)
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Size = new Vector2(0.95f)
                                            });
                                            objectInspector.Show();
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                carouselContainer = new Container
                {
                    Depth = 1,
                    RelativeSizeAxes = Axes.Both,
                    Child = new LoadingSpinner(true) { State = { Value = Visibility.Visible } }
                },
            };

            beatmapDataContainer.Hide();
            userModsSelectOverlay.Hide();

            beatmapIdTextBox.OnCommit += (_, _) => { changeBeatmap(beatmapIdTextBox.Current.Value); };

            accuracyTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            goodsTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            mehsTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            missesTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            comboTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());
            scoreTextBox.Value.BindValueChanged(_ => debouncedCalculatePerformance());

            fullScoreDataSwitch.Current.BindValueChanged(val => updateAccuracyParams(val.NewValue));

            appliedMods.BindValueChanged(modsChanged);
            modDisplay.Current.BindTo(appliedMods);

            ruleset.BindValueChanged(_ =>
            {
                resetCalculations();
            });

            if (RuntimeInfo.IsDesktop)
            {
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(() =>
                {
                    calculateDifficulty();
                    calculatePerformance();
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            modSettingChangeTracker?.Dispose();

            appliedMods.UnbindAll();
            appliedMods.Value = Array.Empty<Mod>();

            difficultyCalculator.UnbindAll();
            base.Dispose(isDisposing);
        }

        private ModSettingChangeTracker modSettingChangeTracker;
        private ScheduledDelegate debouncedStatisticsUpdate;

        private BeatmapSetInfo previousBeatmapSet;

        private void updateSelectedBeatmap(BeatmapInfo beatmapInfo)
        {
            if (beatmapInfo is null) return;

            if (previousBeatmapSet is not null && previousBeatmapSet.Equals(beatmapInfo.BeatmapSet)) {
                changeBeatmap(beatmapInfo.OnlineID.ToString());
            }

            previousBeatmapSet = beatmapInfo.BeatmapSet;
            loadBackground(beatmapInfo);
        }

        private void carouselBeatmapsLoaded()
        {
            Carousel.AllowSelection = true;
        }

        private void modsChanged(ValueChangedEvent<IReadOnlyList<Mod>> mods)
        {
            modSettingChangeTracker?.Dispose();

            if (working is null)
                return;

            modSettingChangeTracker = new ModSettingChangeTracker(mods.NewValue);
            modSettingChangeTracker.SettingChanged += m =>
            {
                debouncedStatisticsUpdate?.Cancel();
                debouncedStatisticsUpdate = Scheduler.AddDelayed(() =>
                {
                    calculateDifficulty();
                    calculatePerformance();
                }, 100);
            };

            calculateDifficulty();
            updateCombo(false);
            calculatePerformance();
        }

        private void resetBeatmap()
        {
            working = null;
            // beatmapTitle.Text = string.Empty;
            resetMods();
            beatmapDataContainer.Hide();

            if (background is not null)
            {
                RemoveInternal(background);
            }
        }

        private void changeBeatmap(string beatmap)
        {
            beatmapDataContainer.Hide();

            if (string.IsNullOrEmpty(beatmap))
            {
                showError("Empty beatmap path!");
                resetBeatmap();
                return;
            }

            try
            {
                if (realmAccess != null && Int32.TryParse(beatmap, out int onlineId)) {
                    BeatmapInfo lazerBeatmap = realmAccess.Run(r => r.All<BeatmapInfo>().FirstOrDefault(b => b.OnlineID == onlineId)?.Detach());
                    if (lazerBeatmap != null) {
                        string fileStorePath = lazerBeatmap.BeatmapSet.GetPathForFile(lazerBeatmap.Path);
                        var stream = realmFileStore.Store.GetStream(fileStorePath);
                        using (var reader = new LineBufferedReader(stream))
                            working = new ProcessorWorkingBeatmap(Decoder.GetDecoder<Beatmap>(reader).Decode(reader), onlineId);
                    }
                }

                if (working == null) {
                    working = ProcessorWorkingBeatmap.FromFileOrId(beatmap, audio, configManager.GetBindable<string>(Settings.CachePath).Value);
                }
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
                return;
            }

            if (working is null)
                return;

            if (!working.BeatmapInfo.Ruleset.Equals(ruleset.Value))
            {
                ruleset.Value = working.BeatmapInfo.Ruleset;
            }
            else
            {
                resetCalculations();
            }

            // beatmapTitle.Text = $"[{ruleset.Value.Name}] {working.BeatmapInfo.GetDisplayTitle()}";

            // loadBackground(working.BeatmapInfo);

            beatmapDataContainer.Show();
            carouselContainer.Hide();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape && beatmapDataContainer.Alpha == 1) {
                beatmapDataContainer.Hide();
                userModsSelectOverlay.Hide();
                carouselContainer.Show();
                return true;
            }
            return base.OnKeyDown(e);
        }
        private void createCalculators()
        {
            if (working is null)
                return;

            var rulesetInstance = ruleset.Value.CreateInstance();
            difficultyCalculator.Value = RulesetHelper.GetExtendedDifficultyCalculator(ruleset.Value, working);
            performanceCalculator = rulesetInstance.CreatePerformanceCalculator();
        }

        private void calculateDifficulty()
        {
            if (working == null || difficultyCalculator.Value == null)
                return;

            try
            {
                difficultyAttributes = difficultyCalculator.Value.Calculate(appliedMods.Value);
                difficultyAttributesContainer.Children = AttributeConversion.ToDictionary(difficultyAttributes).Select(x =>
                    new ExtendedLabelledTextBox
                    {
                        ReadOnly = true,
                        Label = x.Key.Humanize().ToLowerInvariant(),
                        Text = FormattableString.Invariant($"{x.Value:N2}")
                    }
                ).ToArray();
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
                return;
            }

            if (difficultyCalculator.Value is IExtendedDifficultyCalculator extendedDifficultyCalculator)
            {
                // StrainSkill always skips the first object
                if (working.Beatmap?.HitObjects?.Count > 1)
                    strainVisualizer.TimeUntilFirstStrain.Value = (int)working.Beatmap.HitObjects[1].StartTime;

                strainVisualizer.Skills.Value = extendedDifficultyCalculator.GetSkills();
            }
            else
                strainVisualizer.Skills.Value = Array.Empty<Skill>();
        }

        private void debouncedCalculatePerformance()
        {
            debouncedPerformanceUpdate?.Cancel();
            debouncedPerformanceUpdate = Scheduler.AddDelayed(calculatePerformance, 20);
        }

        private void calculatePerformance()
        {
            if (working == null || difficultyAttributes == null)
                return;

            int? countGood = null, countMeh = null;

            if (fullScoreDataSwitch.Current.Value)
            {
                countGood = goodsTextBox.Value.Value;
                countMeh = mehsTextBox.Value.Value;
            }

            var score = RulesetHelper.AdjustManiaScore(scoreTextBox.Value.Value, appliedMods.Value);

            try
            {
                var beatmap = working.GetPlayableBeatmap(ruleset.Value, appliedMods.Value);

                var accuracy = accuracyTextBox.Value.Value / 100.0;
                Dictionary<HitResult, int> statistics = null;

                if (ruleset.Value.OnlineID != -1)
                {
                    // official rulesets can generate more precise hits from accuracy
                    statistics = RulesetHelper.GenerateHitResultsForRuleset(ruleset.Value, accuracyTextBox.Value.Value / 100.0, beatmap, missesTextBox.Value.Value, countMeh, countGood);
                    accuracy = RulesetHelper.GetAccuracyForRuleset(ruleset.Value, statistics);
                }

                var ppAttributes = performanceCalculator?.Calculate(new ScoreInfo(beatmap.BeatmapInfo, ruleset.Value)
                {
                    Accuracy = accuracy,
                    MaxCombo = comboTextBox.Value.Value,
                    Statistics = statistics,
                    Mods = appliedMods.Value.ToArray(),
                    TotalScore = score,
                    Ruleset = ruleset.Value
                }, difficultyAttributes);

                performanceAttributesContainer.Children = AttributeConversion.ToDictionary(ppAttributes).Select(x =>
                    new ExtendedLabelledTextBox
                    {
                        ReadOnly = true,
                        Label = x.Key.Humanize().ToLowerInvariant(),
                        Text = FormattableString.Invariant($"{x.Value:N2}")
                    }
                ).ToArray();
            }
            catch (Exception e)
            {
                showError(e);
                resetBeatmap();
            }
        }

        private void populateScoreParams()
        {
            accuracyContainer.Hide();
            comboTextBox.Hide();
            missesTextBox.Hide();
            scoreTextBox.Hide();

            if (ruleset.Value.ShortName == "osu" || ruleset.Value.ShortName == "taiko" || ruleset.Value.ShortName == "fruits")
            {
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                accuracyContainer.Show();

                updateCombo(true);
                comboTextBox.Show();
                missesTextBox.Show();
            }
            else if (ruleset.Value.ShortName == "mania")
            {
                updateAccuracyParams(fullScoreDataSwitch.Current.Value);
                accuracyContainer.Show();

                missesTextBox.Show();

                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
            else
            {
                // show everything if it's something non-official
                updateAccuracyParams(false);
                accuracyContainer.Show();

                updateCombo(true);
                comboTextBox.Show();
                missesTextBox.Show();

                scoreTextBox.Text = string.Empty;
                scoreTextBox.Show();
            }
        }

        private void updateAccuracyParams(bool useFullScoreData)
        {
            goodsTextBox.Text = string.Empty;
            goodsTextBox.Value.Value = 0;

            mehsTextBox.Text = string.Empty;
            mehsTextBox.Value.Value = 0;

            accuracyTextBox.Text = string.Empty;
            accuracyTextBox.Value.Value = 100;

            if (useFullScoreData)
            {
                goodsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "100s",
                    "taiko" => "Goods",
                    "fruits" => "Droplets",
                    _ => ""
                };

                mehsTextBox.Label = ruleset.Value.ShortName switch
                {
                    "osu" => "50s",
                    "fruits" => "Tiny Droplets",
                    _ => ""
                };

                accuracyContainer.ColumnDimensions = ruleset.Value.ShortName switch
                {
                    "osu" or "fruits" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    "taiko" =>
                        new[]
                        {
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(),
                            new Dimension(GridSizeMode.Absolute),
                            new Dimension(GridSizeMode.AutoSize)
                        },
                    _ => new[]
                    {
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.Absolute),
                        new Dimension(GridSizeMode.AutoSize)
                    }
                };

                fixupTextBox(goodsTextBox);
                fixupTextBox(mehsTextBox);
            }
            else
            {
                accuracyContainer.ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.Absolute),
                    new Dimension(GridSizeMode.AutoSize)
                };

                fixupTextBox(accuracyTextBox);
            }
        }

        private void fixupTextBox(LabelledTextBox textbox)
        {
            // This is a hack around TextBox's way of updating layout and positioning of text
            // It can only be triggered by a couple of input events and there's no way to invalidate it from the outside
            // See: https://github.com/ppy/osu-framework/blob/fd5615732033c5ea650aa5cabc8595883a2b63f5/osu.Framework/Graphics/UserInterface/TextBox.cs#L528
            textbox.TriggerEvent(new FocusEvent(new InputState(), this));
        }

        private void resetMods()
        {
            // This is temporary solution to the UX problem that people would usually want to calculate classic scores, but classic and lazer scores have different max combo
            // We append classic mod automatically so that it is immediately obvious what's going on and makes max combo same as live
            /*var classicMod = ruleset.Value.CreateInstance().CreateAllMods().SingleOrDefault(m => m is ModClassic);

            if (classicMod != null)
            {
                appliedMods.Value = new[] { classicMod };
                return;
            }*/

            appliedMods.Value = Array.Empty<Mod>();
        }

        private void resetCalculations()
        {
            createCalculators();
            resetMods();
            calculateDifficulty();
            calculatePerformance();
            populateScoreParams();
        }

        // This is to make sure combo resets when classic mod is applied
        private int previousMaxCombo;

        private void updateCombo(bool reset)
        {
            if (difficultyAttributes is null)
                return;

            missesTextBox.MaxValue = difficultyAttributes.MaxCombo;

            comboTextBox.PlaceholderText = difficultyAttributes.MaxCombo.ToString();
            comboTextBox.MaxValue = difficultyAttributes.MaxCombo;

            if (comboTextBox.Value.Value > difficultyAttributes.MaxCombo ||
                missesTextBox.Value.Value > difficultyAttributes.MaxCombo ||
                previousMaxCombo != difficultyAttributes.MaxCombo)
                reset = true;

            if (reset)
            {
                comboTextBox.Text = string.Empty;
                comboTextBox.Value.Value = difficultyAttributes.MaxCombo;
                missesTextBox.Text = string.Empty;
            }

            previousMaxCombo = difficultyAttributes.MaxCombo;
        }

        private void loadBackground(BeatmapInfo? beatmap)
        {
            if (background is not null)
            {
                RemoveInternal(background);
            }

            Texture bgTexture = null;
            if (beatmapManager != null) {
                bgTexture = beatmapManager.GetWorkingBeatmap(beatmap)?.Background;
            }
            
            if (bgTexture is null && beatmap?.BeatmapSet?.OnlineID != null) {
                bgTexture = textures.Get($"https://assets.ppy.sh/beatmaps/{beatmap.BeatmapSet.OnlineID}/covers/cover.jpg");
            }

            if (bgTexture is not null)
            {
                LoadComponentAsync(background = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = 99,
                    BlurSigma = new Vector2(6),
                    Children = new Drawable[]
                    {
                        new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            Texture = bgTexture,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FillMode = FillMode.Fill
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = OsuColour.Gray(0),
                            Alpha = 0.85f
                        },
                    }
                }).ContinueWith(_ =>
                {
                    Schedule(() =>
                    {
                        AddInternal(background);
                    });
                });
            }
        }

        private void showError(Exception e)
        {
            Logger.Log(e.ToString(), level: LogLevel.Error);

            var message = e is AggregateException aggregateException ? aggregateException.Flatten().Message : e.Message;
            showError(message, false);
        }

        private void showError(string message, bool log = true)
        {
            if (log)
                Logger.Log(message, level: LogLevel.Error);

            notificationDisplay.Display(new Notification(message));
        }
    }
}
