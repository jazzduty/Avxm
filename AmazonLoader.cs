using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;

namespace Avxm
{
	public class AmazonLoader : BaseLoader
	{
		private string siteUrl = "http://amazon.co.jp";

		public async Task<List<ImageItem>> LoadPages(string shopId)
		{
			if (httpClient == null) {
				InitHttp(); //LoadCookie(cookieFile, sitePrefix);
			}

			var items = new List<ImageItem>();
			HttpResponseMessage resp = null;
			int pageIdx = 1;
            string nextPage = "/shops/" + shopId;

			try {
                while (nextPage != null)
                {
                    Console.WriteLine("Read Page " + pageIdx);
                    Console.WriteLine(siteUrl + nextPage);
                    try
                    {
                        resp = await httpClient.GetAsync(siteUrl + nextPage);
                    }
                    catch (Exception)
                    {
                        await Task.Delay(100); Console.Write("*");
                        resp = await httpClient.GetAsync(siteUrl + nextPage);
                    }

                    if (resp != null && resp.StatusCode == HttpStatusCode.OK)
                    {
                        //var data = await resp.Content.ReadAsByteArrayAsync();
                        //var html = enc.GetString(data);
                        //timeOver = ParsePageUrls(html, dateEnd, itemUrls);
                        var html = await resp.Content.ReadAsStringAsync();
                        File.WriteAllText(@"Z:\Incomming\temp\amazon" + pageIdx + ".html", html);
                        nextPage = ParseItems(html, items);
                    }
                    else
                    {
                        nextPage = null;
                    }
                    if (resp != null)
                    {
                        resp.Dispose(); resp = null;
                    }
                    ++pageIdx;
                }

				//timeOver = ParsePageUrls(File.ReadAllText("aaa.html"), dateEnd, itemUrls);
				Console.WriteLine("Total Page Items " + items.Count);

				/*pageIdx = 1;
				foreach (var itemUrl in itemUrls) {
					Console.WriteLine(itemUrl);
					Console.Write($"{pageIdx++}. ");
					try {
						resp = await httpClient.GetAsync(itemUrl);
					} catch (Exception) {
						await Task.Delay(100); Console.Write("*");
						resp = await httpClient.GetAsync(itemUrl);
					}
					if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
						var data = await resp.Content.ReadAsByteArrayAsync();
						var html = enc.GetString(data);
						var item = ParseItemUrl(html);
						if (item != null) {
							item.Link = itemUrl;
							imageItems.Add(item);
							Console.Write($"{item.PubDate} {item.MainIdx}_{item.FileName}");
						}
					}
					if (resp != null) {
						resp.Dispose(); resp = null;
					}
					Console.WriteLine();
				}*/
			} catch (Exception ex) {
				while (ex.InnerException != null) { ex = ex.InnerException; }
				Console.WriteLine();
				Console.WriteLine(ex);
			}

			if (resp != null) {
				resp.Dispose(); resp = null;
			}

			return items;
		}

		private string ParseItems(string html, List<ImageItem> items)
		{
            string rlt = null;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var nodes = doc.DocumentNode.SelectNodes("//div[@id='btfResults']/ul/li"); //atfResults
            if (nodes != null && nodes.Count > 0)
            {
                Console.WriteLine(nodes.Count);
			}
            else
            {
                Console.WriteLine("NG");
            }

            /*
            var nodeNextPage = doc.DocumentNode.SelectSingleNode("//div[@id='sx-hms-response']/input[@name='url']");
            if (nodeNextPage != null)
            {
                rlt = nodeNextPage.GetAttributeValue("value", null);
                if (rlt != null)
                {
                    rlt = rlt.Replace("&amp;", "&");
                }
            }
            */

            /*int nLastPage = 0;
            var nodeLastPage = doc.DocumentNode.SelectSingleNode("//div[@id='pagn']/span[@class='pagnDisabled']");
            if (nodeLastPage != null && int.TryParse(nodeLastPage.InnerText, out nLastPage) && nCurPage != nLastPage)
            {
                var nodeNextPage = doc.DocumentNode.SelectSingleNode("//a[@id='pagnNextLink']");
                if (nodeNextPage != null)
                {
                    rlt = nodeNextPage.GetAttributeValue("href", null);
                    ++nCurPage;
                    if (nCurPage != 2 && rlt.Contains("page=2"))
                    {
                        rlt = rlt.Replace("page=2", "page=" + nCurPage);
                    }
                }
            }*/

            var nodeNextPage = doc.DocumentNode.SelectSingleNode("//a[@id='pagnNextLink']");
            if (nodeNextPage != null)
            {
                rlt = nodeNextPage.GetAttributeValue("href", null);
                if (rlt != null)
                {
                    rlt = rlt.Replace("&amp;", "&");
                }
            }

            return rlt;
		}
	}
}
