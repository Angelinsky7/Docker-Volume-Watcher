using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.test {
    class Program {
        static void Main(string[] args) {

            Console.WriteLine("Start");

            DockerMonitor monitor = null;
          
            try {
                monitor = new DockerMonitor(null, 500, true, 1);
                monitor.Start();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();

            if(monitor != null) {
                monitor.Stop();
                monitor = null;
            }

            Console.WriteLine("End");

        }

    }
}
