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


namespace Mangos.Scripts.Creatures
{
    public class CreatureAI_Ragefire_Trogg : World.AI.WS_Creatures_AI.BossAI
    {
        private const int AI_UPDATE = 1000;
        private const int STRIKE_COOLDOWN = 4000;
        private const int STRIKE_SPELL = 11976;
        public int NextWaypoint = 0;
        public int NextStrike = 0;
        public int CurrentWaypoint = 0;

        public CreatureAI_Ragefire_Trogg(ref World.Objects.WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            AllowedMove = false;
            Creature.Flying = false;
            Creature.VisibleDistance = 700f;
        }

        public override void OnThink()
        {
            NextStrike -= AI_UPDATE;
            if (NextStrike <= 0)
            {
                NextStrike = STRIKE_COOLDOWN;
                aiCreature.CastSpell(STRIKE_SPELL, aiTarget); // Strike
            }
        }

        public void CastStrike()
        {
            for (int i = 0; i <= 3; i++)
            {
                World.Objects.WS_Base.BaseUnit Target = aiCreature;
                if (Target is null)
                    return;
                aiCreature.CastSpell(STRIKE_SPELL, aiTarget);
            }
        }
    }
}