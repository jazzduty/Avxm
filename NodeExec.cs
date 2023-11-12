/*using System;
using System.IO;
using Jint;

namespace Avxm
{
	public class NodeExec
	{
		public bool JintTest()
		{
			var engine = new Engine().SetValue("log", new Action<object>(Console.WriteLine));

			engine.Execute(@"
      function hello() { 
        log('Hello World');
      };
      
      hello();
    ");
			return true;
		}
	}
}
*/



/*
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Avxm
{
	public class NodeExec
	{
		private class Startup : IStartup
		{
			public void Configure(IApplicationBuilder app)
			{
			}

			public IServiceProvider ConfigureServices(IServiceCollection services)
			{
				services.AddNodeServices();
				return services.BuildServiceProvider();
			}
		}

		public async Task<bool> NodeServiceTest()
		{
			var host = new WebHostBuilder().UseKestrel().UseContentRoot(Directory.GetCurrentDirectory()).UseStartup<Startup>().Build();
			var nodeServices = host.Services.GetRequiredService<INodeServices>();
			//test.js
			//module.exports = function(callback, name) {
			//	var greet = function(name) {
			//		return "Hello " + name;
			//	}
			//	callback(null, greet(name));
			//}
			string rlt = "";
			try {
				rlt = await nodeServices.InvokeAsync<string>("test", "yong");
			} catch (Exception ex) {
				rlt = ex.ToString();
			}
			Console.WriteLine(rlt);
			return true;
		}

	}
}
*/
  