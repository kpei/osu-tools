// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

namespace PerformanceCalculatorGUI.Components
{
    public class ExtendedLabelledDropdown<TItem> : LabelledComponent<OsuDropdown<TItem>, TItem>
    {
        public ExtendedLabelledDropdown()
            : base(false)
        {
        }

        public IEnumerable<TItem> Items
        {
            get => Component.Items;
            set => Component.Items = value;
        }

        protected sealed override OsuDropdown<TItem> CreateComponent() => CreateDropdown().With(d =>
        {
            d.RelativeSizeAxes = Axes.X;
            d.Width = 1f;
        });

        protected virtual OsuDropdown<TItem> CreateDropdown() => new OsuDropdown<TItem>();
    }
}
