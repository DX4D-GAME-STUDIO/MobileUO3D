// SPDX-License-Identifier: BSD-2-Clause

using System.Linq;
using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Resources;
using System.Collections.Generic;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharTradeGump : Gump
    {
        private readonly HSliderBar[] _attributeSliders;
        private readonly PlayerMobile _character;
        private readonly Combobox[] _skillsCombobox;
        private readonly HSliderBar[] _skillSliders;
        private readonly List<SkillEntry> _skillList;



        public CreateCharTradeGump(World world, PlayerMobile character, ProfessionInfo profession) : base(world, 0, 0)
        {
            _character = character;

            foreach (Skill skill in _character.Skills)
            {
                skill.ValueFixed = 0;
                skill.BaseFixed = 0;
                skill.CapFixed = 0;
                skill.Lock = Lock.Locked;
            }

            Add
            (
                new ResizePic(2600)
                {
                    X = 100, Y = 80, Width = 470, Height = 372
                }
            );

            // center menu with fancy top
            // public GumpPic(AControl parent, int x, int y, int gumpID, int hue)
            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);

            // title text
            //TextLabelAscii(AControl parent, int x, int y, int font, int hue, string text, int width = 400)
            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000326), unicode, hue, font: font)
                {
                    X = 148, Y = 132
                }
            );

            // strength, dexterity, intelligence
            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000111), unicode, 1, font: 1)
                {
                    X = 158, Y = 170
                }
            );

            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000112), unicode, 1, font: 1)
                {
                    X = 158, Y = 250
                }
            );

            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000113), unicode, 1, font: 1)
                {
                    X = 158, Y = 330
                }
            );

            // sliders for attributes
            _attributeSliders = new HSliderBar[3];

            (var defSkillsValues, var defStatsValues) = ProfessionInfo.GetDefaults(Client.Game.UO.Version);

            Add
            (
                _attributeSliders[0] = new HSliderBar
                (
                    164,
                    196,
                    93,
                    10,
                    60,
                    defStatsValues[0],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            Add
            (
                _attributeSliders[1] = new HSliderBar
                (
                    164,
                    276,
                    93,
                    10,
                    60,
                    defStatsValues[1],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            Add
            (
                _attributeSliders[2] = new HSliderBar
                (
                    164,
                    356,
                    93,
                    10,
                    60,
                    defStatsValues[2],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            var clientFlags = World.ClientLockedFeatures.Flags;

            _skillList = Client.Game.UO.FileManager.Skills.SortedSkills
                         .Where(s =>
                                     // All standard client versions ignore these skills by defualt
                                     //s.Index != 26 && // MagicResist
                                     s.Index != 47 && // Stealth
                                     s.Index != 48 && // RemoveTrap
                                     s.Index != 54 && // Spellweaving
                                     (character.Race == RaceType.GARGOYLE || s.Index != 57) // Throwing for gargoyle only
                                 )
                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.AOS) ||
                                    (
                                        s.Index != 51 && // Chivlary
                                        s.Index != 50 && // Focus
                                        s.Index != 49    // Necromancy
                                    )
                                )

                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.SE) ||
                                    (
                                        s.Index != 52 && // Bushido
                                        s.Index != 53    // Ninjitsu
                                    )
                                )

                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.SA) ||
                                    (
                                        s.Index != 55 && // Mysticism
                                        s.Index != 56    // Imbuing
                                    )
                                )
                         .ToList();

            // do not include archer if it's a gargoyle
            if (character.Race == RaceType.GARGOYLE)
            {
                var archeryEntry = _skillList.FirstOrDefault(s => s.Index == 31);
                if (archeryEntry != null)
                {
                    _skillList.Remove(archeryEntry);
                }
            }

            var skillNames = _skillList.Select(s => s.Name).ToArray();

            int y = 172;
            _skillSliders = new HSliderBar[CharCreationGump._skillsCount];
            _skillsCombobox = new Combobox[CharCreationGump._skillsCount];

            for (int i = 0; i < CharCreationGump._skillsCount; i++)
            {
                Add
                (
                    _skillsCombobox[i] = new Combobox
                    (
                        344,
                        y,
                        182,
                        skillNames,
                        -1,
                        200,
                        false,
                        "Click here"
                    )
                );

                Add
                (
                    _skillSliders[i] = new HSliderBar
                    (
                        344,
                        y + 32,
                        93,
                        0,
                        50,
                        defSkillsValues[i, 1],
                        HSliderBarStyle.MetalWidgetRecessedBar,
                        true
                    )
                );

                y += 70;
            }

            Add
            (
                new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

            for (int i = 0; i < _attributeSliders.Length; i++)
            {
                for (int j = 0; j < _attributeSliders.Length; j++)
                {
                    if (i != j)
                    {
                        _attributeSliders[i].AddParisSlider(_attributeSliders[j]);
                    }
                }
            }

            for (int i = 0; i < _skillSliders.Length; i++)
            {
                for (int j = 0; j < _skillSliders.Length; j++)
                {
                    if (i != j)
                    {
                        _skillSliders[i].AddParisSlider(_skillSliders[j]);
                    }
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();

            switch ((Buttons) buttonID)
            {
                case Buttons.Prev:
                    charCreationGump.StepBack();

                    break;

                case Buttons.Next:

                    if (ValidateValues())
                    {
                        for (int i = 0; i < _skillsCombobox.Length; i++)
                        {
                            if (_skillsCombobox[i].SelectedIndex != -1)
                            {
                                Skill skill = _character.Skills[_skillList[_skillsCombobox[i].SelectedIndex].Index];
                                skill.ValueFixed = (ushort) _skillSliders[i].Value;
                                skill.BaseFixed = 0;
                                skill.CapFixed = 0;
                                skill.Lock = Lock.Locked;
                            }
                        }

                        _character.Strength = (ushort) _attributeSliders[0].Value;
                        _character.Intelligence = (ushort) _attributeSliders[1].Value;
                        _character.Dexterity = (ushort) _attributeSliders[2].Value;

                        charCreationGump.SetAttributes(true);
                    }

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private bool ValidateValues()
        {
            if (_skillsCombobox.All(s => s.SelectedIndex >= 0))
            {
                int duplicated = _skillsCombobox.GroupBy(o => o.SelectedIndex).Count(o => o.Count() > 1);

                if (duplicated > 0)
                {
                    UIManager.GetGump<CharCreationGump>()?.ShowMessage(Client.Game.UO.FileManager.Clilocs.GetString(1080032));

                    return false;
                }
            }
            else
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(Client.Game.UO.Version <= ClientVersion.CV_5090 ? ResGumps.YouMustHaveThreeUniqueSkillsChosen : Client.Game.UO.FileManager.Clilocs.GetString(1080032));

                return false;
            }

            return true;
        }

        private enum Buttons
        {
            Prev,
            Next
        }
    }
}