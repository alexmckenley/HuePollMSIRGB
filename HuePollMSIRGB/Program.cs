using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static System.Linq.Enumerable;
using MSIRGB;

namespace HuePollMSI
{
    class Program
    {
        private Lighting _lighting;
        static void Main(string[] args)
        {
            new Program(args);
        }

        public Program(string[] args)
        {
            byte r = 255;
            byte b = 255;
            byte g = 255;
            try
            {
                r = getByteVal(args[0]);
                g = getByteVal(args[1]);
                b = getByteVal(args[2]);
            }
            catch (IndexOutOfRangeException) {
                Console.WriteLine("An argument was missing");
            }

            CheckForRunningMSIApps();
            TryInitializeDLL();
            ApplyConfig(r, g, b, false);
        }

        public byte getByteVal(
            string str)
        {
            int num;
            bool test = int.TryParse(str, out num);
            if (test == false)
            {
                throw new Exception("Please enter a numeric argument, got: " + str);
            }
            return Decimal.ToByte(255 - num);
        }

        public void ApplyConfig(
            byte r,
            byte g,
            byte b,
            bool breathingEnabled)
        {
            _lighting.BatchBegin();

            r /= 0x11; // Colour must be passed with 12-bit depth
            g /= 0x11;
            b /= 0x11;

            foreach (byte index in Range(1, 8))
            {
                _lighting.SetColour(index, r, g, b);
            }

            _lighting.SetStepDuration(511);

            // Since breathing mode can't be enabled if flashing was previously enabled
            // we need to set the new flashing speed setting before trying to change breathing mode state
            _lighting.SetFlashingSpeed((Lighting.FlashingSpeed)Lighting.FlashingSpeed.Disabled);

            _lighting.SetBreathingModeEnabled(breathingEnabled);

            _lighting.BatchEnd();
        }
        private void TryInitializeDLL()
        {
            try
            {
                _lighting = new Lighting(true);
            }
            catch (Lighting.Exception exc)
            {
                if (exc.GetErrorCode() == Lighting.ErrorCode.MotherboardModelNotSupported)
                {
                    Console.WriteLine("Your motherboard is not on the list of supported motherboards. " +
                                          "Attempting to use this program may cause irreversible damage to your hardware and/or personal data. " +
                                          "Continuing anyway...");
                }

                else if (exc.GetErrorCode() == Lighting.ErrorCode.MotherboardVendorNotSupported)
                {
                    Console.WriteLine("Your motherboard's vendor was not detected to be MSI. MSIRGB only supports MSI motherboards. " +
                        "To avoid damage to your hardware, MSIRGB will shutdown. " + Environment.NewLine + Environment.NewLine +
                        "If your motherboard's vendor is MSI, " + "" +
                        "please report this problem on the issue tracker at: https://github.com/ixjf/MSIRGB"
                        );
                }
                else if (exc.GetErrorCode() == Lighting.ErrorCode.DriverLoadFailed)
                {
                    Console.WriteLine("Failed to load driver. This could be either due to some program interfering with MSIRGB's driver, " +
                                    "or it could be a bug. Please report this on the issue tracker at: https://github.com/ixjf/MSIRGB"
                                    );
                }
                else if (exc.GetErrorCode() == Lighting.ErrorCode.LoadFailed)
                {
                    Console.WriteLine("Failed to load. Please report this on the issue tracker at: https://github.com/ixjf/MSIRGB"
                                    );
                }
            }
        }

        private void CheckForRunningMSIApps()
        {
            string assemblyTitle = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false))?.Title;

            Process[] runningMSIProcesses = Process.GetProcessesByName("LEDKeeper");

            if (runningMSIProcesses.Count() > 0)
            {
               Console.WriteLine("MSIRGB detected that an MSI application that could potentially interfere is running. " +
                    "This application is likely either MSI Mystic Light or MSI Gaming App. " + Environment.NewLine + Environment.NewLine +
                    "In order to start MSIRGB, you must stop this application." + Environment.NewLine + Environment.NewLine +
                    "Please make sure that neither of these applications are running at any time simultaneously with MSIRGB. " + "" +
                    "If MSIRGB is set to autostart a script on Windows startup, please make sure neither of these MSI applications are autostarted as well.");
            }
        }
    }
}
