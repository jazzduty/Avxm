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
	public class Book2Loader
	{
		private string siteUrl = "https://sanet.cd/blogs/ebooksa/";
		//private HttpClientHandler httpHandler;
		private HttpClient httpClient;

		public async Task<List<ImageItem>> LoadImageUrls(int hours)
		{
			if (httpClient == null) {
				InitHttp();
			}

			var imageItems = new List<ImageItem>();
			var timeOver = false;
			var dateEnd = DateTime.Now.AddHours(-hours);

			HttpResponseMessage resp = null;
			int pageIdx = 1;

			try {
				while (!timeOver && pageIdx <= 100) {
					Console.WriteLine("Read Page " + pageIdx);
					try {
						resp = await httpClient.GetAsync(pageIdx > 1 ? $"{siteUrl}page-{pageIdx}/" : siteUrl); ++pageIdx;
					} catch (Exception) {
						await Task.Delay(100); Console.WriteLine("*");
						resp = await httpClient.GetAsync(pageIdx > 1 ? $"{siteUrl}page-{pageIdx}/" : siteUrl); ++pageIdx;
					}
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
		private bool ParseImageUrls(string html, DateTime dateEnd, List<ImageItem> images)
		{
			bool timeOver = false;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var nodes = doc.DocumentNode.SelectNodes("//ul[@class='main_items_list clear']/li");
			if (nodes != null && nodes.Count > 0) {
				foreach (HtmlNode node in nodes) {
					var nodeLink = node.SelectSingleNode("header/div[@class='list_item_title']/h2/a");
					var nodeDate = node.SelectSingleNode("header/div[@class='post-details clear']/ul/li/time");
					var nodeImg = node.SelectSingleNode("div[@class='item_preview_text clear']/div/a/img");
					if (nodeImg == null) {
						nodeImg = node.SelectSingleNode("div[@class='item_preview_text clear']/div/img");
					}
					if (nodeLink != null && nodeDate != null && nodeImg != null && nodeLink.GetAttributeValue("rel", null) == null) {
						var curDate = DateTime.MinValue;
						var date = nodeDate.GetAttributeValue("datetime", "");
						if (DateTime.TryParse(date, out curDate) && curDate > dateEnd) {
							var link = nodeLink.GetAttributeValue("href", "");
							var title = nodeLink.InnerText;
							var img = nodeImg.GetAttributeValue("data-src", "");
							images.Add(new ImageItem { ImageUrl = img, Link = link, FileName = Path.GetFileNameWithoutExtension(link) + Path.GetExtension(img) });
						} else {
							timeOver = true; break;
						}
					}
				}
			}
			return timeOver && images.Count < 1000;
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
