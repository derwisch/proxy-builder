using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ProxyBuilder
{
    class CardFetcher
    {
        public CardFetcher()
        {
            InitCardData();
        }

        const string API_URL_CARDSEARCH = "https://api.scryfall.com/cards/named?exact={0}{1}";

        private async Task<JObject> GetCardJSON(string cardName, string set)
        {
            var request = WebRequest.CreateHttp(String.Format(API_URL_CARDSEARCH, cardName.Replace(' ', '+'), $"&set={set}"));
            var response = await request.GetResponseAsync();

            using (var stream = response.GetResponseStream())
            using (var tReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(tReader))
            {
                return JObject.Load(reader);
            }
        }

        private JArray defaultCardData = null;

        private async Task<string[]> FindSets(string name)
        {
            if (defaultCardData == null)
            {
                InitCardData();
            }

            var children = defaultCardData.Children<JObject>();
            //var childObjects = children.Where(x => x.GetValue("name").ToString().Equals(name));
            var childObjects = children.Where(evaluateSplitCard);
            var sets = childObjects.Select(x => x.GetValue("set").ToString()).ToArray();
            return sets;

            bool evaluateSplitCard(JObject card)
            {
                var cardName = card.GetValue("name").ToString();

                if (cardName.Equals(name))
                {
                    return true;
                }
                else if (cardName.StartsWith($"{name} // "))
                {
                    return true;
                }
                else if (cardName.EndsWith($" // {name}"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void InitCardData()
        {
            using (var stream = File.Open("scryfall-default-cards.json", FileMode.Open))
            using (var tReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(tReader))
            {
                defaultCardData = JArray.Load(reader);
            }
        }

        public async Task<string> GetImage(string name, string set)
        {
            JObject card = await GetCardJSON(name, set);

            if(!card.TryGetValue("image_uris", out JToken value))
            {
                return null;
            }

            var imgs = value as JObject;

            if (!imgs.TryGetValue("large", out JToken result))
            {
                return null;
            }

            return result.ToString();
        }

        public async Task<IEnumerable<string>> GetTransformImages(string name, string set)
        {
            List<string> result = new List<string>();
            JObject card = await GetCardJSON(name, set);

            if (!card.TryGetValue("card_faces", out JToken faces))
            {
                return null;
            }

            foreach (JObject child in faces.Children())
            {
                if (!child.TryGetValue("image_uris", out JToken value))
                {
                    continue;
                }

                var imgs = value as JObject;

                if (!imgs.TryGetValue("large", out JToken imageUri))
                {
                    continue;
                }

                result.Add(imageUri.ToString());
            }

            return result;
        }

        public async Task<ProxyItem> GetItem(string name)
        {
            dynamic card = await GetCardJSON(name, "");

            var sets = await FindSets(name);

            return new ProxyItem()
            {
                Name = card.name,
                SelectedSet = card.set,
                PossibleSets = sets
            };
        }

        public async Task<IEnumerable<ProxyItem>> ParseList(string deckList)
        {
            var lines = deckList.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
            List<ProxyItem> result = new List<ProxyItem>();

            foreach (var line in lines)
            {
                if (line.Contains(' '))
                {
                    var parts = line.Split(new[] { ' ' }, 2);

                    if (!Int32.TryParse(parts[0], out int count))
                    {
                        continue;
                    }
                    var name = parts[1].Trim();

                    var item = await GetItem(name);

                    item.Count = count;

                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}
