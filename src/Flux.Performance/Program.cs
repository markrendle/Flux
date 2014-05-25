using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flux.Performance
{
    using System.Runtime.Remoting.Channels;
    using System.Threading;

    class Program
    {
        private static Task CompletedTask;
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            CompletedTask = tcs.Task;
            var server = new FluxServer(3589);
            Console.WriteLine("Server created...");
            using (server)
            {
                server.Start(App);
                Console.WriteLine("FluxServer listening on port 3589...");
                Console.WriteLine("Press Escape to stop.");
                Console.ReadKey();
                server.Stop();
            }
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            ExitEvent.Set();
        }

        static Task App(IDictionary<string, object> env)
        {
            env[OwinKeys.ResponseStatusCode] = 200;
            env[OwinKeys.ResponseReasonPhrase] = "OK";
            return CompletedTask;
        }
    }
}
