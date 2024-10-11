#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.System;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.WebUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.Core;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
#endregion

public class SPC_VariablesSimulator : BaseNetLogic
{
    private PeriodicTask MyTask;
    private int iCounter;
    private double dCounter;
    private bool bRun;

    public override void Start()
    {
        MyTask = new PeriodicTask(Simulation, 250, LogicObject);
        iCounter = 0;
        dCounter = 0;
        MyTask.Start();
    }

    public void Simulation()
    {
        bRun = LogicObject.GetVariable("bRunSimulation").Value;
        if (bRun == true)
        {
            if (iCounter <= 50)
            {
                iCounter = iCounter + 1;
            }
            else
            {
                iCounter = 0;
            }
            dCounter = dCounter + 0.05;

            // Generate accelerating and decelerating square waveform
            int accelDecelValue = (iCounter < 50) ? (iCounter * 2) : ((100 - iCounter) * 2);
            LogicObject.GetVariable("iRamp").Value = accelDecelValue;

            // Reduce amplitude of sine and cosine signals by 50%
            LogicObject.GetVariable("iSin").Value = Math.Sin(dCounter) * 50;
            LogicObject.GetVariable("iCos").Value = Math.Cos(dCounter) * 25;
        }
    }

    public override void Stop()
    {
        if (MyTask != null)
        {
            MyTask.Dispose();
            MyTask = null;
        }
    }
}
