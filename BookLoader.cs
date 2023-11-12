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
	public class BookLoader
	{
		private string siteUrl = "http://avaxhome.unblocker.xyz/ebooks/pages/";//"http://avxhome.se/ebooks/pages/";
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
		private bool ParseImageUrls(string html, DateTime dateEnd, List<ImageItem> images)
		{
			bool timeOver = false;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var nodes = doc.DocumentNode.SelectNodes("//div[@class='col-md-12 article']/div[@class='row']/div[@class='col-lg-12']");
			if (nodes != null && nodes.Count > 0) {
				foreach (HtmlNode node in nodes) {
					HtmlNode nodeDate = node.SelectNodes("div")[1];
					if (nodeDate != null) {
						string date = nodeDate.InnerText;
						date = date.Substring(date.IndexOf("Date") + 6);
						var curDate = DateTime.MinValue;
						//DateTime.TryParseExact(date, "dd.MM.yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out curDate)
						if (DateTime.TryParse(date, out curDate) && curDate > dateEnd) {
							HtmlNode nodeTitle = node.SelectSingleNode("h1/a");
							HtmlNode nodeImg = node.SelectSingleNode("div[@class='text']/div/a/img");
							if (nodeTitle != null && nodeImg != null) {
								var img = nodeImg.GetAttributeValue("src", "").Replace("_medium", "");
								if (!img.StartsWith("http")) {
									if (img.StartsWith("//")) {
										img = "http:" + img;
									}
								}
								images.Add(new ImageItem { ImageUrl = img, Link = nodeTitle.GetAttributeValue("href", ""), FileName = img.Substring(img.LastIndexOf('/') + 1) });
							}
						} else if (!date.Contains('.')) {
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
