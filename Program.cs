using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Avxm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //AmazonProc().Wait();

            //Directory.SetCurrentDirectory("/Volumes/Bluema/Vector");
            //args = new string[] { "vector", "unpack" };
            //args = new string[] { "json" };

            //args = new string[] { "unity", "soso", "15 Mar 2017 00:27:24" };
            //args = new string[] { "vector", "img2", "2017-10-11T00:44:45-05:00" };
            //Directory.SetCurrentDirectory("/Users/yong/Downloads");
            //args = new string[] { "unity", "soso" };
            //Directory.SetCurrentDirectory("Z:\\Incomming\\temp");
            //args = new string[] { "dmzj" };
            //args = new string[] { "unity", "store", "5.5.0-r88001", "26c4202eb475d02864b40827dfff11a14657aa41" };
            //Directory.SetCurrentDirectory("/Volumes/Bluema/Incomming/Maya");
            args = new string[] { "maya", "700" };
            Directory.SetCurrentDirectory("/Volumes/Bluema/Incomming/aaa");

            var hours = 26;
            if (args.Length > 0) {
                if (args.Length > 1) {
                    int.TryParse(args[1], out hours);
                }
                switch (args[0]) {
                    case "all": BookProc(hours); DayProc(hours); break;
                    case "maya": MayaProc(hours); break;
                    case "book": BookProc(hours); break;
                    case "day": DayProc(hours); break;
                    case "json": JsonProc(); break;
                    case "dmzj": DmzjProc(); break;
                    case "vector":
                        if (args.Length > 1) {
                            if (args[1] == "unpack") {
                                var dir = "/Volumes/Bluema/Incomming/Vectors/";
                                if (args.Length > 2) {
                                    dir = args[2];
                                }
                                VectorUnpackProc(dir);
                            } else if (args[1] == "img" && args.Length == 3) {
                                VectorListProc(args[2]);
                            } else if (args[1] == "img2" && args.Length == 3) {
                                Vector2ListProc(args[2]);
                            } else {
                                PrintUsage();
                            }
                        } else {
                            PrintUsage();
                        }
                        break;
                    case "unity":
                        if (args.Length > 1) {
                            if (args[1] == "unpack") {
                                UnityUnpackProc();
                            } else if (args[1] == "ids") {
                                UnityIdsProc();
                            } else if (args[1] == "merge") {
                                UnityMergeProc(args.Length > 2);
                            } else if (args[1] == "store" && args.Length == 4) {
                                UnityStoreProc(args[2], args[3]).Wait();
                            } else if (args[1] == "soso") {
                                CgsosoProc(args.Length > 2 ? args[2] : "");
                            } else {
                                PrintUsage();
                            }
                        } else {
                            PrintUsage();
                        }
                        break;
                }
            } else {
                PrintUsage();
            }
            //Console.ReadLine();
        }
        public static void PrintUsage()
        {
            Console.WriteLine("all");
            Console.WriteLine("maya [hours]");
            Console.WriteLine("book [hours]");
            Console.WriteLine("day [hours]");
            Console.WriteLine("json");
            Console.WriteLine("dmzj [name,url,imgroot,json]");
            Console.WriteLine("vector unpack/img/img2");
            Console.WriteLine("unity unpack/ids/store/merge/soso");
        }

        public static void MayaProc(int hours)
        {
            Console.WriteLine();
            Console.WriteLine("Start MayaProc");
            var loader = new MayaLoader();
            if (loader.Login().Result) {
                Console.WriteLine("Login OK");
                var images = loader.LoadImageUrls(hours).Result;
                if (images != null && images.Count > 0) {
                    var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                    var json = JsonConvert.SerializeObject(images, Formatting.Indented, jset);
                    File.WriteAllText("maya.json", json);

                    var imgloader = new ImageLoader();
                    imgloader.Download(images, "maya").Wait();
                }
            } else {
                Console.WriteLine("** Not Login");
            }
        }
        public static void BookProc(int hours)
        {
            Console.WriteLine();
            Console.WriteLine("Start BookProc");
            var loader = new Book2Loader();
            var images = loader.LoadImageUrls(hours).Result;
            if (images != null && images.Count > 0) {
                var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(images, Formatting.Indented, jset);
                File.WriteAllText("book.json", json);

                var imgloader = new ImageLoader();
                imgloader.Download(images, "book").Wait();
            }
        }
        public static void DayProc(int hours)
        {
            Console.WriteLine();
            Console.WriteLine("Start DayProc");
            var loader = new DayLoader();
            var images = loader.LoadImageUrls(hours).Result;
            if (images != null && images.Count > 0) {
                var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(images, Formatting.Indented, jset);
                File.WriteAllText("day.json", json);

                var imgloader = new ImageLoader();
                imgloader.Download(images, "day").Wait();
            }
        }
        public static void JsonProc()
        {
            Console.WriteLine();
            Console.WriteLine("Start JsonProc");
            var imgloader = new ImageLoader();
            imgloader.LoadJson("maya").Wait();
            imgloader.LoadJson("book").Wait();
            imgloader.LoadJson("day").Wait();
            imgloader.LoadJson("vector").Wait();
        }
        public static void VectorListProc(string endDate)
        {
            Console.WriteLine();
            Console.WriteLine("Start VectorProc");
            var loader = new VectorLoader();
            var images = loader.LoadImageUrls(endDate).Result;
            if (images != null && images.Count > 0) {
                var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(images, Formatting.Indented, jset);
                File.WriteAllText("vector.json", json);

                Console.WriteLine("*********************************");
                Console.WriteLine("  Date From : " + endDate);
                Console.WriteLine("    Date To : " + images[0].PubDate);
                Console.WriteLine("Total Count : " + images.Count);
                Console.WriteLine("*********************************");
                foreach (var item in images) {
                    Console.WriteLine("[" + Path.GetFileName(item.FileName) + "][" + item.Extra + "]");
                    Console.WriteLine(item.Link);
                }
                Console.WriteLine();

                var imgloader = new ImageLoader();
                imgloader.Download(images, "vector").Wait();
            }
        }
        public static void Vector2ListProc(string endDate)
        {
            Console.WriteLine();
            Console.WriteLine("Start VectorProc");
            var loader = new Vector2Loader();
            var images = loader.LoadImageUrls(endDate).Result;
            if (images != null && images.Count > 0) {
                var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(images, Formatting.Indented, jset);
                File.WriteAllText("vector2.json", json);

                Console.WriteLine("*********************************");
                Console.WriteLine("  Date From : " + endDate);
                Console.WriteLine("    Date To : " + images[0].PubDate);
                Console.WriteLine("Total Count : " + images.Count);
                Console.WriteLine("*********************************");
                foreach (var item in images) {
                    Console.WriteLine("[" + Path.GetFileName(item.FileName) + "][" + item.Extra + "]");
                    Console.WriteLine(item.Link);
                }
                Console.WriteLine();

                var imgloader = new ImageLoader();
                imgloader.Download(images, "vector2").Wait();
            }
        }
        public static void CgsosoProc(string endDate)
        {
            Console.WriteLine();
            Console.WriteLine("Start CgsosoProc");
            var loader = new CgsosoLoader();
            loader.SosoProc(endDate).Wait();
        }
        public static void VectorUnpackProc(string dir)
        {
            if (!Directory.Exists("temp")) {
                Directory.CreateDirectory("temp");
            }
            Directory.SetCurrentDirectory("temp");

            var sb = new StringBuilder();
            var fsAi = new List<string>();
            var fsEps = new List<string>();

            var files = Directory.GetFiles(dir, "*.rar", SearchOption.TopDirectoryOnly);
            foreach (var file in files) {
                Console.WriteLine();
                Console.WriteLine("********************");
                Console.WriteLine();

                System.Diagnostics.Process.Start("rar", "x \"" + file + "\"").WaitForExit();

                Console.WriteLine();
                Console.WriteLine("vector files");
                fsAi.Clear(); fsEps.Clear();

                int idx = 0;
                var fvec = Directory.GetFiles(".", "*", SearchOption.AllDirectories);
                foreach (var v in fvec) {
                    var ext = Path.GetExtension(v).ToLower();
                    if (ext == ".ai") {
                        fsAi.Add(v);
                    } else if (ext == ".eps") {
                        fsEps.Add(v);
                    }
                }
                if (fsAi.Count > 0 || fsEps.Count > 0) {
                    foreach (var fs in fsAi) {
                        File.Move(fs, (++idx).ToString("00") + ".ai");
                        Console.WriteLine(fs);
                    }
                    foreach (var fs in fsEps) {
                        File.Move(fs, (++idx).ToString("00") + ".eps");
                        Console.WriteLine(fs);
                    }

                    sb.Clear();
                    sb.Append("a \"");
                    sb.Append(Path.GetDirectoryName(file));
                    sb.Append("/0Pack/");
                    sb.Append(Path.GetFileNameWithoutExtension(file));
                    sb.Append(".rar\" ");
                    for (idx = 1; idx <= fsAi.Count; ++idx) {
                        sb.Append(idx.ToString("00"));
                        sb.Append(".ai ");
                    }
                    if (fsEps.Count > 0) {
                        for (; idx <= fsAi.Count + fsEps.Count; ++idx) {
                            sb.Append(idx.ToString("00"));
                            sb.Append(".eps ");
                        }
                    }
                    System.Diagnostics.Process.Start("rar", sb.ToString()).WaitForExit();
                } else {
                    throw new Exception();
                }

                // clean up
                var dclean = Directory.GetDirectories(".");
                foreach (var dc in dclean) {
                    Directory.Delete(dc, true);
                }
                var fclean = Directory.GetFiles(".");
                foreach (var fc in fclean) {
                    File.Delete(fc);
                }
                File.Delete(file);
            }
        }
        public static void DmzjProc()
        {
            if (!File.Exists("dmzj.txt")) {
                Console.WriteLine("*ERR dmzj.txt NOT Exists");
            } else {
                try {
                    // name, refer, imgroot, json(pages)
                    var data = File.ReadAllLines("dmzj.txt");
                    var list = JsonConvert.DeserializeObject<List<string>>(data[3]);
                    var idx = 1;
                    var images = new List<ImageItem>();
                    foreach (var m in list) {
                        images.Add(new ImageItem { FileName = $"{idx:000}{Path.GetExtension(m).ToLower()}", ImageUrl = data[2] + m });
                        ++idx;
                    }
                    var imgloader = new ImageLoader();
                    imgloader.Download(images, data[0], data[1]).Wait();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void TreeDir(string path, List<string> list)
        {
            var dirs = Directory.GetDirectories(path);
            if (dirs != null && dirs.Length > 0) {
                if (path == ".") {
                    foreach (var dir in dirs) {
                        TreeDir(dir.Substring(2), list);
                    }
                } else {
                    foreach (var dir in dirs) {
                        TreeDir(dir, list);
                    }
                }
            } else {
                var empty = true;
                var files = Directory.GetFiles(path);
                foreach (var file in files) {
                    var ext = Path.GetExtension(file);
                    if (ext == ".jpg" || ext == ".rar" || ext == ".zip" || ext == ".unitypackage") {
                        empty = false; break;
                    }
                }
                if (!empty) {
                    list.Add(path);
                }
            }
        }
        static void NormalizeTree(string path, Regex regPath)
        {
            var idx = path.LastIndexOf('/');
            if (idx >= 0) {
                var fname = path.Substring(idx + 1);
                var rname = regPath.Replace(fname, "_");
                if (fname != rname) {
                    var rpath = path.Substring(0, idx + 1) + rname;
                    Directory.Move(path, rpath);
                    Console.WriteLine(path);
                    Console.WriteLine(rpath);
                    Console.WriteLine();
                    path = rpath;
                }
            }

            var dirs = Directory.GetDirectories(path);
            if (dirs != null && dirs.Length > 0) {
                if (path == ".") {
                    foreach (var dir in dirs) {
                        NormalizeTree(dir.Substring(2), regPath);
                    }
                } else {
                    foreach (var dir in dirs) {
                        NormalizeTree(dir, regPath);
                    }
                }
            }
        }
        public static void UnityUnpackProc()
        {
            var files = Directory.GetFiles(".");
            if (files != null && files.Length > 0) {
                if (!Directory.Exists("Unpack0")) { Directory.CreateDirectory("Unpack0"); }
                if (!Directory.Exists("Unpack1")) { Directory.CreateDirectory("Unpack1"); }
                foreach (var file in files) {
                    var info = ZipInfo.LoadInfo(file);
                    if (info != null) {
                        var fname = file.Substring(2);
                        Console.Write(fname);
                        var itemcnt = info.ItemFlat.Where(m => m.Items == null).Count();
                        if (itemcnt == 1 || itemcnt == 3) {
                            ZipItem pitem = null;
                            foreach (var item in info.ItemFlat) {
                                if (item.Name.EndsWith("unitypackage", StringComparison.CurrentCulture)) {
                                    pitem = item; break;
                                }
                            }
                            if (pitem != null && info.Extract(pitem, "Unpack1")) {
                                var idx = fname.IndexOf('_');
                                if (idx > 0) {
                                    File.Move("Unpack1/" + pitem.Name, "Unpack1/" + fname.Substring(0, idx + 1) + pitem.Name);
                                    File.Move(fname, "Unpack0/" + fname);
                                }
                                Console.WriteLine(" OK");
                            } else {
                                Console.WriteLine(" Not Found");
                            }
                        } else {
                            Console.WriteLine(" NG");
                        }
                    }
                }
            }
        }
        public static void UnityIdsProc()
        {
            int idx; string aid; var aids = new List<string>();

            var files = Directory.GetFiles(".");
            if (files != null && files.Length > 0) {
                foreach (var fname in files) {
                    idx = fname.IndexOf('_');
                    if (idx > 0) {
                        aid = fname.Substring(0, idx);
                        if (aids.Contains(aid)) {
                            Console.WriteLine(fname);
                        } else {
                            aids.Add(aid);
                        }
                    } else {
                        Console.WriteLine(fname);
                    }
                }
            }
        }
        public static void UnityMergeProc(bool replace)
        {
            const string rootPath = "/Volumes/Element/Asset Store/";
            var list = new List<string>();
            TreeDir(".", list);
            foreach (var dir in list) {
                var apath = rootPath + dir;
                if (Directory.Exists(apath)) {
                    if (replace) {
                        Directory.Delete(apath, true);
                        Directory.Move(dir, apath);
                        Console.Write("- ");
                    } else {
                        Console.Write("* ");
                    }
                } else {
                    var adir = apath.Substring(0, apath.LastIndexOf('/'));
                    if (!Directory.Exists(adir)) {
                        Directory.CreateDirectory(adir);
                    }
                    Directory.Move(dir, apath);
                    Console.Write("+ ");
                }
                Console.WriteLine(dir);
            }
        }
        public static async Task UnityStoreProc(string version, string session)
        {
            var loader = new UnityLoader();
            loader.Version = version;
            loader.Session = session;

            var files = Directory.GetFiles(".");
            if (files != null && files.Length > 0) {
                foreach (var file in files) {
                    var fname = file.Substring(2);
                    if (!fname.StartsWith(".") && !await loader.GetAssert(fname)) {
                        break;
                    }
                }
            }
        }
        public static async Task AmazonProc()
        {
            Console.WriteLine();
            Console.WriteLine("Start AmazonProc");

            var shopList = new List<string> { "ANP7EU93CU7I6", "A1PRVM9DH96P7T", "ASITWL0F9M2V7", "A307EUBCD7NAR1", "A2XW3KVE6X6WJ2", "AED21HEND7FER",
                //"A1ZBDTF55Y6QTM", "AN53RVHJMQB88", "A28V5TPDD5X2QZ", "A2R7BFKFCFSF87", "A28V5TPDD5X2QZ", "A1KF9O7TC4S35C"
            };



            var jset = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
            var loader = new AmazonLoader();

            var tasks = new List<Task>();

            foreach (var shop in shopList) {
                //tasks.Add(loader.LoadPages(shop));
                var list = await loader.LoadPages(shop);
                var json = JsonConvert.SerializeObject(list, Formatting.Indented, jset);
                //File.WriteAllText("cgsoso.json", json);
                //Console.WriteLine(json);
            }

            //Task.WaitAll(tasks.ToArray());
        }
    }
}
