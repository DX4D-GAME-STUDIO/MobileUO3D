// SPDX-License-Identifier: BSD-2-Clause

using System.Linq;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MenuGump : Gump
    {
        private readonly ContainerHorizontal _container;
        private bool _isDown,
            _isLeft;
        private readonly HSliderBar _slider;

        public MenuGump(World world, uint serial, uint serv, string name) : base(world, serial, serv)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            IsFromServer = true;

            Add(new GumpPic(0, 0, 0x0910, 0));

            Add(new ColorBox(217, 49, 1) { X = 40, Y = 42 });

            Label label = new Label(name, false, 0x0386, 200, 1, FontStyle.Fixed)
            {
                X = 39,
                Y = 18
            };

            Add(label);

            _container = new ContainerHorizontal
            {
                X = 40,
                Y = 42,
                Width = 217,
                Height = 49,
                WantUpdateSize = false,
            };

            Add(_container);

            Add(
                _slider = new HSliderBar(
                    40,
                    _container.Y + _container.Height + 12,
                    217,
                    0,
                    1,
                    0,
                    HSliderBarStyle.MetalWidgetRecessedBar
                )
            );

            _slider.ValueChanged += (sender, e) =>
            {
                _container.Value = _slider.Value;
            };

            HitBox left = new HitBox(25, 60, 10, 15) { Alpha = 0f };

            left.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = true;
            };

            left.MouseUp += (sender, e) =>
            {
                _isDown = false;
            };
            Add(left);

            HitBox right = new HitBox(260, 60, 10, 15) { Alpha = 0f };

            right.MouseDown += (sender, e) =>
            {
                _isDown = true;
                _isLeft = false;
            };

            right.MouseUp += (sender, e) =>
            {
                _isDown = false;
            };
            Add(right);
        }

        public override void Update()
        {
            base.Update();

            if (_isDown)
            {
                _container.Value += _isLeft ? -1 : 1;
            }
        }

        public void AddItem(ushort graphic, ushort hue, string name, int x, int y, int index)
        {
            var view = new ItemView(graphic, hue)
            {
                X = x,
                Y = y
            };

            view.MouseDoubleClick += (sender, e) =>
            {
                NetClient.Socket.Send_MenuResponse(
                    LocalSerial,
                    (ushort)ServerSerial,
                    index,
                    graphic,
                    hue
                );
                Dispose();
                e.Result = true;
            };

            view.SetTooltip(name);

            _container.Add(view);

            _container.CalculateWidth();
            _slider.MaxValue = _container.MaxValue;
        }

        protected override void CloseWithRightClick()
        {
            base.CloseWithRightClick();

            NetClient.Socket.Send_MenuResponse(LocalSerial, (ushort)ServerSerial, 0, 0, 0);
        }

        class ItemView : Control
        {
            private readonly ushort _graphic;
            private readonly ushort _hue;
            private readonly bool _isPartial;

            public ItemView(ushort graphic, ushort hue)
            {
                AcceptMouseInput = true;
                WantUpdateSize = true;

                _graphic = graphic;

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

                Width = artInfo.UV.Width;
                Height = artInfo.UV.Height;
                _hue = hue;
                _isPartial = Client.Game.UO.FileManager.TileData.StaticData[graphic].IsPartialHue;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (_graphic != 0)
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hue, _isPartial, 1f);

                    batcher.Draw(artInfo.Texture, new Vector2(x, y), artInfo.UV, hueVector);
                }

                return base.Draw(batcher, x, y);
            }
        }

        private class ContainerHorizontal : Control
        {
            private int _value;

            public int Value
            {
                get => _value;
                set
                {
                    if (value < 0)
                    {
                        value = 0;
                    }
                    else if (value > MaxValue)
                    {
                        value = MaxValue;
                    }

                    _value = value;
                }
            }

            public int MaxValue { get; private set; }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    int width = 0;
                    int maxWidth = Value + Width;
                    bool drawOnly1 = true;

                    foreach (Control child in Children)
                    {
                        if (!child.IsVisible)
                        {
                            continue;
                        }

                        child.X = width - Value;

                        if (width + child.Width <= Value) { }
                        else if (width + child.Width <= maxWidth)
                        {
                            child.Draw(batcher, child.X + x, y);
                        }
                        else
                        {
                            if (drawOnly1)
                            {
                                child.Draw(batcher, child.X + x, y);
                                drawOnly1 = false;
                            }
                        }

                        width += child.Width;
                    }

                    batcher.ClipEnd();
                }

                return true; // base.Draw(batcher,position, hue);
            }

            public void CalculateWidth()
            {
                MaxValue = Children.Sum(s => s.Width) - Width;

                if (MaxValue < 0)
                {
                    MaxValue = 0;
                }
            }
        }
    }

    internal class GrayMenuGump : Gump
    {
        private readonly ResizePic _resizePic;

        public GrayMenuGump(World world, uint local, uint serv, string name) : base(world, local, serv)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            IsFromServer = true;

            Add(_resizePic = new ResizePic(0x13EC) { Width = 400, Height = 111111 });

            Label l;

            Add(l = new Label(name, false, 0x0386, 370, 1) { X = 20, Y = 16 });

            Width = _resizePic.Width;
            Height = l.Height;
        }

        public void SetHeight(int h)
        {
            _resizePic.Height = h;
            Width = _resizePic.Width;
            Height = _resizePic.Height;
        }

        public int AddItem(string name, int y)
        {
            RadioButton radio = new RadioButton(0, 0x138A, 0x138B, name, 1, 0x0386, false, 330)
            {
                X = 50,
                Y = y
            };

            Add(radio);

            return radio.Height;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0: // cancel
                    NetClient.Socket.Send_GrayMenuResponse(LocalSerial, (ushort)ServerSerial, 0);

                    Dispose();

                    break;

                case 1: // continue

                    ushort index = 1;

                    foreach (RadioButton radioButton in Children.OfType<RadioButton>())
                    {
                        if (radioButton.IsChecked)
                        {
                            NetClient.Socket.Send_GrayMenuResponse(
                                LocalSerial,
                                (ushort)ServerSerial,
                                index
                            );

                            Dispose();
                            break;
                        }

                        index++;
                    }

                    break;
            }
        }
    }
}
