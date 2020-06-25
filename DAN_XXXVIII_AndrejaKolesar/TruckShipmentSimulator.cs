using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DAN_XXXVIII_AndrejaKolesar
{
    class TruckShipmentSimulator
    {
        #region fields 
        public Random random = new Random();
        private readonly string fileName = "PotentialRoutes.txt";
        private readonly object locker = new object();
        //list of best routes
        public List<int> bestRoutes = new List<int>();
        //array of threads. Every thread is representing one truck
        public Thread[] trucks = new Thread[10];
        //this variables are used in TruckWork method
        public int enterCounter;
        public int exitCounter;

        public CountdownEvent countdown = new CountdownEvent(10);
        public EventWaitHandle waitHandle = new AutoResetEvent(false);

        #endregion

        #region DoShipment - creating treads
        /// <summary>
        /// Method for creating and joining threads
        /// </summary>
        public void DoShipment()
        {
            //create tread that generates 1000 random numbers in range [1,5000]
            Thread t1 = new Thread(NumberGenerator)
            {
                Name = "number_generator"
            };
            Thread t2 = new Thread(ManagerJob)
            {
                Name = "manager"
            };
            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            Console.WriteLine("\nTRUCKS LOADING, DRIVING AND UNLOADING: ");
            for (int i = 0; i < 10; i++)
            {
                int br = bestRoutes[i];
                trucks[i] = new Thread(TruckJob)
                {
                    Name = String.Format("Truck_{0}", i + 1)
                };
                trucks[i].Start(bestRoutes[i]);
            }

        }
        #endregion

        #region Finding best routes
        //in this section I'm using AutoResetEvent

        /// <summary>
        /// Generates 1000 numbers in range [1,5000] and logs them into file PotentialRoutes.txt
        /// </summary>
        public void NumberGenerator()
        {
            int n;
            using (StreamWriter sw = File.CreateText(fileName))
            {
                for (int i = 0; i < 5000; i++)
                {
                    n = random.Next(1, 5001);
                    sw.WriteLine(n);
                }
            }
            //allow Manager to do his work
            waitHandle.Set();
        }

        /// <summary>
        /// Manager is choosing the best routes
        /// Best routes are routes that are divisable by 3
        /// </summary>
        public void ManagerJob()
        {
            //manager is waiting 3sec while system generating all posible routes
            waitHandle.WaitOne(3000);
            using (StreamReader sr = File.OpenText(fileName))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    int n = Convert.ToInt32(s);
                    if (n % 3 == 0)
                    {
                        bestRoutes.Add(n);
                    }
                }

            }
            Console.WriteLine("All possible routes are shown in the file {0}", fileName);
            Console.Write("\nMANAGER: \nBest routes are selected and drivers can start with loading.\nList of selected routes: ");
            //take just distinct routes
            bestRoutes = bestRoutes.Distinct().ToList();
            //sort routes and take the best (first 10)
            bestRoutes.Sort();
            //displaying best routes
            for (int i = 0; i < 10; i++)
            {
                Console.Write(bestRoutes[i] + " ");
            }
            Console.WriteLine();
        }
        #endregion

        #region Truck loading and delivering
        //in this section I'm using Countdown event

        /// <summary>
        /// Method that allows two by two threads to go further 
        /// </summary>
        public void AllowTwoByTwoTrucks()
        {
            while (true)
            {
                lock (locker)
                {
                    enterCounter++;
                    if (enterCounter > 2)
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        exitCounter++;
                        break;
                    }
                }
            }
        }

        public int Loading(string name)
        {
            Console.WriteLine("{0} has started loading", name);
            int loadingTime = random.Next(500, 5001);
            Thread.Sleep(loadingTime);
            Console.WriteLine("{0} has finished loading", name);
            return loadingTime;
        }

        public void Unloading(string name, int loadingTime, object route)
        {
            Console.WriteLine("The driver on a truck {0} has just started driving on route {1}. They can expect delivery between 500ms and 5sec.", name, route);
            int deliveryTime = random.Next(500, 5001);
            if (deliveryTime > 3000)
            {
                Thread.Sleep(3000);
                Console.WriteLine("ORDER ON ROUTE {0} CANCELED. {1} did not arrived in 3sec. Truck need 3s to come back to starting point.", route, name, deliveryTime);
            }
            else
            {
                Thread.Sleep(deliveryTime);
                Console.WriteLine("The {0} arrived at its destination {1}. Unloading time was {2}ms.", name, route, Convert.ToInt32(loadingTime / 1.5));
            }
        }


        /// <summary>
        /// This method is representing everything that single truck must do in application (loading, taking route, driving and unloading)
        /// </summary>
        /// <param name="route"></param>
        public void TruckJob(object route)
        {
            //get truck name
            var name = Thread.CurrentThread.Name;
            AllowTwoByTwoTrucks();
            int loadingTime = Loading(name);
            lock (locker)
            {
                exitCounter--;
                countdown.Signal();
                if (exitCounter == 0)
                {
                    //reset enter number
                    enterCounter = 0;

                }
            }
            //start with delivery when all trucks loaded
            countdown.Wait();
            Console.WriteLine("{0} will drive through route {1}", name, route);
            Unloading(name, loadingTime, route);
        }
        #endregion
    }
}
