using System;


namespace DAN_XXXVIII_AndrejaKolesar
{
    class Program
    {
        static void Main(string[] args)
        {
            TruckShipmentSimulator simulator = new TruckShipmentSimulator();
            simulator.DoShipment();
            Console.ReadLine();
        }
    }
}
