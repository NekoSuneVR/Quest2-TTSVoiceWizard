﻿using System;
using System.IO;

namespace Quest2_VRC
{
    public class Check_Vars
    {
        public static void CheckVars()
        {
            bool exists = File.Exists("vars.txt");
            if (!exists)
            {
                Console.WriteLine("vars.txt does not exist, creating...");
                string[] lines =
                {
                    "HMDBat = HMDBat", "ControllerBatL = leftControllerBattery", "ControllerBatR = rightControllerBattery", "Receive_addr = /avatar/parameters/Eyes mode", "Receive_addr_test = /avatar/parameters/Eyes_mode", "SendPort = 4026", "ReceivePort = 9001"  // Default settings for my avatar
                };
                File.WriteAllLines("vars.txt", lines);
            }

            else
            {
                Console.WriteLine("vars.txt exists");
            }
        }
    }
}
