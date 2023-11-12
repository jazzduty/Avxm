using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Avxm
{
	public class DayLoader
	{
		private string siteUrl = "http://www.0daydown.com/page/";
		//private HttpClientHandler httpHandler;
		private HttpClient httpClient;

		public async Task<List<ImageItem>> LoadImageUrls(int hours)
		{
			if (httpClient == null) {
				InitHttp();
			}

			var imageItems = new List<ImageItem>();
			var timeOver = false;
			var dateEnd = $"{(hours / 24) + 1}天前";

			HttpResponseMessage resp = null;
			int pageIdx = 1;

			try {
				while (!timeOver) {
					Console.WriteLine("Read Page " + pageIdx);
					resp = await httpClient.GetAsync(siteUrl + (pageIdx++));
					if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
						var html = await resp.Content.ReadAsStringAsync();
						timeOver = ParseImageUrls(html, dateEnd, imageItems);
					} else {
						timeOver = true;
					}
					if (resp != null) {
						resp.Dispose(); resp = null;
					}
				}
				Console.WriteLine("Total Page Items " + imageItems.Count);
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
		private bool ParseImageUrls(string html, string dateEnd, List<ImageItem> images)
		{
			bool timeOver = false;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var nodes = doc.DocumentNode.SelectNodes("//article");
			if (nodes != null && nodes.Count > 0) {
				foreach (HtmlNode node in nodes) {
					HtmlNode nodeDate = node.SelectNodes("//span[@class='muted']")[1];
					if (nodeDate != null) {
						string date = nodeDate.InnerText.Trim();
						if (date != dateEnd) {
							HtmlNode nodeTitle = node.SelectSingleNode("header");
							HtmlNode nodeImg = node.SelectSingleNode("div[@class='focus']/a");
							if (nodeTitle != null && nodeImg != null) {
								var link = nodeImg.GetAttributeValue("href", "");
								var img = nodeImg.SelectSingleNode("img").GetAttributeValue("src", "");
								img = Regex.Replace(img, @"-\d+x\d+\.", ".");
								images.Add(new ImageItem { ImageUrl = img, Link = link, Extra = nodeTitle.InnerText.Trim(), FileName = Path.GetFileNameWithoutExtension(link) + Path.GetExtension(img) });
							}
						} else {
							timeOver = true; break;
						}
					}
				}
			}
			return timeOver;
		}

		private void InitHttp()
		{
			var _httpHandler = new HttpClientHandler();
			_httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			var _httpClient = new HttpClient(_httpHandler);
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36");

			//LoadCookie(_httpHandler);

			if (httpClient != null) {
				httpClient.Dispose();
			}
			//httpHandler = _httpHandler;
			httpClient = _httpClient;
		}
	}
}
