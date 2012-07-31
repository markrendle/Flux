namespace Flux
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Fix;

    class Program
    {
        private static Fixer _fixer;
        private static bool _stop;

        public static void Main(string[] args)
        {
            int port = 0;
            if (args.Length == 0)
            {
                port = 3333;
            }
            else if (args.Length == 1)
            {
                if (!int.TryParse(args[0], out port))
                {
                    Console.Error.WriteLine("Usage: flux.exe [port]");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.Error.WriteLine("Usage: flux.exe [port]");
                Environment.Exit(1);
            }

            using (var server = new Server(port))
            {
                _fixer = new Fixer(server.Start, server.Stop);

                if (Directory.EnumerateFiles(Environment.CurrentDirectory, "*.dll").Any())
                {
                    FixUpAssemblies(_fixer, "*.dll", Environment.CurrentDirectory);
                }
                else
                {
                    var bin = Path.Combine(Environment.CurrentDirectory, "bin");
                    if (Directory.Exists(bin))
                    {
                        FixUpAssemblies(_fixer, "*.dll", bin);
                    }
                    else
                    {
                        Console.Error.WriteLine("No application found in {0} or {0}\\bin", Environment.CurrentDirectory);
                        Environment.Exit(1);
                    }
                }

                Console.CancelKeyPress += ConsoleOnCancelKeyPress;
                Console.TreatControlCAsInput = false;
                _fixer.Start();
                Console.WriteLine("Flux {0}: listening on port {1}. Press CTRL-C to stop.", Assembly.GetExecutingAssembly().GetName().Version, port);
                while (!_stop)
                {
                    Console.ReadKey();
                }
            }
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            if (_fixer != null)
            {
                _fixer.Stop();
            }
            _stop = true;
            consoleCancelEventArgs.Cancel = false;
        }

        private static void FixUpAssemblies(Fixer fixer, string searchPattern, string directory)
        {
            using (var catalog = new AggregateCatalog())
            {
                foreach (var file in Directory.GetFiles(directory, searchPattern))
                {
                    try
                    {
                        var assembly = Assembly.LoadFile(file);
                        if (assembly.FullName.StartsWith("Microsoft.") || assembly.FullName.StartsWith("System."))
                        {
                            continue;
                        }
                        var assemblyCatalog = new AssemblyCatalog(assembly);
                        catalog.Catalogs.Add(assemblyCatalog);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);
                    }
                }
                var container = new CompositionContainer(catalog);
                container.ComposeParts(fixer);
            }
        }
    }
}