using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LyncZMachine {
    using log4net.Config;

    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            try {
                XmlConfigurator.Configure();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            if (args.FirstOrDefault() == "-debug") {
                var svc = new ZMachineService();
                svc.RunDebug(args);
            } else if (args.FirstOrDefault() == "-config") {
                var setupForm = new SetupForm();
                setupForm.ShowDialog();
            } else {
                var servicesToRun = new ServiceBase[] {
                    new ZMachineService()
                };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
