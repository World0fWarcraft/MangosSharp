﻿//
//  Copyright (C) 2013-2020 getMaNGOS <https:\\getmangos.eu>
//  
//  This program is free software. You can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation. either version 2 of the License, or
//  (at your option) any later version.
//  
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY. Without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System;
using Mangos.Common.Enums.Chat;
using Mangos.Common.Enums.Misc;

namespace Mangos.Scripts.Creatures
{
    public class CreatureAI_Targorr_the_Dread : Mangos.World.AI.WS_Creatures_AI.BossAI
    {
        private const int AI_UPDATE = 1000;
        private const int ThrashCD = 7000;
        private const int FrenzyCD = 90000; // This should never be reused.
        private const int Spell_Frenzy = 8599;
        private const int Spell_Thrash = 3391;
        public int NextThrash = 0;
        public int NextWaypoint = 0;
        public int NextAcid = 0;
        public int CurrentWaypoint = 0;

        public CreatureAI_Targorr_the_Dread(ref Mangos.World.Objects.WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            this.AllowedMove = false;
            Creature.Flying = false;
            Creature.VisibleDistance = 700f;
        }

        public override void OnThink()
        {
            NextThrash -= AI_UPDATE;
            if (NextThrash <= 0)
            {
                NextThrash = ThrashCD;
                this.aiCreature.CastSpellOnSelf(Spell_Thrash); // Should be cast on self. Correct me if wrong.
            }
        }

        public void CastThrash()
        {
            for (int i = 0; i <= 0; i++)
            {
                Mangos.World.Objects.WS_Base.BaseUnit Target = this.aiCreature;
                if (Target is null)
                    return;
                try
                {
                    this.aiCreature.CastSpellOnSelf(Spell_Thrash);
                }
                catch (Exception)
                {
                    this.aiCreature.SendChatMessage("AI was unable to cast Thrash on himself. Please report this to a developer.", ChatMsg.CHAT_MSG_YELL, LANGUAGES.LANG_GLOBAL);
                }
            }
        }

        public override void OnHealthChange(int Percent)
        {
            base.OnHealthChange(Percent);
            if (Percent <= 40)
            {
                try
                {
                    this.aiCreature.CastSpellOnSelf(Spell_Frenzy);
                }
                catch (Exception)
                {
                    this.aiCreature.SendChatMessage("AI was unable to cast Frenzy on himself. Please report this to a developer.", ChatMsg.CHAT_MSG_YELL, LANGUAGES.LANG_GLOBAL);
                }
            }
        }
    }
}