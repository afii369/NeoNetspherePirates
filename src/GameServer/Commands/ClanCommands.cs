﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Dapper.FastCrud;
using ExpressMapper.Extensions;
using NeoNetsphere.Database.Auth;
using NeoNetsphere.Network;
using NeoNetsphere.Network.Data.Club;
using NeoNetsphere.Network.Message.Club;

namespace NeoNetsphere.Commands
{
    internal class ClanCommands : ICommand
    {
        public ClanCommands()
        {
            Name = "/clan";
            AllowConsole = true;
            Permission = SecurityLevel.GameMaster;
            SubCommands = new ICommand[0];
        }

        public string Name { get; }
        public bool AllowConsole { get; }
        public SecurityLevel Permission { get; }
        public IReadOnlyList<ICommand> SubCommands { get; }

        public bool Execute(GameServer server, Player plr, string[] args)
        {
            if (args.Length < 1)
            {
                plr.SendConsoleMessage(S4Color.Red + "Wrong Usage, possible usages:");
                plr.SendConsoleMessage(S4Color.Red + "> /clan forcejoin <username> <clan>");
                plr.SendConsoleMessage(S4Color.Red + "> /clan forcekick <username> <clan>");
                return true;
            }

            if (args.Length < 3)
            {
                Array.Resize(ref args, 3);
                args[1] = "none";
                args[2] = "none";
            }

            var username = args[1].ToLower();
            var clan = args[2].ToLower().Replace("%20", " ");

            var club = GameServer.Instance.ClubManager.FirstOrDefault(x => x.Clan_Name.ToLower() == clan);
            var player =
                GameServer.Instance.PlayerManager.FirstOrDefault(x => x.Account.Nickname.ToLower() == username);

            if (club == null)
            {
                plr.SendConsoleMessage(S4Color.Red + "Unknown clan");
                return true;
            }

            if (player == null)
            {
                plr.SendConsoleMessage(S4Color.Red + "Unknown player");
                return true;
            }

            switch (args[0].ToLower())
            {
                case "forcejoin":
                {
                    if (GameServer.Instance.ClubManager.Any(x => x.Players.ContainsKey(player.Account.Id)))
                    {
                        plr.SendConsoleMessage(S4Color.Red + "Player is already in a clan");
                        return true;
                    }

                    club.Players.Add(player.Account.Id,
                        new ClubPlayerInfo()
                        {
                            AccountId = plr.Account.Id,
                            State = ClubState.Member,
                            IsMod = false
                        });
                    club.NeedsToSave = true;
                    player.Club = club;
                    player.Session.SendAsync(new ClubMyInfoAckMessage(player.Map<Player, MyInfoDto>()));
                    Club.LogOn(player);
                    plr?.SendConsoleMessage(S4Color.Green +
                                            $"Added player {player.Account.Nickname} to clan {club.Clan_Name}");
                    return true;
                }
                case "forcekick":
                {
                    if (!GameServer.Instance.ClubManager.Any(x => x.Players.ContainsKey(player.Account.Id)))
                    {
                        plr.SendConsoleMessage(S4Color.Red + "Player is not in a clan");
                        return true;
                    }

                    if (!club.Players.ContainsKey(player.Account.Id))
                    {
                        plr.SendConsoleMessage(S4Color.Red + "Player is in another clan");
                        return true;
                    }

                    Club.LogOff(player);
                    club.Players.Remove(player.Account.Id, out _);
                    club.NeedsToSave = true;
                    player.Club = null;
                    player.Session.SendAsync(new ClubMyInfoAckMessage(player.Map<Player, MyInfoDto>()));
                    plr?.SendConsoleMessage(S4Color.Green +
                                            $"Removed player {player.Account.Nickname} from clan {club.Clan_Name}");
                    return true;
                }
                default:
                {
                    plr.SendConsoleMessage(S4Color.Red + "Wrong Usage, possible usages:");
                    plr.SendConsoleMessage(S4Color.Red + "> /clan forcejoin <username> <clan>");
                    plr.SendConsoleMessage(S4Color.Red + "> /clan forcekick <username> <clan>");
                    return true;
                }
            }
        }

        public string Help()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Name);
            foreach (var cmd in SubCommands)
            {
                sb.Append(" ");
                sb.AppendLine(cmd.Help());
            }

            return sb.ToString();
        }
    }
}