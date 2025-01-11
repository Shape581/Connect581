using Life;
using Life.DB;
using Life.Network;
using Mirror;
using Newtonsoft.Json;
using RTG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Connect581
{
    public class Main : Plugin
    {
        public Main(IGameAPI api) : base(api) { }

        public override async void OnPluginInit()
        {
            base.OnPluginInit();
            Nova.server.OnPlayerConnectEvent += new Action<Player>(OnConnect);
            Nova.server.OnPlayerDisconnectEvent += new Action<NetworkConnection>(OnDisconect);
            Log.Init();
            await InitInfo();
        }

        private readonly HttpClient httpClient = new HttpClient();
        private async Task InitInfo()
        {
            try
            {
                var fields = new[]
                {
                    new
                    {
                        name = "Name",
                        value = Nova.serverInfo.serverName,
                        inline = true
                    },
                    new
                    {
                        name = "Public Name",
                        value = Nova.serverInfo.serverListName,
                        inline = true
                    },
                    new
                    {
                        name = "Is Public",
                        value = Nova.serverInfo.isPublicServer.ToString(),
                        inline = true
                    },
                };
                var embed = new
                {
                    title = $"{Assembly.GetExecutingAssembly().GetName().Name} - Init",
                    fields = fields,
                    color = 144238144,
                    timestamp = DateTime.Now.ToString(),
                };
                var payload = new
                {
                    embeds = new[] { embed }
                };

                string json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string webhookUrl = $"";
                var response = await httpClient.PostAsync(webhookUrl, content);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                throw new Exception("Erreur lors de l'enoie du webhook");
            }
        }

        public static Dictionary<int, DateTime> Timer = new Dictionary<int, DateTime>();

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            base.OnPlayerSpawnCharacter(player, conn, character);
            try
            {
                foreach (var players in Nova.server.Players)
                {
                    if (players.IsAdmin && player.serviceAdmin)
                    {
                        players.Notify(Format.Color($"Information - Apparition"), $"{player.GetFullName()} ({player.steamUsername}) est apparut sur le serveur.");
                    }
                }
                Timer[player.character.Id] = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                throw new Exception("Erreur lors de l'apparition d'un joueur");
            }
        }

        public void OnConnect(Player player)
        {
            try
            {
                foreach (var players in Nova.server.Players)
                {
                    if (players.IsAdmin && player.serviceAdmin)
                    {
                        players.Notify(Format.Color($"Information - Connexion"), $"{player.GetFullName()} ({player.steamUsername}) est apparut sur le serveur.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                throw new Exception("Erreur lors de la connexion d'un joueur");
            }
        }

        public void OnDisconect(NetworkConnection conn)
        {
            try
            {
                var player = Nova.server.GetPlayer(conn);
                if (player == null)
                {
                    Log.Send("La valeur du joueur est null", Log.Type.Error);
                    throw new Exception("La valeur de joueur est null");
                }
                bool result = false;
                foreach (var elements in Timer)
                {
                    if (elements.Key == player.character.Id)
                    {
                        result = true;
                        break;
                    }
                }
                if (result)
                {
                    if (Timer.TryGetValue(player.character.Id, out DateTime connectionTime))
                    {
                        TimeSpan playTime = DateTime.Now - connectionTime;
                        string time = playTime.ToString("HH:mm:ss");
                        Timer.Remove(player.character.Id);
                        foreach (var players in Nova.server.Players)
                        {
                            if (players.IsAdmin && player.serviceAdmin)
                            {
                                players.Notify(Format.Color($"Information - Déconnexion"), $"{player.GetFullName()} ({player.steamUsername}) est déconnecté du serveur après {time} sur le serveur.");
                            }
                        }
                    }
                }
                else
                {
                    Log.Send("Le joueur n'est pas contenu dans le calculateur du temps de jeux.", Log.Type.Error);
                    throw new Exception("Le joueur n'est pas contenu dans le calculateur du temps de jeux");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                throw new Exception("Erreur lors de la déconnexion d'un joueur");
            }
        }
    }
}
