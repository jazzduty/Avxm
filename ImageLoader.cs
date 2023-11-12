using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace Avxm
{
	public class ImageLoader
	{
		private List<ImageItem> items;
		private int idx;
		private int pid;
		private string folder;
		private string refer;

		public async Task Download(List<ImageItem> _items, string _folder = null, string _refer = null)
		{
			items = _items;
			folder = _folder;
			refer = _refer;

			if (!string.IsNullOrEmpty(folder)) {
				if (!Directory.Exists(folder)) {
					Directory.CreateDirectory(folder);
				}
				if (folder.EndsWith("/")) {
					folder = folder.Substring(0, folder.Length - 1);
				}
			} else {
				folder = "";
			}

			Console.WriteLine("Start Download Images");

			var tasks = new Task[] {
				Task.Run(DownloadProc), Task.Run(DownloadProc), Task.Run(DownloadProc), Task.Run(DownloadProc), Task.Run(DownloadProc),
				Task.Run(DownloadProc), Task.Run(DownloadProc), Task.Run(DownloadProc)
			};
			await Task.WhenAll(tasks);

			//await DownloadProc();

			Console.WriteLine();
		}
		public async Task LoadJson(string type)
		{
			if (File.Exists(type + ".json")) {
				var json = File.ReadAllText(type + ".json");
				var images = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ImageItem>>(json);
				if (images != null && images.Count > 0) {
					await Download(images, type);
				}
			}
		}
		private async Task DownloadProc()
		{
			var httpHandler = new HttpClientHandler();
			httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			httpHandler.UseCookies = false;
			//httpHandler.AllowAutoRedirect = true;
			//httpHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

			var httpClient = new HttpClient(httpHandler);
			httpClient.DefaultRequestHeaders.Add("Accept", "image/webp,image/*,*/*;q=0.8");
			httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch");
			httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2");
			httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
			httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
			httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36");
			if (refer != null) {
				httpClient.DefaultRequestHeaders.Add("Referer", refer);
			}
			//switch (folder) {
			//	case "maya": httpClient.DefaultRequestHeaders.Add("Referer", "http://www.mayathis.com/viewthread.php"); break;
			//	case "blame10": httpClient.DefaultRequestHeaders.Add("Referer", "http://manhua.dmzj.com/blame/412.shtml"); break;
			//}
			//httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
			//httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
			//httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_1) AppleWebKit/602.2.14 (KHTML, like Gecko) Version/10.0.1 Safari/602.2.14");

			HttpResponseMessage resp = null;
			var retry = 0;
			var cpid = Interlocked.Increment(ref pid) - 1;
			var cidx = Interlocked.Increment(ref idx) - 1;
			Console.Write(string.Format("[{0}]", cpid));
			while (cidx < items.Count) {
				var item = items[cidx];
				if (!string.IsNullOrEmpty(item.ImageUrl) && item.ImageUrl.StartsWith("http")) {
					var fname = "";
                    var ext = item.ImageUrl.Substring(item.ImageUrl.LastIndexOf('.'));
                    if (ext == ".jpeg") {
                        ext = ".jpg";
                    }
                    if (item.FileName == null) {
                        fname = string.Format("{0}/{1:000}{2:00}{3}", folder, item.MainIdx, item.SubIdx, ext);
                    } else if (!fname.Contains(".")) {
                        fname = string.Format("{0}/{1}{2}", folder, item.FileName, ext);
					} else {
						fname = string.Format("{0}/{1}", folder, item.FileName);
					}
					if (!File.Exists(fname)) {
                        try {
                            try {
                                resp = await httpClient.GetAsync(item.ImageUrl, new CancellationTokenSource(5000).Token);
                            } catch (Exception) {
                                await Task.Delay(100); Console.Write("*");
                                resp = await httpClient.GetAsync(item.ImageUrl, new CancellationTokenSource(5000).Token);
                            }
                            if (resp.StatusCode == HttpStatusCode.OK) {
                                var data = await resp.Content.ReadAsByteArrayAsync();
                                File.WriteAllBytes(fname, data);
                                if (retry > 0) { retry = 0; }
                                Console.Write(string.Format(" {0}|{1}", cidx, cpid));
                            } else if (resp.StatusCode == HttpStatusCode.Moved) {
                                item.ImageUrl = resp.Headers.Location.ToString();
                                resp.Dispose();
                                try {
                                    resp = await httpClient.GetAsync(item.ImageUrl, new CancellationTokenSource(5000).Token);
                                } catch (Exception) {
                                    await Task.Delay(100); Console.Write("*");
                                    resp = await httpClient.GetAsync(item.ImageUrl, new CancellationTokenSource(5000).Token);
                                }
                                if (resp.StatusCode == HttpStatusCode.OK) {
                                    var data = await resp.Content.ReadAsByteArrayAsync();
                                    File.WriteAllBytes(fname, data);
                                    if (retry > 0) { retry = 0; }
                                    Console.Write(string.Format(" {0}|{1}r", cidx, cpid));
                                } else {
                                    ++retry;
                                    Console.Write(string.Format(" {0}|{1}r*", cidx, cpid));
                                    Console.Write(resp.StatusCode);
                                }
                            } else {
                                ++retry;
                                Console.Write(string.Format(" {0}|{1}*", cidx, cpid));
                                Console.Write(resp.StatusCode);
                            }
                        } catch (TaskCanceledException) {
                            Console.Write(string.Format(" {0}|{1}c*", cidx, cpid));
                        } catch (Exception ex) {
							//++retry;
							Console.Write(string.Format(" {0}|{1}*", cidx, cpid));
							//while (ex.InnerException != null) { ex = ex.InnerException; }
							Console.Write(ex.Message);
						}
						if (resp != null) {
							resp.Dispose(); resp = null;
						}
					} else {
						Console.Write(string.Format(" {0}|{1}-", cidx, cpid));
					}
				}

				if (retry == 0 || retry > 1) {
					cidx = Interlocked.Increment(ref idx) - 1;
				} else {
					await Task.Delay(1000);
				}
			}

			if (resp != null) {
				resp.Dispose(); resp = null;
			}
		}
	}
}
