using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Avxm
{
	public class JsCover
	{
		public JsContent content;
		public string error;
	}
	public class JsContent
	{
		public string title;
		public JsCategory category;
		public JsImage[] images;
		public string error;

		public static JsContent FromJson(string json)
		{
			JsContent rlt = null;
			var cover = JsonConvert.DeserializeObject<JsCover>(json);
			if (cover != null) {
				rlt = cover.content;
				rlt.error = cover.error;
			}
			return rlt;
		}
	}
	public class JsCategory
	{
		public string label;
	}
	public class JsImage
	{
		public string link;
		public string type;
	}

	public class UnityLoader
	{
		private HttpClient httpClient;
		public string Version;
		public string Session;

		public static readonly Regex regName = new Regex("[\\/?:*\"><|]+");
		//var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
		//var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));

		public async Task<bool> GetAssert(string fname)
		{
			var rlt = false;
			HttpResponseMessage resp = null;
			var aid = "";
			var idx = fname.IndexOf('_');
			if (idx > 0) {
				aid = fname.Substring(0, idx);
				if (!int.TryParse(aid, out idx)) {
					Console.WriteLine("*NAME " + fname);
					return false;
				}
			}

			if (httpClient == null) {
				InitHttp();
			}

			try {
				Console.WriteLine();
				Console.WriteLine(fname);
				resp = await httpClient.GetAsync("https://www.assetstore.unity3d.com/api/en-US/content/overview/" + aid + ".json");
				if (resp != null) {
					if (resp.StatusCode == HttpStatusCode.OK) {
						var json = await resp.Content.ReadAsStringAsync();
						resp.Dispose(); resp = null;
						var content = JsContent.FromJson(json);
						if (content != null && content.error != null) {
							Console.WriteLine(string.Format("*{0} {1}", content.error, fname));
							content.error = "0" + content.error;
							if (!Directory.Exists(content.error)) {
								Directory.CreateDirectory(content.error);
							}
							File.Move(fname, content.error + "/" + fname);
							rlt = true;
						} else if (content != null) {
							var dir = content.category.label + "/" + regName.Replace(content.title, "_");
							if (!Directory.Exists(dir)) {
								Directory.CreateDirectory(dir);
							}
							Console.Write(dir);
							for (var i = 0; i < content.images.Length; ++i) {
								Console.Write(" " + (i + 1));
								var img = content.images[i];
								if (img.type != "screenshot") {
									File.WriteAllText(string.Format("{0}/{1:000}.{2}.txt", dir, (i + 1), img.type), img.link);
									Console.Write("*");
								} else {
									try {
										if (!img.link.StartsWith("http", StringComparison.CurrentCulture)) {
											img.link = "http:" + img.link;
										}
										resp = await httpClient.GetAsync(img.link);
										if (resp != null && resp.StatusCode == HttpStatusCode.OK) {
											var data = await resp.Content.ReadAsByteArrayAsync();
											File.WriteAllBytes(string.Format("{0}/{1:000}.{2}", dir, (i + 1), img.link.Substring(img.link.Length - 3)), data);
										} else {
											File.WriteAllText(string.Format("{0}/{1:000}.{2}.txt", dir, (i + 1), img.link.Substring(img.link.Length - 3)), img.link);
											Console.Write("E");
										}
									} catch (Exception ex) {
										while (ex.InnerException != null) { ex = ex.InnerException; }
										Console.WriteLine(ex.Message);
									}
								}
							}
							File.Move(fname, dir + "/" + fname);
							Console.WriteLine(" OK");
							rlt = true;
						}
					} else {
						Console.Write(string.Format("*ERROR {0}: ", resp.StatusCode));
						Console.WriteLine(await resp.Content.ReadAsStringAsync());
						resp.Dispose(); resp = null;
					}
				}
			} catch (Exception ex) {
				while (ex.InnerException != null) { ex = ex.InnerException; }
				Console.WriteLine(ex.Message);
			} finally {
				if (resp != null) {
					resp.Dispose(); resp = null;
				}
			}

			return rlt;
		}
		public void InitHttp()
		{
			var _httpHandler = new HttpClientHandler();
			_httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			_httpHandler.CookieContainer = new CookieContainer();
			_httpHandler.UseCookies = true;

			var _httpClient = new HttpClient(_httpHandler);
			_httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
			_httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch, br");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2");
			_httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			_httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
			_httpClient.DefaultRequestHeaders.Add("Referer", "https://www.assetstore.unity3d.com/en/");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.84 Safari/537.36");
			_httpClient.DefaultRequestHeaders.Add("X-Kharma-Version", Version);
			_httpClient.DefaultRequestHeaders.Add("X-Requested-With", "UnityAssetStore");
			_httpClient.DefaultRequestHeaders.Add("X-Unity-Session", Session);

			//httpHandler.CookieContainer.Add(new Cookie(parts[5], parts[6], parts[2], parts[0]));

			if (httpClient != null) {
				httpClient.Dispose();
			}
			httpClient = _httpClient;
		}
	}
}
