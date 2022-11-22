﻿using AdvancedSharpAdbClient;
using Bespoke.Osc;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Quest2_VRC
{
    static class VRCProgram
    {
        static readonly IPAddress IP = IPAddress.Loopback;
        static readonly int Port = 9000;
        static readonly IPEndPoint VRChat = new IPEndPoint(IP, Port); 
        static AdvancedAdbClient client;
        static DeviceData device;

        public static async void Run()
        {
            
            client = new AdvancedAdbClient();
            client.Connect("127.0.0.1:62001");
            device = client.GetDevices().FirstOrDefault();
            if (device == null)
            {
                Console.WriteLine("No devices found, please restart app and try again");
                Console.ReadLine();
                return;
            }
            if (device is not null)
            {
                Console.WriteLine("Selecting device: {0} with name {1}", device.Model, device.Name);

            }
            if (device.Name != "hollywood" && device.Name != "vr_monterey" && device.Name != "monterey" && device.Name != "seacliff")
            {
                Console.WriteLine("Oculus/Meta device is not detected, please disconnect all non Oculus/Meta devices and clouse all emulators on PC, restart app and try again");
                Console.ReadLine();
                return;

            }
            Random rnd = new Random();
            int Uport = rnd.Next(1, 9999);
            Console.WriteLine("UDP port is {0}", Uport);


            await questwd(Uport);

        }

        public static async Task questwd(int Uport)
        {
            // Create a bogus port for the client
            OscPacket.UdpClient = new UdpClient(Uport);

            while (true)
            {
                try
                {
                    int Hbatlevelint = 0;
                    int Rbatlevelint = 0;
                    int Lbatlevelint = 0;
                    bool LowHMDBat = false;

                    ConsoleOutputReceiver Hbat_receiver = new ConsoleOutputReceiver();
                    client.ExecuteRemoteCommand("dumpsys CompanionService | grep Battery", device, Hbat_receiver);
                    ConsoleOutputReceiver Rbat_receiver = new ConsoleOutputReceiver();
                    client.ExecuteRemoteCommand("dumpsys OVRRemoteService | grep Right", device, Rbat_receiver);
                    ConsoleOutputReceiver Lbat_receiver = new ConsoleOutputReceiver();
                    client.ExecuteRemoteCommand("dumpsys OVRRemoteService | grep Left", device, Lbat_receiver);

                    //Console.WriteLine($"String with number: {receiver}");  //Debug output
                    var Hbat_match = Regex.Match(Hbat_receiver.ToString(), @"\d+", RegexOptions.RightToLeft);
                    var Rbat_match = Regex.Match(Rbat_receiver.ToString(), @"\d+", RegexOptions.RightToLeft);
                    var Lbat_match = Regex.Match(Lbat_receiver.ToString(), @"\d+", RegexOptions.RightToLeft);

                    Hbatlevelint = int.Parse(Hbat_match.Value);
                    Rbatlevelint = int.Parse(Rbat_match.Value);
                    Lbatlevelint = int.Parse(Lbat_match.Value);
                    float Hbatlevelf = Hbatlevelint;
                    float Rbatlevelf = Rbatlevelint;
                    float Lbatlevelf = Lbatlevelint;

                    if (Hbatlevelf < 15)
                    {
                        LowHMDBat = true;
                        //SoundPlayer playSound = new SoundPlayer(Properties.Resources.HMDloworbelow15);
                        //playSound.Play();

                    }
                    if (Rbatlevelf < 15)
                    {
                        LogToConsole("Right controller is discharged or disabled");
                        //SoundPlayer playSound = new SoundPlayer(Properties.Resources.Rcrtloworbelow15);
                        //playSound.Play();

                    }
                    if (Lbatlevelf < 15)
                    {
                        LogToConsole("Left controller is discharged or disabled");
                        //SoundPlayer playSound = new SoundPlayer(Properties.Resources.Lctrloworbelow15);
                        //playSound.Play();

                    }

                    VRChatMessage Msg1 = new VRChatMessage("HMDBat", Hbatlevelf);
                    VRChatMessage Msg2 = new VRChatMessage("ControllerBatL", Lbatlevelf);
                    VRChatMessage Msg3 = new VRChatMessage("ControllerBatR", Rbatlevelf);
                    VRChatMessage Msg4 = new VRChatMessage("LowHMDBat", LowHMDBat);

                    SendPacket(Msg1, Msg2, Msg3, Msg4);

                    LogToConsole("Sending HMD status", Msg1, Msg2, Msg3, Msg4);

                    Thread.Sleep(300);

                }
                catch
                {
                    LogToConsole("Error!");

                }


            }

            static void SendPacket(params VRChatMessage[] Params)
            {
                foreach (var Param in Params)
                {
                    try
                    {
                        // Check if there's a valid target
                        if (Param.Parameter == null || string.IsNullOrEmpty(Param.Parameter))
                            throw new Exception("Parameter target not set!");

                        // Check if Parameter is not null and of one of the following supported types
                        if (Param.Data != null &&
                            Param.Data.GetType() != typeof(int) &&
                            Param.Data.GetType() != typeof(float) &&
                            Param.Data.GetType() != typeof(bool))
                            throw new Exception(String.Format("Param of type {0} is not supported by VRChat!", Param.Data.GetType()));

                        // Create a bundle that contains the target address and port (VRChat works on localhost:9000)
                        OscBundle VRBundle = new OscBundle(VRChat);

                        // Create the message, and append the parameter to it
                        OscMessage Message = new OscMessage(VRChat, String.Format("/avatar/parameters/{0}", Param.Parameter));
                        Message.Append(Param.Data);

                        // Append the message to the bundle
                        VRBundle.Append(Message);

                        // Send the bundle to the target address and port
                        VRBundle.Send(VRChat);

                    }
                    catch (Exception ex)
                    {
                        LogToConsole(ex.ToString());
                    }
                }
            }

            static void LogToConsole(string Message, params VRChatMessage[] Parameters)
            {
                StringBuilder MessageBuilder = new StringBuilder();

                MessageBuilder.Append(String.Format("{0} - {1}", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.ffff"), Message));

                if (Parameters.Length > 0)
                {
                    MessageBuilder.Append(" (");

                    var LastParam = Parameters[Parameters.Length - 1];
                    foreach (var Parameter in Parameters)
                    {
                        MessageBuilder.Append(String.Format("{0} of type {1}", Parameter.Data, Parameter.Data.GetType()));

                        if (Parameter != LastParam)
                            MessageBuilder.Append(", ");
                    }

                    MessageBuilder.Append(")");
                }

                Console.WriteLine(MessageBuilder.ToString());
            }
        }

        public class VRChatMessage
        {
            // The target of the data
            public string? Parameter { get; }

            // The data itself
            public object? Data { get; }

            public VRChatMessage(string A, object B)
            {
                Parameter = A;
                Data = B;
            }

        }
    }
}