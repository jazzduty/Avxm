using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Avxm
{
    public class CgsosoLoader : BaseLoader
    {
        private const string sitePrefix = "http://www.cgsoso.com/";
        private const string siteUrl0 = "forum-211-1.html";
        private const string siteUrl = "forum.php?mod=forumdisplay&fid=211&sortid=13&page=";
        private const string cookieFile = "export.json";
        private const string dataFile = "cgsoso.json";
        private const string htmlFile = "cgsoso.html";
        private const string assetPad = " unity3d asset";

        public async Task SosoProc(string endDate)
        {
            List<ImageItem> oldItems = null;
            try {
                oldItems = JsonConvert.DeserializeObject<List<ImageItem>>(File.ReadAllText(dataFile));
                Console.WriteLine($"Load old data file: {oldItems.Count} items");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            if (string.IsNullOrEmpty(endDate) && oldItems.Count > 0) {
                endDate = oldItems[0].PubDate;
            }

            Console.WriteLine($"End data: {endDate}");
            var imageItems = await LoadPage(endDate);
            Console.WriteLine($"Pages loaded: {imageItems.Count} items");

            var cnt = 0; var cntall = imageItems.Count;
            foreach (var item in imageItems) {
                if (item.ImageUrl == null) {
                    await LoadItem(item);
                }
                Console.Write($"{++cnt}|{cntall} ");
            }

            if (oldItems != null) {
                var item0 = oldItems[0];
                for (var i = imageItems.Count - 1; i >= 0; --i) {
                    if (imageItems[i].PubDate.CompareTo(item0.PubDate) <= 0) {
                        imageItems.RemoveAt(i);
                    }
                }
                if (imageItems.Count > 0) {
                    imageItems.AddRange(oldItems);
                }
            }
            if (imageItems.Count > 0) {
                var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(imageItems, Formatting.Indented, jset);
                File.WriteAllText(dataFile, json);
            }
        }

        private async Task<List<ImageItem>> LoadPage(string endDate)
        {
            if (httpClient == null) {
                InitHttp(); LoadCookie(cookieFile, sitePrefix);
            }

            var imageItems = new List<ImageItem>();
            var timeOver = false;
            DateTime dateEnd = DateTime.Now.AddHours(-28);
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime date)) {
                dateEnd = date;
            }

            HttpResponseMessage resp = null;
            int pageIdx = 1;
            var enc = Encoding.GetEncoding("GB2312");

            try {
                while (!timeOver) {
                    Console.WriteLine();
                    Console.Write($"Read Page {pageIdx} ");
                    try {
                        resp = await httpClient.GetAsync(pageIdx > 0 ? sitePrefix + siteUrl + pageIdx : sitePrefix + siteUrl0); ++pageIdx;
                    } catch (Exception) {
                        await Task.Delay(100); Console.Write("*");
                        resp = await httpClient.GetAsync(pageIdx == 0 ? sitePrefix + siteUrl + pageIdx : sitePrefix + siteUrl0); ++pageIdx;
                    }
                    if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
                        var data = await resp.Content.ReadAsByteArrayAsync();
                        var html = enc.GetString(data);
                        timeOver = ParsePage(html, dateEnd, imageItems);
                        if (timeOver && imageItems.Count == 0) {
                            File.WriteAllText(htmlFile, html);
                        }
                    } else {
                        timeOver = true;
                    }
                    if (resp != null) {
                        resp.Dispose(); resp = null;
                    }
                }
                Console.WriteLine();
            } catch (Exception ex) {
                while (ex.InnerException != null) { ex = ex.InnerException; }
                Console.WriteLine();
                Console.WriteLine(ex);
            }

            if (resp != null) {
                resp.Dispose(); resp = null;
            }

            return imageItems;
        }
        private bool ParsePage(string html, DateTime dateEnd, List<ImageItem> items)
        {
            dateEnd = dateEnd.Date;
            bool timeOver = false;
            ImageItem item = null;
            int cnt = 0;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var tables = doc.DocumentNode.SelectNodes("//table[@id='threadlisttableid']/tbody");
            if (tables == null) {
                Console.WriteLine("**Parse page not found items");
                timeOver = true;
            } else {
                foreach (var m in tables) {
                    var attr = m.GetAttributeValue("id", "");
                    if (attr.StartsWith("normalthread")) {
                        var nodeLink = m.SelectSingleNode("tr/th[@class='common']/a[@class='s xstt']");
                        var nodeDay = m.SelectSingleNode("tr/td[@class='by']/em/span");
                        if (nodeLink != null && nodeDay != null) {
                            item = new ImageItem();
                            item.FileName = nodeLink.InnerText.Trim();
                            if (item.FileName.EndsWith(assetPad)) {
                                item.FileName = item.FileName.Substring(0, item.FileName.Length - assetPad.Length);
                            }
                            var nodeDay1 = nodeDay.SelectSingleNode("span");
                            if (nodeDay1 != null) {
                                attr = nodeDay1.GetAttributeValue("title", "");
                            } else {
                                attr = nodeDay.InnerText;
                            }
                            item.PubDate = attr;
                            var day = DateTime.MinValue;
                            DateTime.TryParseExact(attr, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out day);
                            if (day >= dateEnd && (attr = nodeLink.GetAttributeValue("href", "")) != "") {
                                item.Link = attr.Replace("&amp;", "&");
                                items.Add(item); ++cnt;
                            } else {
                                timeOver = true;
                            }
                        } else {
                            timeOver = true;
                        }
                    }
                }
            }
            if (item != null) {
                Console.Write($"[{cnt}][{item.PubDate}]");
            }
            return timeOver;
        }

        private async Task<bool> LoadItem(ImageItem item)
        {
            bool rlt = false;
            if (httpClient == null) {
                InitHttp(); LoadCookie(cookieFile, sitePrefix);
            }

            HttpResponseMessage resp = null;
            var enc = Encoding.GetEncoding("GB2312");

            try {
                resp = await httpClient.GetAsync(item.Link);
            } catch (Exception) {
                await Task.Delay(100); Console.Write("*");
                resp = await httpClient.GetAsync(item.Link);
            }
            if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
                var data = await resp.Content.ReadAsByteArrayAsync();
                var html = enc.GetString(data);
                var pm = ParseItem(html);
                if (pm != null) {
                    rlt = true;
                    item.Extra = pm.Extra;
                    item.FileName = pm.FileName;
                    item.ImageUrl = pm.ImageUrl;
                    item.MainIdx = pm.MainIdx;
                    item.PubDate = pm.PubDate;
                }
            }
            if (resp != null) {
                resp.Dispose(); resp = null;
            }
            return rlt;
        }

        private ImageItem ParseItem(string html)
        {
            ImageItem item = null;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodeTitle = doc.DocumentNode.SelectSingleNode("//span[@id='thread_subject']");
            var nodeLinks = doc.DocumentNode.SelectNodes("//div[@id='dobtn']/ul/li/span/a");
            var nodeTag = doc.DocumentNode.SelectSingleNode("//td[@class='plc']/div[@class='pct']/div[@class='pcb']/div[@class='t_fsz']/div/a");
            var nodeTime = doc.DocumentNode.SelectSingleNode("//td[@class='plc']/div[@class='pi']/div[@class='pti']/div[@class='authi']/em");
            if (nodeTitle != null && nodeTime != null && nodeLinks != null && nodeLinks.Count > 0) {
                var nval = 0;
                item = new ImageItem {
                    FileName = nodeTitle.InnerText.Replace("&amp;", "&")
                };
                if (nodeTag != null) {
                    item.Extra = nodeTag.GetAttributeValue("title", null);
                    if (item.Extra != null && item.Extra.StartsWith("1") && int.TryParse(item.Extra.Substring(1, item.Extra.Length - 1), out nval)) {
                        item.MainIdx = nval;
                        item.FileName = $"{item.MainIdx}_{item.FileName}";
                    }
                    item.Extra = null;
                }
                if (nodeLinks.Count > 0) {
                    item.ImageUrl = nodeLinks[0].GetAttributeValue("href", null)?.Replace("&amp;", "&");
                }
                if (nodeLinks.Count > 1) {
                    item.Extra = nodeLinks[1].GetAttributeValue("href", null)?.Replace("&amp;", "&");
                }
                var nodeTime1 = nodeTime.SelectSingleNode("span");
                if (nodeTime1 != null) {
                    item.PubDate = nodeTime1.GetAttributeValue("title", "");
                } else {
                    item.PubDate = nodeTime.InnerText;
                    const string pubpad = "发表于 ";
                    if (item.PubDate.StartsWith(pubpad)) {
                        item.PubDate = item.PubDate.Substring(pubpad.Length);
                    }
                }
                var day = DateTime.MinValue;
                if (DateTime.TryParseExact(item.PubDate, "yyyy-M-d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out day)) {
                    item.PubDate = day.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (item.FileName.EndsWith(assetPad)) {
                    item.FileName = item.FileName.Substring(0, item.FileName.Length - assetPad.Length);
                }
                if (!item.ImageUrl.StartsWith("http")) {
                    if (item.ImageUrl.StartsWith("/")) {
                        item.ImageUrl = sitePrefix + item.ImageUrl.Substring(1);
                    } else {
                        item.ImageUrl = sitePrefix + item.ImageUrl;
                    }
                }
            }
            return item;
        }
    }
}
