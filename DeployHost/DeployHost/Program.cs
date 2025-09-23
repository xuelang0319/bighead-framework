using Bighead.Deploy;

string prefix = args.Length > 0 ? args[0] : "http://+:8081/";
string root   = args.Length > 1 ? args[1] : @"D:\FileRoot";
string token  = args.Length > 2 ? args[2] : "SECRET_TOKEN";

var host = new DeployHost(prefix, root, token);
host.Start();

Console.WriteLine($"[DeployHost] 启动成功，监听 {prefix}，根目录 {root}");
Console.WriteLine("按回车退出...");
Console.ReadLine();

host.Stop();