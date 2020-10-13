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
using System.Data;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Server;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Guild;
using Mangos.Common.Globals;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Mangos.Cluster.Handlers
{
    public class WC_Handlers_Guild
    {
        public void On_CMSG_GUILD_QUERY(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 9)
                return;
            packet.GetInt16();
            uint guildId = packet.GetUInt32();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_QUERY [{2}]", client.IP, client.Port, guildId);
            ClusterServiceLocator._WC_Guild.SendGuildQuery(client, guildId);
        }

        public void On_CMSG_GUILD_ROSTER(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            // packet.GetInt16()

            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_ROSTER", client.IP, client.Port);
            ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
        }

        public void On_CMSG_GUILD_CREATE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string guildName = packet.GetString();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_CREATE [{2}]", client.IP, client.Port, guildName);
            if (client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_ALREADY_IN_GUILD);
                return;
            }

            // DONE: Create guild data
            var MySQLQuery = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query(string.Format("INSERT INTO guilds (guild_name, guild_leader, guild_cYear, guild_cMonth, guild_cDay) VALUES (\"{0}\", {1}, {2}, {3}, {4}); SELECT guild_id FROM guilds WHERE guild_name = \"{0}\";", guildName, client.Character.Guid, DateAndTime.Now.Year - 2006, DateAndTime.Now.Month, DateAndTime.Now.Day), ref MySQLQuery);
            ClusterServiceLocator._WC_Guild.AddCharacterToGuild(client.Character, Conversions.ToInteger(MySQLQuery.Rows[0]["guild_id"]), 0);
        }

        public void On_CMSG_GUILD_INFO(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            packet.GetInt16();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_INFO", client.IP, client.Port);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }

            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_INFO);
            response.AddString(client.Character.Guild.Name);
            response.AddInt32(client.Character.Guild.cDay);
            response.AddInt32(client.Character.Guild.cMonth);
            response.AddInt32(client.Character.Guild.cYear);
            response.AddInt32(0);
            response.AddInt32(0);
            client.Send(response);
            response.Dispose();
        }

        public void On_CMSG_GUILD_RANK(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 14)
                return;
            packet.GetInt16();
            int rankID = packet.GetInt32();
            uint rankRights = packet.GetUInt32();
            string rankName = packet.GetString().Replace("\"", "_").Replace("'", "_");
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_RANK [{2}:{3}:{4}]", client.IP, client.Port, rankID, rankRights, rankName);
            if (rankID < 0 || rankID > 9)
                return;
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            client.Character.Guild.Ranks[rankID] = rankName;
            client.Character.Guild.RankRights[rankID] = rankRights;
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_rank{1} = \"{2}\", guild_rank{1}_Rights = {3} WHERE guild_id = {0};", client.Character.Guild.ID, rankID, rankName, rankRights));
            ClusterServiceLocator._WC_Guild.SendGuildQuery(client, client.Character.Guild.ID);
            ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
        }

        public void On_CMSG_GUILD_ADD_RANK(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string NewRankName = packet.GetString().Replace("\"", "_").Replace("'", "_");
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_ADD_RANK [{2}]", client.IP, client.Port, NewRankName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }
            else if (ClusterServiceLocator._Functions.ValidateGuildName(NewRankName) == false)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL);
                return;
            }

            for (int i = 0; i <= 9; i++)
            {
                if (string.IsNullOrEmpty(client.Character.Guild.Ranks[i]))
                {
                    client.Character.Guild.Ranks[i] = NewRankName;
                    client.Character.Guild.RankRights[i] = (uint)(GuildRankRights.GR_RIGHT_GCHATLISTEN | GuildRankRights.GR_RIGHT_GCHATSPEAK);
                    ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_rank{1} = '{2}', guild_rank{1}_Rights = '{3}' WHERE guild_id = {0};", client.Character.Guild.ID, i, NewRankName, client.Character.Guild.RankRights[i]));
                    ClusterServiceLocator._WC_Guild.SendGuildQuery(client, client.Character.Guild.ID);
                    ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
                    return;
                }
            }

            ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL);
        }

        public void On_CMSG_GUILD_DEL_RANK(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            packet.GetInt16();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DEL_RANK", client.IP, client.Port);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // TODO: Check if someone in the guild is the rank we're removing?
            // TODO: Can we really remove all ranks?
            for (int i = 9; i >= 0; i -= 1)
            {
                if (!string.IsNullOrEmpty(client.Character.Guild.Ranks[i]))
                {
                    ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_rank{1} = '{2}', guild_rank{1}_Rights = '{3}' WHERE guild_id = {0};", client.Character.Guild.ID, i, "", 0));
                    ClusterServiceLocator._WC_Guild.SendGuildQuery(client, client.Character.Guild.ID);
                    ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
                    return;
                }
            }

            ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL);
        }

        public void On_CMSG_GUILD_LEADER(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = packet.GetString();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_LEADER [{2}]", client.IP, client.Port, playerName);
            if (playerName.Length < 2)
                return;
            playerName = ClusterServiceLocator._Functions.CapitalizeName(playerName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Find new leader's GUID
            var MySQLQuery = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT char_guid, char_guildId, char_guildrank FROM characters WHERE char_name = '" + playerName + "';", ref MySQLQuery);
            if (MySQLQuery.Rows.Count == 0)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }
            else if (Conversions.ToUInteger(MySQLQuery.Rows[0]["char_guildId"]) != client.Character.Guild.ID)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName);
                return;
            }

            ulong PlayerGUID = Conversions.ToULong(MySQLQuery.Rows[0]["char_guid"]);
            client.Character.GuildRank = 1; // Officer
            client.Character.SendGuildUpdate();
            if (ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(PlayerGUID))
            {
                ClusterServiceLocator._WorldCluster.CHARACTERs[PlayerGUID].GuildRank = 0;
                ClusterServiceLocator._WorldCluster.CHARACTERs[PlayerGUID].SendGuildUpdate();
            }

            client.Character.Guild.Leader = PlayerGUID;
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_leader = \"{1}\" WHERE guild_id = {0};", client.Character.Guild.ID, PlayerGUID));
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", 0, PlayerGUID));
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", client.Character.GuildRank, client.Character.Guid));

            // DONE: Send notify message
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.LEADER_CHANGED);
            response.AddInt8(2);
            response.AddString(client.Character.Name);
            response.AddString(playerName);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
        }

        public void On_MSG_SAVE_GUILD_EMBLEM(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length < 34)
                return;
            packet.GetInt16();
            int unk0 = packet.GetInt32();
            int unk1 = packet.GetInt32();
            int tEmblemStyle = packet.GetInt32();
            int tEmblemColor = packet.GetInt32();
            int tBorderStyle = packet.GetInt32();
            int tBorderColor = packet.GetInt32();
            int tBackgroundColor = packet.GetInt32();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_SAVE_GUILD_EMBLEM [{2},{3}] [{4}:{5}:{6}:{7}:{8}]", client.IP, client.Port, unk0, unk1, tEmblemStyle, tEmblemColor, tBorderStyle, tBorderColor, tBackgroundColor);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;

                // TODO: Check if you have enough money
                // ElseIf client.Character.Copper < 100000 Then
                // SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_NOT_ENOUGH_MONEY, 0, 0)
                // Exit Sub
            }

            client.Character.Guild.EmblemStyle = (byte)tEmblemStyle;
            client.Character.Guild.EmblemColor = (byte)tEmblemColor;
            client.Character.Guild.BorderStyle = (byte)tBorderStyle;
            client.Character.Guild.BorderColor = (byte)tBorderColor;
            client.Character.Guild.BackgroundColor = (byte)tBackgroundColor;
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_tEmblemStyle = {1}, guild_tEmblemColor = {2}, guild_tBorderStyle = {3}, guild_tBorderColor = {4}, guild_tBackgroundColor = {5} WHERE guild_id = {0};", client.Character.Guild.ID, tEmblemStyle, tEmblemColor, tBorderStyle, tBorderColor, tBackgroundColor));
            ClusterServiceLocator._WC_Guild.SendGuildQuery(client, client.Character.Guild.ID);
            var packet_event = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            packet_event.AddInt8((byte)GuildEvent.TABARDCHANGE);
            packet_event.AddInt32((int)client.Character.Guild.ID);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(packet_event, client.Character.Guild, notTo: argnotTo);
            packet_event.Dispose();

            // TODO: This tabard design costs 10g!
            // Client.Character.Copper -= 100000
            // Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, client.Character.Copper)
            // Client.Character.SendCharacterUpdate(False)
        }

        public void On_CMSG_GUILD_DISBAND(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            // packet.GetInt16()

            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DISBAND", client.IP, client.Port);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Clear all members
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.DISBANDED);
            response.AddInt8(0);
            int GuildID = (int)client.Character.Guild.ID;
            var tmpArray = client.Character.Guild.Members.ToArray();
            foreach (ulong Member in tmpArray)
            {
                if (ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Member))
                {
                    var tmp = ClusterServiceLocator._WorldCluster.CHARACTERs;
                    var argobjCharacter = tmp[Member];
                    ClusterServiceLocator._WC_Guild.RemoveCharacterFromGuild(argobjCharacter);
                    tmp[Member] = argobjCharacter;
                    ClusterServiceLocator._WorldCluster.CHARACTERs[Member].Client.SendMultiplyPackets(response);
                }
                else
                {
                    ClusterServiceLocator._WC_Guild.RemoveCharacterFromGuild(Member);
                }
            }

            ClusterServiceLocator._WC_Guild.GUILDs[(uint)GuildID].Dispose();
            response.Dispose();

            // DONE: Delete guild information
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update("DELETE FROM guilds WHERE guild_id = " + GuildID + ";");
        }

        public void On_CMSG_GUILD_MOTD(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            // Isn't the client even sending a null terminator for the motd if it's empty?
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string Motd = "";
            if (packet.Length != 4)
                Motd = packet.GetString().Replace("\"", "_").Replace("'", "_");
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_MOTD", client.IP, client.Port);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_SETMOTD))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            client.Character.Guild.Motd = Motd;
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE guilds SET guild_MOTD = '{1}' WHERE guild_id = '{0}';", client.Character.Guild.ID, Motd));
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.MOTD);
            response.AddInt8(1);
            response.AddString(Motd);

            // DONE: Send message to everyone in the guild
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
        }

        public void On_CMSG_GUILD_SET_OFFICER_NOTE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = packet.GetString();
            if (packet.Data.Length - 1 < 6 + playerName.Length + 1)
                return;
            string Note = packet.GetString();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_SET_OFFICER_NOTE [{2}]", client.IP, client.Port, playerName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_EOFFNOTE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildOffNote = \"{1}\" WHERE char_name = \"{0}\";", playerName, Note.Replace("\"", "_").Replace("'", "_")));
            ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
        }

        public void On_CMSG_GUILD_SET_PUBLIC_NOTE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = packet.GetString();
            if (packet.Data.Length - 1 < 6 + playerName.Length + 1)
                return;
            string Note = packet.GetString();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_SET_PUBLIC_NOTE [{2}]", client.IP, client.Port, playerName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_EPNOTE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildPNote = \"{1}\" WHERE char_name = \"{0}\";", playerName, Note.Replace("\"", "_").Replace("'", "_")));
            ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
        }

        public void On_CMSG_GUILD_REMOVE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = packet.GetString().Replace("'", "_").Replace("\"", "_");
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_REMOVE [{2}]", client.IP, client.Port, playerName);
            if (playerName.Length < 2)
                return;
            playerName = ClusterServiceLocator._Functions.CapitalizeName(playerName);

            // DONE: Player1 checks
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_REMOVE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Find player2's guid
            var q = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT char_guid FROM characters WHERE char_name = '" + playerName + "';", ref q);

            // DONE: Removed checks
            if (q.Rows.Count == 0)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }
            else if (!ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(q.Rows[0]["char_guid"])))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }

            var objCharacter = ClusterServiceLocator._WorldCluster.CHARACTERs[Conversions.ToULong(q.Rows[0]["char_guid"])];
            if (objCharacter.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_LEADER_LEAVE);
                return;
            }

            // DONE: Send guild event
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.REMOVED);
            response.AddInt8(2);
            response.AddString(playerName);
            response.AddString(objCharacter.Name);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
            ClusterServiceLocator._WC_Guild.RemoveCharacterFromGuild(objCharacter);
        }

        public void On_CMSG_GUILD_PROMOTE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = ClusterServiceLocator._Functions.CapitalizeName(packet.GetString());
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_PROMOTE [{2}]", client.IP, client.Port, playerName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_PROMOTE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Find promoted player's guid
            var q = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT char_guid FROM characters WHERE char_name = '" + playerName.Replace("'", "_") + "';", ref q);

            // DONE: Promoted checks
            if (q.Rows.Count == 0)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID);
                return;
            }
            else if (!ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(q.Rows[0]["char_guid"])))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }

            var objCharacter = ClusterServiceLocator._WorldCluster.CHARACTERs[Conversions.ToULong(q.Rows[0]["char_guid"])];
            if (objCharacter.Guild.ID != client.Character.Guild.ID)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName);
                return;
            }
            else if (objCharacter.GuildRank <= client.Character.GuildRank)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }
            else if (objCharacter.GuildRank == ClusterServiceLocator._Global_Constants.GUILD_RANK_MIN)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL);
                return;
            }

            // DONE: Do the real update
            objCharacter.GuildRank = (byte)(objCharacter.GuildRank - 1);
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", objCharacter.GuildRank, objCharacter.Guid));
            objCharacter.SendGuildUpdate();

            // DONE: Send event to guild
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.PROMOTION);
            response.AddInt8(3);
            response.AddString(objCharacter.Name);
            response.AddString(playerName);
            response.AddString(client.Character.Guild.Ranks[objCharacter.GuildRank]);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
        }

        public void On_CMSG_GUILD_DEMOTE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = ClusterServiceLocator._Functions.CapitalizeName(packet.GetString());
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DEMOTE [{2}]", client.IP, client.Port, playerName);
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_PROMOTE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Find demoted player's guid
            var q = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT char_guid FROM characters WHERE char_name = '" + playerName.Replace("'", "_") + "';", ref q);

            // DONE: Demoted checks
            if (q.Rows.Count == 0)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID);
                return;
            }
            else if (!ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(q.Rows[0]["char_guid"])))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }

            var objCharacter = ClusterServiceLocator._WorldCluster.CHARACTERs[Conversions.ToULong(q.Rows[0]["char_guid"])];
            if (objCharacter.Guild.ID != client.Character.Guild.ID)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName);
                return;
            }
            else if (objCharacter.GuildRank <= client.Character.GuildRank)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }
            else if (objCharacter.GuildRank == ClusterServiceLocator._Global_Constants.GUILD_RANK_MAX)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL);
                return;
            }

            // DONE: Max defined rank check
            if (string.IsNullOrEmpty(Strings.Trim(client.Character.Guild.Ranks[objCharacter.GuildRank + 1])))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL);
                return;
            }

            // DONE: Do the real update
            objCharacter.GuildRank = (byte)(objCharacter.GuildRank + 1);
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Update(string.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", objCharacter.GuildRank, objCharacter.Guid));
            objCharacter.SendGuildUpdate();

            // DONE: Send event to guild
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.DEMOTION);
            response.AddInt8(3);
            response.AddString(objCharacter.Name);
            response.AddString(playerName);
            response.AddString(client.Character.Guild.Ranks[objCharacter.GuildRank]);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
        }

        // User Options
        public void On_CMSG_GUILD_INVITE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 6)
                return;
            packet.GetInt16();
            string playerName = packet.GetString();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_INVITE [{2}]", client.IP, client.Port, playerName);

            // DONE: Inviter checks
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (!client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_INVITE))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS);
                return;
            }

            // DONE: Find invited player's guid
            var q = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT char_guid FROM characters WHERE char_name = '" + playerName.Replace("'", "_") + "';", ref q);

            // DONE: Invited checks
            if (q.Rows.Count == 0)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID);
                return;
            }
            else if (!ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(q.Rows[0]["char_guid"])))
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName);
                return;
            }

            var objCharacter = ClusterServiceLocator._WorldCluster.CHARACTERs[Conversions.ToULong(q.Rows[0]["char_guid"])];
            if (objCharacter.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.ALREADY_IN_GUILD, playerName);
                return;
            }
            else if (objCharacter.Side != client.Character.Side)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NOT_ALLIED, playerName);
                return;
            }
            else if (objCharacter.GuildInvited != 0L)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_INVITE_S, GuildError.ALREADY_INVITED_TO_GUILD, playerName);
                return;
            }

            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_INVITE);
            response.AddString(client.Character.Name);
            response.AddString(client.Character.Guild.Name);
            objCharacter.Client.Send(response);
            response.Dispose();
            objCharacter.GuildInvited = client.Character.Guild.ID;
            objCharacter.GuildInvitedBy = client.Character.Guid;
        }

        public void On_CMSG_GUILD_ACCEPT(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (client.Character.GuildInvited == 0L)
                throw new ApplicationException("Character accepting guild invitation whihtout being invited.");
            ClusterServiceLocator._WC_Guild.AddCharacterToGuild(client.Character, (int)client.Character.GuildInvited);
            client.Character.GuildInvited = 0U;
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.JOINED);
            response.AddInt8(1);
            response.AddString(client.Character.Name);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
            ClusterServiceLocator._WC_Guild.SendGuildRoster(client.Character);
            ClusterServiceLocator._WC_Guild.SendGuildMOTD(client.Character);
        }

        public void On_CMSG_GUILD_DECLINE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            client.Character.GuildInvited = 0U;
            if (ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey((ulong)Conversions.ToLong(client.Character.GuildInvitedBy)))
            {
                var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_DECLINE);
                response.AddString(client.Character.Name);
                ClusterServiceLocator._WorldCluster.CHARACTERs[(ulong)Conversions.ToLong(client.Character.GuildInvitedBy)].Client.Send(response);
                response.Dispose();
            }
        }

        public void On_CMSG_GUILD_LEAVE(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            // packet.GetInt16()

            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_LEAVE", client.IP, client.Port);

            // DONE: Checks
            if (!client.Character.IsInGuild)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD);
                return;
            }
            else if (client.Character.IsGuildLeader)
            {
                ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_LEADER_LEAVE);
                return;
            }

            ClusterServiceLocator._WC_Guild.RemoveCharacterFromGuild(client.Character);
            ClusterServiceLocator._WC_Guild.SendGuildResult(client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_PLAYER_NO_MORE_IN_GUILD, client.Character.Name);
            var response = new Packets.PacketClass(OPCODES.SMSG_GUILD_EVENT);
            response.AddInt8((byte)GuildEvent.LEFT);
            response.AddInt8(1);
            response.AddString(client.Character.Name);
            ulong argnotTo = 0UL;
            ClusterServiceLocator._WC_Guild.BroadcastToGuild(response, client.Character.Guild, notTo: argnotTo);
            response.Dispose();
        }

        public void On_CMSG_TURN_IN_PETITION(Packets.PacketClass packet, WC_Network.ClientClass client)
        {
            if (packet.Data.Length - 1 < 13)
                return;
            packet.GetInt16();
            ulong itemGuid = packet.GetUInt64();
            ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TURN_IN_PETITION [GUID={2:X}]", client.IP, client.Port, itemGuid);

            // DONE: Get info
            var q = new DataTable();
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query("SELECT * FROM petitions WHERE petition_itemGuid = " + (itemGuid - ClusterServiceLocator._Global_Constants.GUID_ITEM) + " LIMIT 1;", ref q);
            if (q.Rows.Count == 0)
                return;
            byte Type = Conversions.ToByte(q.Rows[0]["petition_type"]);
            string Name = Conversions.ToString(q.Rows[0]["petition_name"]);

            // DONE: Check if already in guild
            if (Type == 9 && client.Character.IsInGuild)
            {
                var response = new Packets.PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS);
                response.AddInt32((int)PetitionTurnInError.PETITIONTURNIN_ALREADY_IN_GUILD);
                client.Send(response);
                response.Dispose();
                return;
            }

            // DONE: Check required signs
            byte RequiredSigns = 9;
            if (Conversions.ToInteger(q.Rows[0]["petition_signedMembers"]) < RequiredSigns)
            {
                var response = new Packets.PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS);
                response.AddInt32((int)PetitionTurnInError.PETITIONTURNIN_NEED_MORE_SIGNATURES);
                client.Send(response);
                response.Dispose();
                return;
            }

            var q2 = new DataTable();

            // DONE: Create guild and add members
            ClusterServiceLocator._WorldCluster.CharacterDatabase.Query(string.Format("INSERT INTO guilds (guild_name, guild_leader, guild_cYear, guild_cMonth, guild_cDay) VALUES ('{0}', {1}, {2}, {3}, {4}); SELECT guild_id FROM guilds WHERE guild_name = '{0}';", Name, client.Character.Guid, DateAndTime.Now.Year - 2006, DateAndTime.Now.Month, DateAndTime.Now.Day), ref q2);
            ClusterServiceLocator._WC_Guild.AddCharacterToGuild(client.Character, Conversions.ToInteger(q2.Rows[0]["guild_id"]), 0);

            // DONE: Adding 9 more signed _WorldCluster.CHARACTERs
            for (byte i = 1; i <= 9; i++)
            {
                if (ClusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(q.Rows[0]["petition_signedMember" + i])))
                {
                    var tmp = ClusterServiceLocator._WorldCluster.CHARACTERs;
                    var argobjCharacter = tmp[Conversions.ToULong(q.Rows[0]["petition_signedMember" + i])];
                    ClusterServiceLocator._WC_Guild.AddCharacterToGuild(argobjCharacter, Conversions.ToInteger(q2.Rows[0]["guild_id"]));
                    tmp[Conversions.ToULong(q.Rows[0]["petition_signedMember" + i])] = argobjCharacter;
                }
                else
                {
                    ClusterServiceLocator._WC_Guild.AddCharacterToGuild(Conversions.ToULong(q.Rows[0]["petition_signedMember" + i]), Conversions.ToInteger(q2.Rows[0]["guild_id"]));
                }
            }

            // DONE: Delete guild charter item, on the world server
            client.Character.GetWorld.ClientPacket(client.Index, packet.Data);
            var success = new Packets.PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS);
            success.AddInt32(0); // Okay
            client.Send(success);
            success.Dispose();
        }
    }
}