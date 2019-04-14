using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProxyBuilder
{
    class ProxyPrinter
    {
        private WebClient client = new WebClient();

        private string DownloadImage(string uri)
        {
            var fileName = System.IO.Path.GetTempFileName();
            client.DownloadFile(new Uri(uri), fileName);
            return fileName;
        }

        public async void Print(IEnumerable<ProxyItem> items)
        {
            List<string> images = new List<string>();
            foreach (var item in items)
            {
                var uri = await App.CardFetcher.GetImage(item.Name, item.SelectedSet);

                if (uri == null)
                {
                    var uris = await App.CardFetcher.GetTransformImages(item.Name, item.SelectedSet);

                    foreach (var multiFaceUri in uris)
                    {
                        for (int i = 0; i < item.Count; ++i)
                        {
                            images.Add(DownloadImage(multiFaceUri));
                        }
                    }
                }
                else
                {
                    for (int i  = 0; i < item.Count; ++i)
                    {
                        images.Add(DownloadImage(uri));
                    }
                }
            }

            var imageQueue = new Queue<string>(images);

            PrintDocument document = new PrintDocument();

            document.PrintPage += (sender, e) =>
            {
                e.Graphics.PageUnit = GraphicsUnit.Millimeter;

                for (int i = 0; i < 9; ++i)
                {
                    if (imageQueue.Count == 0)
                    {
                        break;
                    }

                    // 63 x 88 mm in size (2.5 by 3.5 inches)

                    using (var image = Image.FromFile(imageQueue.Dequeue()))
                    {
                        e.Graphics.DrawImage(
                            image,
                            5f + 70f * (i % 3),
                            5f + 90f * (i / 3),
                            66,
                            88);
                    }
                }

                if (imageQueue.Count > 0)
                {
                    e.HasMorePages = true;
                }
            };

            document.Print();
        }
    }
}
