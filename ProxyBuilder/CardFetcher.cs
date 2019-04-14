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
        private JArray defaultCardData = null;
        private Dictionary<string, string> setNames = new Dictionary<string, string>();

        private const string API_URL_SETLIST = "https://api.scryfall.com/sets";

        public CardFetcher()
        {
            InitCardData();
            InitSetData();
        }

        private async void InitSetData()
        {
            var request = WebRequest.CreateHttp(API_URL_SETLIST);
            var response = await request.GetResponseAsync();

            using (var stream = response.GetResponseStream())
            using (var tReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(tReader))
            {
                var setList = JObject.Load(reader);

                if (!setList.TryGetValue("data", out JToken value))
                {
                    return;
                }
                if (!(value is JArray dataArray))
                {
                    return;
                }
                foreach (JObject setItem in dataArray)
                {
                    if (!setItem.TryGetValue("code", out JToken setCode))
                    {
                        continue;
                    }
                    if (!setItem.TryGetValue("name", out JToken setName))
                    {
                        continue;
                    }
                    setNames.Add(setCode.ToString().ToLower(), setName.ToString());
                }
            }
        }

        public string GetSetName(string setCode)
        {
            if (String.IsNullOrWhiteSpace(setCode))
            {
                return "";
            }

            return setNames[setCode];
        }

        public string GetSetKey(string setName)
        {
            if (String.IsNullOrWhiteSpace(setName))
            {
                return "";
            }

            return setNames.First(x => x.Value == setName).Value;
        }
        
        private JObject FindCardJSON(string name, string set)
        {
            var children = defaultCardData.Children<JObject>();
            var result = children.FirstOrDefault(evaluateSplitCard);
            return result;

            bool evaluateSplitCard(JObject card)
            {
                var cardName = card.GetValue("name").ToString();
                var cardSet = card.GetValue("set").ToString();

                if (!String.IsNullOrWhiteSpace(set) && !set.Equals(cardSet))
                {
                    return false;
                }
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

        private string[] FindSets(string name)
        {
            var children = defaultCardData.Children<JObject>();
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

        public string GetImage(string name, string set)
        {
            JObject card = FindCardJSON(name, set);

            if (!card.TryGetValue("image_uris", out JToken value))
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

        public IEnumerable<string> GetTransformImages(string name, string set)
        {
            List<string> result = new List<string>();
            JObject card = FindCardJSON(name, set);

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

        public ProxyItem GetItem(string name)
        {
            dynamic card = FindCardJSON(name, "");

            var sets = FindSets(name);

            return new ProxyItem()
            {
                Name = card.name,
                SelectedSet = card.set,
                PossibleSets = sets
            };
        }

        public IEnumerable<ProxyItem> ParseList(string deckList)
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

                    var item = GetItem(name);

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
