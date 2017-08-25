﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A list of command or selection items.
    /// </summary>
    public class Menu<TItem> : CompositeDrawable, IStateful<MenuState>
        where TItem : MenuItem
    {
        /// <summary>
        /// Gets or sets the <see cref="TItem"/>s contained within this <see cref="Menu{TItem}"/>.
        /// </summary>
        public IReadOnlyList<TItem> Items
        {
            get { return itemsContainer.Select(r => r.Model).ToList(); }
            set
            {
                itemsContainer.ChildrenEnumerable = value.Select(CreateDrawableMenuItem);
                menuWidth.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the corner radius of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public new float CornerRadius
        {
            get { return base.CornerRadius; }
            set { base.CornerRadius = value; }
        }

        /// <summary>
        /// Gets or sets whether the scroll bar of this <see cref="Menu{TItem}"/> is visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get { return scrollContainer.ScrollbarVisible; }
            set { scrollContainer.ScrollbarVisible = value; }
        }

        public new Axes RelativeSizeAxes
        {
            get { return base.RelativeSizeAxes; }
            set { throw new InvalidOperationException($"{nameof(Menu<TItem>)} will determine its size based on the value of {nameof(UseParentWidth)}."); }
        }

        public new Axes AutoSizeAxes
        {
            get { return base.AutoSizeAxes; }
            set { throw new InvalidOperationException($"{nameof(Menu<TItem>)} will determine its size based on the value of {nameof(UseParentWidth)}."); }
        }

        private bool useParentWidth;
        public bool UseParentWidth
        {
            get { return useParentWidth; }
            set
            {
                useParentWidth = value;
                menuWidth.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the background colour of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public Color4 BackgroundColour
        {
            get { return background.Colour; }
            set { background.Colour = value; }
        }

        private Cached menuWidth = new Cached();

        private readonly Box background;
        private readonly ScrollContainer scrollContainer;
        private readonly FlowContainer<DrawableMenuItem> itemsContainer;

        public Menu()
        {
            Masking = true;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                scrollContainer = new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = false,
                    Child = itemsContainer = CreateItemsFlow()
                }
            };

            itemsContainer.RelativeSizeAxes = Axes.X;
            itemsContainer.AutoSizeAxes = Axes.Y;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (State == MenuState.Closed)
                AnimateClose();
            else
                AnimateOpen();
        }

        /// <summary>
        /// Adds a <see cref="TItem"/> to this <see cref="Menu{TItem}"/>.
        /// </summary>
        /// <param name="item">The <see cref="TItem"/> to add.</param>
        public void Add(TItem item)
        {
            var drawableItem = CreateDrawableMenuItem(item);
            drawableItem.CloseRequested = Close;

            itemsContainer.Add(drawableItem);
            menuWidth.Invalidate();
        }

        /// <summary>
        /// Removes a <see cref="TItem"/> from this <see cref="Menu{TItem}"/>.
        /// </summary>
        /// <param name="item">The <see cref="TItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(TItem item) => itemsContainer.RemoveAll(r => r.Model == item) > 0;

        /// <summary>
        /// Clears all <see cref="TItem"/>s in this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Clear() => itemsContainer.Clear();

        /// <summary>
        /// Gets the model representations contained by this <see cref="Menu"/>.
        /// </summary>
        protected IReadOnlyList<DrawableMenuItem> Children => itemsContainer;

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public MenuState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;
                state = value;

                switch (value)
                {
                    case MenuState.Closed:
                        AnimateClose();
                        if (HasFocus)
                            GetContainingInputManager().ChangeFocus(null);
                        break;
                    case MenuState.Opened:
                        AnimateOpen();

                        //schedule required as we may not be present currently.
                        Schedule(() => GetContainingInputManager().ChangeFocus(this));
                        break;
                }

                UpdateMenuHeight();
            }
        }

        /// <summary>
        /// Opens this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Open() => State = MenuState.Opened;

        /// <summary>
        /// Closes this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Close() => State = MenuState.Closed;

        /// <summary>
        /// Toggles the state of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Toggle() => State = State == MenuState.Closed ? MenuState.Opened : MenuState.Closed;

        private float maxHeight = float.MaxValue;
        /// <summary>
        /// Gets or sets maximum height allowable by this <see cref="Menu{TItem}"/>.
        /// </summary>
        public float MaxHeight
        {
            get { return maxHeight; }
            set
            {
                maxHeight = value;
                UpdateMenuHeight();
            }
        }

        /// <summary>
        /// Animates the opening of this <see cref="Menu{TItem}"/>.
        /// </summary>
        protected virtual void AnimateOpen() => Show();

        /// <summary>
        /// Animates the closing of this <see cref="Menu{TItem}"/>.
        /// </summary>
        protected virtual void AnimateClose() => Hide();

        public override bool AcceptsFocus => true;
        protected override bool OnClick(InputState state) => true;
        protected override void OnFocusLost(InputState state) => State = MenuState.Closed;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            UpdateMenuHeight();
            updateMenuWidth();
        }

        /// <summary>
        /// The height of the <see cref="TItem"/>s contained by this <see cref="Menu{TItem}"/>, clamped by <see cref="MaxHeight"/>.
        /// </summary>
        protected float ContentHeight => Math.Min(itemsContainer.Height, MaxHeight);

        /// <summary>
        /// Computes and applies the height of this <see cref="Menu{TItem}"/>.
        /// </summary>
        protected virtual void UpdateMenuHeight() => Height = ContentHeight;

        private void updateMenuWidth()
        {
            if (menuWidth.IsValid)
                return;

            if (UseParentWidth)
            {
                base.RelativeSizeAxes = Axes.X;
                Width = 1;
            }
            else
            {
                // We're now defining the size of ourselves based on our children, but our children are relatively-sized, so we need to compute our size ourselves
                float textWidth = 0;

                foreach (var item in Children)
                    textWidth = Math.Max(textWidth, item.TextDrawWidth);

                Width = textWidth;
            }

            menuWidth.Validate();
        }

        /// <summary>
        /// Creates the visual representation for a <see cref="TItem"/>.
        /// </summary>
        /// <param name="model">The <see cref="TItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected virtual DrawableMenuItem CreateDrawableMenuItem(TItem model) => new DrawableMenuItem(model);

        protected virtual FlowContainer<DrawableMenuItem> CreateItemsFlow() => new FillFlowContainer<DrawableMenuItem> { Direction = FillDirection.Vertical };

        #region DrawableMenuItem
        protected class DrawableMenuItem : CompositeDrawable
        {
            public readonly TItem Model;

            /// <summary>
            /// Fired generally when this item was clicked and requests the containing menu to close itself.
            /// </summary>
            public Action CloseRequested;

            private readonly Drawable content;

            protected readonly Box Background;
            protected readonly Container Foreground;

            public DrawableMenuItem(TItem model)
            {
                Model = model;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                InternalChildren = new Drawable[]
                {
                    Background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    Foreground = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Child = content = CreateContent(),
                    },
                };
            }

            private Color4 backgroundColour = Color4.DarkSlateGray;
            /// <summary>
            /// Gets or sets the default background colour.
            /// </summary>
            public Color4 BackgroundColour
            {
                get { return backgroundColour; }
                set
                {
                    backgroundColour = value;
                    AnimateBackground(IsHovered);
                }
            }

            private Color4 foregroundColour = Color4.White;
            /// <summary>
            /// Gets or sets the default foreground colour.
            /// </summary>
            public Color4 ForegroundColour
            {
                get { return foregroundColour; }
                set
                {
                    foregroundColour = value;
                    AnimateForeground(IsHovered);
                }
            }

            private Color4 backgroundColourHover = Color4.DarkGray;
            /// <summary>
            /// Gets or sets the background colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 BackgroundColourHover
            {
                get { return backgroundColourHover; }
                set
                {
                    backgroundColourHover = value;
                    AnimateBackground(IsHovered);
                }
            }

            private Color4 foregroundColourHover = Color4.White;
            /// <summary>
            /// Gets or sets the foreground colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 ForegroundColourHover
            {
                get { return foregroundColourHover; }
                set
                {
                    foregroundColourHover = value;
                    AnimateForeground(IsHovered);
                }
            }

            /// <summary>
            /// The draw width of the text of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            public float TextDrawWidth => content.DrawWidth;

            protected virtual void AnimateBackground(bool hover)
            {
                Background.FadeColour(hover ? BackgroundColourHover : BackgroundColour);
            }

            protected virtual void AnimateForeground(bool hover)
            {
                Foreground.FadeColour(hover ? ForegroundColourHover : ForegroundColour);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Background.Colour = BackgroundColour;
                Foreground.Colour = ForegroundColour;
            }

            protected override bool OnHover(InputState state)
            {
                AnimateBackground(true);
                AnimateForeground(true);
                return base.OnHover(state);
            }

            protected override void OnHoverLost(InputState state)
            {
                base.OnHover(state);
                AnimateBackground(false);
                AnimateForeground(false);
            }

            protected override bool OnClick(InputState state)
            {
                if (Model.Action.Disabled)
                    return false;

                Model.Action.Value?.Invoke();
                CloseRequested?.Invoke();
                return true;
            }

            /// <summary>
            /// Creates the text which will be displayed in this <see cref="DrawableMenuItem"/>.
            /// </summary>
            protected virtual Drawable CreateContent() => new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Child = new SpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    TextSize = 17,
                    Text = Model.Text,
                }
            };
        }
        #endregion
    }

    public class Menu : Menu<MenuItem>
    {
    }

    public enum MenuState
    {
        Closed,
        Opened
    }
}
