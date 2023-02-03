// This is a console application that can be used to test an ASCOM driver

// Remove the "#define UseChooser" line to bypass the code that uses the chooser to select the driver and replace it with code that accesses the driver directly via its ProgId.
#define UseChooser

using ASCOM.DeviceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if UseChooser
            // Choose the device
            string id = ASCOM.DriverAccess.CoverCalibrator.Choose("");

            // Exit if no device was selected
            if (string.IsNullOrEmpty(id))
                return;

            // Create this device
            ASCOM.DriverAccess.CoverCalibrator device = new ASCOM.DriverAccess.CoverCalibrator(id);
#else
            // Create the driver class directly.
            ASCOM.DriverAccess.CoverCalibrator device = new ASCOM.DriverAccess.CoverCalibrator("ASCOM.SpikeAFlat.CoverCalibrator");
#endif

            // Connect to the device
            device.Connected = true;

            // Now exercise some calls that are common to all drivers.
            Console.WriteLine($"Name: {device.Name}");
            Console.WriteLine($"Description: {device.Description}");
            Console.WriteLine($"DriverInfo: {device.DriverInfo}");
            Console.WriteLine($"DriverVersion: {device.DriverVersion}");
            Console.WriteLine($"InterfaceVersion: {device.InterfaceVersion}");
            try
            {


                Console.WriteLine($"Brightness: {device.Brightness}");
                device.CalibratorOn(20);
                CalibratorStatus cs = device.CalibratorState;
                while (cs != DeviceInterface.CalibratorStatus.Ready)
                {
                    Console.WriteLine(cs);
                    System.Threading.Thread.Sleep(200);
                    cs = device.CalibratorState;
                }
                Console.WriteLine($"Brightness: {device.Brightness}");
                device.CalibratorOn(1000);
                cs = device.CalibratorState;
                while (cs != DeviceInterface.CalibratorStatus.Ready)
                {
                    Console.WriteLine(cs);
                    System.Threading.Thread.Sleep(200);
                    cs = device.CalibratorState;
                }
                Console.WriteLine($"Brightness: {device.Brightness}");
            }
            catch (Exception ex)
            {
                device.Connected = false;
                throw ex;

            }
            finally
            {
                device.Connected = false;
            }
            // Disconnect from the device

            Console.WriteLine("Press Enter to finish");
            Console.ReadLine();
        }
    }
}
