using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text;

namespace Avxm
{
	public class MayaLoader
	{
        private const string sitePrefix = "http://www.uumaya.com/";
		private const string loginUrl = "logging.php?action=login";
		private const string siteUrl = "forumdisplay.php?fid=5&filter=0&orderby=dateline&page=";
		//private string cookieFile = "maya.txt";
		private HttpClientHandler httpHandler;
		private HttpClient httpClient;

		public async Task<bool> Login()
		{
			var rlt = false;
			if (httpClient == null) {
				InitHttp();
			}

			HttpResponseMessage resp = null;

			try {
				resp = await httpClient.GetAsync(sitePrefix + loginUrl);
				if (resp.StatusCode == HttpStatusCode.OK) {
					resp.Dispose(); resp = null;

					var values = new Dictionary<string, string>();
                    values.Add("formhash", "638fea1c");
					values.Add("referer", "index.php");
					values.Add("loginfield", "username");
					values.Add("username", "twinco");
					values.Add("password", "twinco00");
					values.Add("questionid", "0");
					values.Add("answer", "");
					values.Add("cookietime", "315360000");
					values.Add("loginmode", "");
					values.Add("styleid", "");
					values.Add("loginsubmit", "%CC%E1+%26%23160%3B+%BD%BB");
					var content = new FormUrlEncodedContent(values);
					resp = await httpClient.PostAsync(sitePrefix + "logging.php?action=login&", content);
					if (resp.StatusCode == HttpStatusCode.OK) {
						rlt = !string.IsNullOrEmpty(GetCookieValue(sitePrefix, "cdb_auth"));
						if (!rlt) {
							var data = await resp.Content.ReadAsByteArrayAsync();
							//var html = Encoding.GetEncoding("GB18030").GetString(data);
							var html = Encoding.UTF8.GetString(data);
							Console.WriteLine(html);
						}
						resp.Dispose(); resp = null;
					}
				}
				if (resp != null) {
					resp.Dispose(); resp = null;
				}
			} catch (Exception ex) {
				while (ex.InnerException != null) { ex = ex.InnerException; }
				Console.WriteLine(); Console.WriteLine(ex.Message);
			}

			if (resp != null) {
				resp.Dispose(); resp = null;
			}

			return rlt;
		}

		public async Task<List<ImageItem>> LoadImageUrls(int hours)
		{
			if (httpClient == null) {
				InitHttp();
			}

			var pageUrls = new List<string>();
			var imageItems = new List<ImageItem>();
			var timeOver = false;
			var dateEnd = DateTime.Now.AddHours(-hours);
			//var enc = Encoding.GetEncoding("GBK");
			var enc = Encoding.UTF8;

			HttpResponseMessage resp = null;
			int pageIdx = 1;

			try {
				while (!timeOver) {
					Console.WriteLine("Read Page " + pageIdx);
					try {
						resp = await httpClient.GetAsync(sitePrefix + siteUrl + pageIdx); ++pageIdx;
					} catch (Exception) {
						await Task.Delay(100); Console.Write("*");
						resp = await httpClient.GetAsync(sitePrefix + siteUrl + pageIdx); ++pageIdx;
					}
					if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
						var data = await resp.Content.ReadAsByteArrayAsync();
						var html = enc.GetString(data);
						timeOver = ParsePageUrls(html, dateEnd, pageUrls);
					} else {
						timeOver = true;
					}
					if (resp != null) {
						resp.Dispose(); resp = null;
					}
				}
				Console.WriteLine("Total Page Items " + pageUrls.Count);

				pageIdx = 0;
				foreach (var page in pageUrls) {
					Console.Write(pageIdx++);
					resp = await httpClient.GetAsync(page);
					if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
						var data = await resp.Content.ReadAsByteArrayAsync();
						var html = enc.GetString(data);
						Console.Write('|');
						Console.Write(ParseImageUrls(html, pageIdx, imageItems));
						Console.Write('|');
						Console.Write(imageItems.Count);
						Console.Write(' ');
					}
					if (resp != null) {
						resp.Dispose(); resp = null;
					}
					Console.WriteLine();
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
		private bool ParsePageUrls(string html, DateTime dateEnd, List<string> pages)
		{
			bool timeOver = false;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var imgNodes = doc.DocumentNode.SelectNodes("//td[@class='f_title']/a[1]");
			var timeNodes = doc.DocumentNode.SelectNodes("//td[@class='f_last']/span/a");
			if (imgNodes != null && timeNodes != null && imgNodes.Count == timeNodes.Count) {
				DateTime time;
				for (int i = 0; i < imgNodes.Count && !timeOver; ++i) {
					DateTime.TryParseExact(timeNodes[i].InnerText, "yyyy-M-d  HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out time);

					if (time > dateEnd) {
						//viewthread.php?tid=2086308&amp;extra=page%3D1%26amp%3Bfilter%3D0%26amp%3Borderby%3Ddateline
						//http://www.mayaback.com/viewthread.php?tid=2086170&extra=page%3D2%26amp%3Bfilter%3D0%26amp%3Borderby%3Ddateline
						var href = imgNodes[i].GetAttributeValue("href", "");
						//href = href.Substring(0, href.IndexOf("%26"));
						href = href.Replace("&amp;", "&");
						href = sitePrefix + href;
						pages.Add(href);
					} else {
						timeOver = true;
					}
				}
			} else {
				timeOver = true;
			}

			//var nodes = doc.DocumentNode.SelectNodes("//tr/td[@class='line-content']/span/a");
			//foreach (var node in nodes) {
			//	var href = node.GetAttributeValue("href", "");
			//	if (href.EndsWith("dateline")) {
			//		pages.Add(href);
			//	} else if (href.EndsWith("lastpost")) {
			//		var lastTime = node.ParentNode.ParentNode.InnerText;
			//		if (lastTime.Length > 40) {
			//			lastTime = lastTime.Substring(lastTime.Length - 40, 17);
			//			lastTime = lastTime.Substring(lastTime.IndexOf("20"));

			//			DateTime dateItem;
			//			DateTime.TryParseExact(lastTime, "yyyy-M-d  HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dateItem);

			//			if (dateItem < dateEnd) {
			//				timeOver = true; break;
			//			}
			//		}
			//	}
			//}
			return timeOver;
		}
		private int ParseImageUrls(string html, int pageIdx, List<ImageItem> images)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var idx = 0;
			var nodes = doc.DocumentNode.SelectNodes("//div[@class='t_msgfont']/img");
			foreach (var node in nodes) {
				var href = node.GetAttributeValue("src", "");
				images.Add(new ImageItem { MainIdx = pageIdx, SubIdx = idx++, ImageUrl = href });
			}
			return idx;
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
			_httpHandler.CookieContainer = new CookieContainer();
			_httpHandler.UseCookies = true;

			if (httpClient != null) {
				httpClient.Dispose();
			}
			httpHandler = _httpHandler;
			httpClient = _httpClient;
		}
		/*private void LoadCookie(HttpClientHandler _httpHandler = null)
		{
			if (_httpHandler == null) {
				_httpHandler = this.httpHandler;
			}
			if (File.Exists(cookieFile)) {
				_httpHandler.CookieContainer = new CookieContainer();
				_httpHandler.UseCookies = true;

				string[] cookies = File.ReadAllLines(cookieFile);
				int idx = 0;
				if (cookies.Length > 2) {
					siteUrl = cookies[0];
					sitePrefix = siteUrl.Substring(0, siteUrl.LastIndexOf('/') + 1);
					idx = 2;
				}
				for (; idx < cookies.Length; ++idx) {
					var parts = cookies[idx].Split('\t');
					if (parts.Length == 7) {
						if (parts[6].Contains(',')) {
							parts[6] = parts[6].Replace(",", "%2C");
						}
						_httpHandler.CookieContainer.Add(new Cookie(parts[5], parts[6], parts[2], parts[0]));
					}
				}

				Console.WriteLine("LoadCookie " + cookieFile);
			} else {
				Console.WriteLine("*Error cookie file " + Directory.GetCurrentDirectory());
			}
		}*/
		private string GetCookieValue(string domain, string name)
		{
			string rlt = null;
			var cookies = httpHandler?.CookieContainer?.GetCookies(new Uri(domain));
			if (cookies?.Count > 0) {
				foreach (Cookie cookie in cookies) {
					if (cookie.Name == name) {
						rlt = cookie.Value; break;
					}
				}
			}
			return rlt;
		}
		private void DumpCookie()
		{
			Console.WriteLine("Dump Cookie:");
			var cookies = httpHandler?.CookieContainer?.GetCookies(new Uri(sitePrefix));
			if (cookies?.Count > 0) {
				foreach (Cookie cookie in cookies) {
					Console.WriteLine($"{cookie.Name} - {cookie.Value}");
				}
			}
			Console.WriteLine();
		}
	}
}
