using BepInEx;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Discord;
using Discord.Webhook;
using System;
using Valheim;

namespace ValheimDiscordLink
{
    [BepInPlugin("com.bluetigeresw.valheimdiscordlink", "Valheim Discord Link", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimDiscordLink : BaseUnityPlugin
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private DiscordWebhookClient _webhookClient;

        private void Awake()
        {
            tring discordWebhookUrl = Config.Bind<string>("General", "DiscordWebhookUrl", "").Value;
            if (string.IsNullOrEmpty(discordWebhookUrl))
            {
                Logger.LogError("DiscordWebhookUrl is not configured.");
                return;
            }

            _webhookClient = new DiscordWebhookClient(discordWebhookUrl);
            _webhookClient.MessageReceived += OnDiscordMessageReceived;
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony harmony = new Harmony("com.bluetigeresw.valheimdiscordlink");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Chat), "InputText")]
        static class Chat_InputText_Patch
        {
            static void Prefix(ref string text)
            {
                SendToDiscord(text);
            }
        }

        private static async void SendToDiscord(string message)
        {
            var payload = new Dictionary<string, string>
            {
                { "content", message }
            };
            var json = JsonConvert.SerializeObject(payload);
            var stringContent = new StringContent(json, Encoding.UTF8, "applications/json");

            await httpClient.PostAsync("https://discord.com/api/webhooks/1093329973324038234/3DVJ8AzfQsv8jV-pZX5S5brfmyOaneJkmJfCNatzu9AUHtbJY7Z8f2GQQnPjcdZ_BmS9", stringContent);
        }

        private async Task OnDiscordMessageReceived(Discord.Webhook.WebhookMessage message)
        {
            string username = message.Author.Username;
            string content = message.Content;
            DateTime timestamp = message.Timestamp;

            string formattedMessage = $"[Discord] {username}: {content} ({timestamp})";
            ZNet.instance.Broadcast(formattedMessage);
        }

        public void OnDestroy()
        {
            if (_webhookClient != null)
            {
                _webhookClient.MessageReceived -= OnDiscordMessageReceived;
                _webhookClient.Dispose();
                _webhookClient = null;
            }
        }

    }
}