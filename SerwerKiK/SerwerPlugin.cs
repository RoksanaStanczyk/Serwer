using System;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using Scripts.Models;
using Scripts.Networking;
namespace SerwerKiK
{
    public class SerwerPlugin : Plugin
    {
        public override bool ThreadSafe => false;
        public override Version Version => new Version(0, 0, 1);

        private PlayerModel pendingPlayer;
        private Dictionary<ushort, MatchModel> matches;

        public SerwerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {

            ClientManager.ClientConnected += OnClientConnected;
            ClientManager.ClientDisconnected += OnClientDisconnected;

            matches = new Dictionary<ushort, MatchModel>();
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            WriteEvent("hej, " + e.Client.ID, DarkRift.LogType.Info);
            e.Client.MessageReceived += OnClientMessageReceived;
        }

        private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            WriteEvent("papa, " + e.Client.ID, DarkRift.LogType.Info);

            if (pendingPlayer != null && pendingPlayer.Client == e.Client)
            {
                pendingPlayer = null;
            }

        }

        private void OnClientMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            switch (e.Tag)
            {
                case (ushort)Tags.Tag.SET_NAME:

                    

                    using (Message message = e.GetMessage()) //czytanie message
                    {
                        using (DarkRiftReader reader = message.GetReader())
                        {
                            string name = reader.ReadString();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("witaj " + name);
                            Console.ForegroundColor = ConsoleColor.White;

                            PlayerModel newPlayer = new PlayerModel(e.Client, name);

                            if (pendingPlayer == null)
                            {
                                // nowy gracz
                                pendingPlayer = newPlayer;
                            }
                            else
                            {
                                //nowy meczyk

                                MatchModel match = new MatchModel(pendingPlayer, newPlayer);
                                matches.Add(match.id, match);

                                
                                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(match.id);
                                    writer.Write(match.CurrentPlayerClientID);
                                    using (Message msg = Message.Create((ushort)Tags.Tag.GOT_MATCH, writer))
                                    {
                                        pendingPlayer.Client.SendMessage(msg, SendMode.Reliable);
                                        newPlayer.Client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }

                                pendingPlayer = null;
                            }
                        }
                    }
                    break;

                case (ushort)Tags.Tag.SLATE_TAKEN:

                    using (Message message = e.GetMessage())
                    {
                        using (DarkRiftReader reader = message.GetReader())
                        {

                            ushort matchId = reader.ReadUInt16();
                            ushort slateIndex = reader.ReadUInt16();

                            if (matches.ContainsKey(matchId))
                            {
                                MatchModel match = matches[matchId];
                                match.PlayerTakesSlate(slateIndex, e.Client);
                                if (match.MatchOver)
                                {
                                    
                                    Console.WriteLine("Koniec rozgrywki");
                                    matches.Remove(matchId);
                                    
                                }
                            }
                        }
                    }

                    break;
            }
        }
    }
}
