using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.UI;

namespace HomeControl
{
    public class ControlSystem : CrestronControlSystem
    {
        string ServerIP = "";
        IROutputPort IR1;
        Xpanel Xpan;
        
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);                
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                Thread StartThread = new Thread(systemStart, null, Thread.eThreadStartOptions.Running);
                StartThread.Priority = Thread.eThreadPriority.HighPriority;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        object systemStart(object userObject)
        {
            while (ServerIP.Length < 7)
            {
                ServerIP = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0);
                CrestronConsole.PrintLine("ServerIP: Retry Until LAN Ready: {0}", ServerIP);
                Thread.Sleep(1000);
            }
            if (this.SupportsIROut)
            {
                IR1.LoadIRDriver("");
                IR1.Register();
            }

            Xpan = new Xpanel(0x03, this);
            Xpan.SigChange += new SigEventHandler(MyXPanSigChangeHandler);
            Xpan.Register();
            return null;            
        }

        void MyXPanSigChangeHandler(BasicTriList currentDevice, SigEventArgs args)
        {
            uint button; // join of button press from panel
            bool push;   // button push (true) or release (false)

            if (args.Sig.Type == eSigType.Bool)  // digitals only
            {
                button = args.Sig.Number;
                push = args.Sig.BoolValue;  // true is a push, false is a release

                if ((button >= 1) && (button <= 100)) // Page buttons
                {
                    Buttons((int)button, push);
                }
            }        
        }

        void Buttons(int button, bool push)
        {
            if (push)
            {
                switch (button)
                {
                    case 1:
                        {
                            IR1.GetStandardCmdFromIRCmd("On");
                            IR1.SendSerialData("1");
                            
                            break;
                        }

                }
            }
        }

        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }
        
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
}