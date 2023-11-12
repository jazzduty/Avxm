using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Avxm
{
	public class ZipItem
	{
		public List<ZipItem> Items { get; set; }
		public string Attribute { get; set; }
		public bool Encrypted { get; set; }
		public long Size { get; set; }
		public long SizeR { get; set; }
		public string Date { get; set; }
		public string Name { get; set; }
		public string FullName { get; set; }
	}
	public enum ZipType
	{
		Rar, S7Zip
	}
	public class ZipInfo
	{
		public ZipType ZipType { get; set; }
		public string FileName { get; set; }
		public int FileCount { get; set; }
		public List<ZipItem> ItemTree { get; set; } = new List<ZipItem>();
		public List<ZipItem> ItemFlat { get; set; } = new List<ZipItem>();
		public string Comment { get; set; }
		public string Errors { get; set; }

		public bool AddTreeItem(ZipItem item)
		{
			bool rlt = false;
			int count = 0;
			string[] parts = item.FullName.Split('/');
			if (string.IsNullOrEmpty(item.Name)) {
				item.Name = parts[parts.Length - 1];
			}

			if (parts.Length == 1) {
				bool exist = false;
				foreach (var m in ItemTree) {
					if (m.Name == item.Name) {
						exist = true; break;
					}
				}
				if (!exist) {
					ItemTree.Add(item);
					++count;
					rlt = true;
				}
			} else {
				var roots = ItemTree;
				bool exist = true;
				for (int i = 0; exist && i < parts.Length; ++i) {
					var part = parts[i];
					exist = false;
					foreach (var m in roots) {
						if (m.Name == part && m.Items != null) {
							roots = m.Items;
							exist = true; break;
						}
					}
					if (!exist && i == parts.Length - 1) {
						roots.Add(item);
						rlt = true;
					}
				}
			}

			FileCount = FileCount + count;

			return rlt;
		}
		public void RebuildFlat()
		{
			ItemFlat.Clear();
			AddFlat(ItemTree);
		}
		private void AddFlat(List<ZipItem> items)
		{
			foreach (var m in items) {
				if (m.Items == null) {
					ItemFlat.Add(m);
				} else {
					AddFlat(m.Items);
				}
			}
		}

		public void LoadRarInfo(string fileName)
		{
			ZipType = ZipType.Rar;
			FileName = fileName;
			var items = new List<ZipItem>();
			try {
				var proc = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = "rar",
						Arguments = "lt \"" + fileName + "\"",
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						StandardOutputEncoding = Encoding.UTF8
					}
				};
				proc.Start();

				var infos = new List<string>();
				var arc = "Archive: " + fileName;
				ZipItem oper = null;

				proc.StandardOutput.ReadLine(); //
				proc.StandardOutput.ReadLine(); //RAR 5.30   Copyright (c) 1993-2015 Alexander Roshal   18 Nov 2015
				proc.StandardOutput.ReadLine(); //Registered to State Grid Corporation Of China
				proc.StandardOutput.ReadLine(); //

				bool isInfo = true;
				const string tagName = "        Name: ";
				while (!proc.StandardOutput.EndOfStream) {
					string data = proc.StandardOutput.ReadLine();
					if (isInfo) {
						if (data == arc) {
							isInfo = false;
						} else {
							infos.Add(data);
						}
					} else if (data.Length > 0) {
						switch (data.Substring(0, tagName.Length)) {
							case "        Name: ": oper = new ZipItem(); oper.FullName = data.Substring(tagName.Length); break;
							case "        Type: ": if (data.Substring(tagName.Length) == "Directory") { oper.Items = new List<ZipItem>(); } break;
							case "        Size: ": oper.Size = long.Parse(data.Substring(tagName.Length)); break;
							case " Packed size: ": oper.SizeR = long.Parse(data.Substring(tagName.Length)); break;
							case "       Ratio: ": break;
							case "       mtime: ": oper.Date = data.Substring(tagName.Length, 19); break;
							case "  Attributes: ": oper.Attribute = data.Substring(tagName.Length); break;
							case "       CRC32: ": break;
							case "     Host OS: ": break;
							case " Compression: ": break;
							case "       Flags: ": oper.Encrypted = (data.Substring(tagName.Length).StartsWith("encrypted")); break;
						}
					} else if (oper != null) {
						items.Add(oper);
						oper = null;
					}
				}

				if (infos.Count > 0) {
					Comment = string.Join("\n", infos);
				}
				Errors = proc.StandardError.ReadToEnd();
			} catch (Exception ex) {
				Errors = ex.ToString();
			}

			for (int i = items.Count - 1; i >= 0; --i) {
				if (AddTreeItem(items[i])) {
					items.RemoveAt(i);
				}
			}
			for (int i = items.Count - 1; i >= 0; --i) {
				if (AddTreeItem(items[i])) {
					items.RemoveAt(i);
				}
			}
			RebuildFlat();
		}
		public void Load7zInfo(string fileName)
		{
			ZipType = ZipType.S7Zip;
			FileName = fileName;
			var items = new List<ZipItem>();
			try {
				var proc = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = "7z",
						Arguments = "l -slt \"" + fileName + "\"",
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						StandardOutputEncoding = Encoding.UTF8
					}
				};
				proc.Start();

				string data = null;
				ZipItem oper = null;

				while (!proc.StandardOutput.EndOfStream) {
					data = proc.StandardOutput.ReadLine();
					if (data == "----------") {
						break;
					}
				}

				int idx = 0;
				while (!proc.StandardOutput.EndOfStream) {
					data = proc.StandardOutput.ReadLine();
					if (!string.IsNullOrEmpty(data) && (idx = data.IndexOf(" = ")) > 0) {
						switch (data.Substring(0, idx)) {
							case "Path": oper = new ZipItem(); oper.FullName = data.Substring(idx + 3); break;
							case "Size": oper.Size = long.Parse(data.Substring(idx + 3)); break;
							case "Packed size": oper.SizeR = long.Parse(data.Substring(idx + 3)); break;
							case "Modified": oper.Date = data.Substring(idx + 3); break;
							case "Attributes": oper.Attribute = data.Substring(idx + 3); if (oper.Attribute == "D") { oper.Items = new List<ZipItem>(); } break;
							case "CRC": break;
							case "Encrypted": if (data.Substring(idx + 3) == "+") { oper.Encrypted = true; } break;
							case "Method": break;
							case "Block": break;
						}
					} else if (oper != null) {
						items.Add(oper);
						oper = null;
					}
				}
				Errors = proc.StandardError.ReadToEnd();
			} catch (Exception ex) {
				Errors = ex.ToString();
			}

			for (int i = items.Count - 1; i >= 0; --i) {
				if (AddTreeItem(items[i])) {
					items.RemoveAt(i);
				}
			}
			for (int i = items.Count - 1; i >= 0; --i) {
				if (AddTreeItem(items[i])) {
					items.RemoveAt(i);
				}
			}
			RebuildFlat();
		}
		public bool Extract(ZipItem item, string destDir)
		{
			const string passwd = "www.cgsoso.com";
			var rlt = false;
			try {
				var startInfo = new ProcessStartInfo {
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					StandardOutputEncoding = Encoding.UTF8
				};
				if (ZipType == ZipType.Rar) {
					startInfo.FileName = "rar";
					if (item.Encrypted) {
						startInfo.Arguments = "e -p" + passwd + " \"" + FileName + "\" \"" + item.FullName + "\" \"" + destDir + "\"";
					} else {
						startInfo.Arguments = "e \"" + FileName + "\" \"" + item.FullName + "\" \"" + destDir + "\"";
					}
					var proc = new Process { StartInfo = startInfo };
					rlt = proc.Start();
					proc.WaitForExit();
				} else if (ZipType == ZipType.S7Zip) {
					startInfo.FileName = "7z";
					if (item.Encrypted) {
						startInfo.Arguments = "e -p" + passwd + " -o" + destDir + " \"" + FileName + "\" \"" + item.FullName + "\"";
					} else {
						startInfo.Arguments = "e -o" + destDir + " \"" + FileName + "\" \"" + item.FullName + "\"";
					}
					var proc = new Process { StartInfo = startInfo };
					rlt = proc.Start();
					proc.WaitForExit();
				}
			} catch (Exception ex) {
				while (ex.InnerException != null) { ex = ex.InnerException; }
				Console.WriteLine(ex.Message);
			}
			return rlt;
		}

		public static ZipInfo LoadInfo(string fileName)
		{
			ZipInfo info = null;
			var ext = Path.GetExtension(fileName);
			if (ext == ".rar") {
				info = new ZipInfo();
				info.LoadRarInfo(fileName);
			} else if (ext == ".7z") {
				info = new ZipInfo();
				info.Load7zInfo(fileName);
			}

			return info;
		}
	}
}

