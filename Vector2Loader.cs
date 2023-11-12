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
	public class Vector2Loader
	{
		//private string siteUrl = "https://sanet.cd/blogs/graphicriverblog/";
		private string siteUrl = "https://sanet.cd/graphics/tag/vector-graphics/";
		//private HttpClientHandler httpHandler;
		private HttpClient httpClient;

		public async Task<List<ImageItem>> LoadImageUrls(string endDate)
		{
			if (httpClient == null) {
				InitHttp();
			}

			var imageItems = new List<ImageItem>();
			var timeOver = false;
			var dateEnd = DateTime.MinValue; 
			if (string.IsNullOrEmpty(endDate) || !DateTime.TryParse(endDate, out dateEnd)) {
				dateEnd = DateTime.Now.AddDays(-15);
			}

			HttpResponseMessage resp = null;
			int pageIdx = 1;

			try {
				while (!timeOver && pageIdx <= 100) {
					Console.WriteLine("Read Page " + pageIdx);
					resp = await httpClient.GetAsync(pageIdx > 1 ? $"{siteUrl}page-{pageIdx}/" : siteUrl); ++pageIdx;
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

				pageIdx = 1;
				foreach (var item in imageItems) {
					Console.WriteLine(string.Format("{0:##0} {1}", pageIdx++, item.FileName));
					resp = await httpClient.GetAsync(item.Link);
					if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
						var html = await resp.Content.ReadAsStringAsync();
						ParseDownloadUrl(html, item);
					}
					if (resp != null) {
						resp.Dispose(); resp = null;
					}
				}
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
					var nodeDate = node.SelectSingleNode("header/div[@class='post-details clear']/ul[@class='time-by-in']/li/time");
					if (nodeDate == null) {
						node.SelectSingleNode("header/div[@class='postinfo clear']/time");
					}
					var nodeInfo = node.SelectSingleNode("div[@class='item_preview_text clear']/div[@class='release-info']");
					if (nodeInfo == null) {
						nodeInfo = node.SelectSingleNode("div[@class='item_preview_text clear']/div[@class='center']/b");
						if (nodeInfo != null) {
							nodeInfo = nodeInfo.ParentNode;
						}
					}
					var nodeImg = node.SelectSingleNode("div[@class='item_preview_text clear']/div[@class='center']/a/img");
					if (nodeImg == null) {
						nodeImg = node.SelectSingleNode("div[@class='item_preview_text clear']/div[@class='center']/img");
					}
					if (nodeLink != null && nodeDate != null && nodeInfo != null && nodeImg != null && nodeLink.GetAttributeValue("rel", null) == null) {
						var curDate = DateTime.MinValue;
						var date = nodeDate.GetAttributeValue("datetime", "");
						if (DateTime.TryParse(date, out curDate) && curDate > dateEnd) {
							var link = nodeLink.GetAttributeValue("href", "");
							var title = nodeLink.InnerText;
							if (title.StartsWith("Vectors -")) {
								var img = nodeImg.GetAttributeValue("data-src", "");
								img = img.Replace("/th_", "/");
								var info = nodeInfo.InnerText.Trim();
								info = info.Substring(info.LastIndexOf('|') + 2);
								title = title.Substring(9);
								if (title.StartsWith("-")) {
									title = title.Substring(1);
								}
								title = title.Trim();
								images.Add(new ImageItem { PubDate = date, ImageUrl = img, Link = link, Extra = info,
									FileName = title + Path.GetExtension(img) });
							}
						} else {
							timeOver = true; break;
						}
					} else if (nodeLink == null || nodeDate == null || nodeInfo == null || nodeImg == null) {
						nodeInfo = null;
					}
				}
			}
			return timeOver && images.Count < 1000;
		}
		private bool ParseDownloadUrl(string html, ImageItem item)
		{
			bool timeOver = false;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			try {
				foreach (var node in doc.DocumentNode.SelectNodes("//a")) {
					if (node.GetAttributeValue("href", "").StartsWith("http://nitroflare.com/")) {
						item.Link = node.GetAttributeValue("href", null);
					}
				}
			} catch (Exception) {
				timeOver = true;
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
