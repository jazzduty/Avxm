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
	public class BaseLoader
	{
		protected HttpClientHandler httpHandler;
		protected HttpClient httpClient;

		protected void InitHttp()
		{
			var _httpHandler = new HttpClientHandler();
			_httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			var _httpClient = new HttpClient(_httpHandler);
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			_httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch");
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2");
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_13_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");

			//LoadCookie(_httpHandler);
			_httpHandler.CookieContainer = new CookieContainer();
			_httpHandler.UseCookies = true;

			if (httpClient != null) {
				httpClient.Dispose();
			}
			httpHandler = _httpHandler;
			httpClient = _httpClient;
		}
		private class CookiePoco
		{
			public string domain { get; set; }
			public float expirationDate { get; set; }
			public bool hostOnly { get; set; }
			public bool httpOnly { get; set; }
			public string name { get; set; }
			public string path { get; set; }
			public string sameSite { get; set; }
			public bool secure { get; set; }
			public bool session { get; set; }
			public string storeId { get; set; }
			public string value { get; set; }
			public int id { get; set; }
		}
		protected void LoadCookie(string cookieFile, string rootUrl, HttpClientHandler _httpHandler = null)
		{
			if (_httpHandler == null) {
				_httpHandler = httpHandler;
			}
			List < CookiePoco > pocos = null;
			if (File.Exists(cookieFile) && (pocos = JsonConvert.DeserializeObject<List<CookiePoco>>(File.ReadAllText((cookieFile)))) != null) {
				//_httpHandler.CookieContainer = new CookieContainer();
				//_httpHandler.UseCookies = true;
				var url = new Uri(rootUrl);
				foreach (var p in pocos) {
					_httpHandler.CookieContainer.Add(url, new Cookie(p.name, p.value, p.path, p.domain));
				}
				Console.WriteLine("LoadCookie " + cookieFile);
			} else {
				Console.WriteLine("*Error cookie file " + Directory.GetCurrentDirectory());
			}
		}
		protected string GetCookieValue(string domain, string name)
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
        protected void DumpCookie()
        {
            Console.WriteLine("Dump Cookie:");
            //var cookies = httpHandler?.CookieContainer?.GetCookies(new Uri("sitePrefix"));
            //if (cookies?.Count > 0) {
            //    foreach (Cookie cookie in cookies) {
            //        Console.WriteLine($"{cookie.Name} - {cookie.Value}");
            //    }
            //}
            var cookies = httpHandler?.CookieContainer?.GetAllCookies();
            foreach (Cookie cookie in cookies) {
                Console.WriteLine($"{cookie.Name} - {cookie.Value}");
            }
            Console.WriteLine();
        }
    }

    public static class CookieContainerExtension
    {
        public static CookieCollection GetAllCookies(this CookieContainer container)
        {
            var allCookies = new CookieCollection();
            /*var domainTableField = container.GetType().GetRuntimeFields().FirstOrDefault(x => x.Name == "m_domainTable");
            var domains = (IDictionary)domainTableField.GetValue(container);

            foreach (var val in domains.Values) {
                var type = val.GetType().GetRuntimeFields().First(x => x.Name == "m_list");
                var values = (IDictionary)type.GetValue(val);
                foreach (CookieCollection cookies in values.Values) {
                    allCookies.Add(cookies);
                }
            }*/
            return allCookies;
        }
    }
}
