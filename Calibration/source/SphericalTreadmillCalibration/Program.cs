using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SphericalTreadmillCalibration
{
    class Program
    {
        public static MqttClient client;
        public class Data
        {
            public float pitch;
            public float roll;
            public float yaw;
        }
        public static List<Data> msgBuffer;

        static void Main(string[] args)
        {
            // Pre-amble
            Console.WriteLine("Please ensure the scaling is set to 1 for calibration\n");
            msgBuffer = new List<Data>();

            // Device name
            string deviceName = QuestionString("device name? (e.g. sphericalTreadmill):");

            // Connect to broker.
            #region Connect to Broker.
            IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
            Console.WriteLine(String.Format("Connecting to: 127.0.0.1:1883"));
            client = new MqttClient(ipAdress, 1883, false, null, null, MqttSslProtocols.None);
            try
            {
                byte msg = client.Connect(Guid.NewGuid().ToString());
                MqttMsgConnack connack = new MqttMsgConnack();
                connack.GetBytes(msg);
                if (!client.IsConnected) Exit("Failed to connect");
            }
            catch { Exit("Failed to connect"); }
            #endregion

            // Subscribe to topic.
            #region Subscribe to topic
            client.MqttMsgPublishReceived += receiveMessage;

            string topic = string.Format("{0}/Data", deviceName);
            Console.WriteLine(String.Format("Subscribing to topic: {0}\n", topic));
            try { client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE }); }
            catch { Exit("Failed to subscribe to topic"); }
            #endregion

            // Ask radius.
            float radius = float.Parse(QuestionString("Radius of ball in VR World units? (spherical: 1.976):"));
            float circumference = 2f * (float)Math.PI * radius;
            float arcLength = circumference / 2;

            // Request axis to calibrate.
            ConsoleKeyInfo axisString = new ConsoleKeyInfo();
            while (axisString.Key != ConsoleKey.E)
            {
                Console.Write("Calibrate: (P)itch, (R)oll, (Y)aw (E)xit\n");
                axisString = Console.ReadKey();
                int axis = 0;
                switch (axisString.Key)
                {
                    case ConsoleKey.P:
                        {
                            axis = 1;
                            break;
                        }
                    case ConsoleKey.R:
                        {
                            axis = 2;
                            break;
                        }
                    case ConsoleKey.Y:
                        {
                            axis = 3;
                            break;
                        }
                    case ConsoleKey.E:
                        {
                            continue;
                        }
                    default: { Exit("Not recognized"); break; }
                }

                // Ask average.
                int averageNum = int.Parse(QuestionString("Average over how many measurements?:"));

                // Start measurement.
                string[] axisNames = { "Pitch", "Roll", "Yaw" };
                float[] measurements = new float[averageNum];

                for (int i = 0; i < averageNum; i++)
                {
                    // clean buffer.
                    msgBuffer.Clear();
                    Console.WriteLine(string.Format(
                        "Rotate treadmill +180 degrees in {0} direction (Left-hand rule)\nand press any key.",
                        axisNames[axis - 1]));
                    Console.ReadKey();

                    // collect data
                    Data result = new Data();
                    lock (msgBuffer)
                    {
                        result.pitch = msgBuffer.Sum(x => x.pitch);
                        result.roll = msgBuffer.Sum(x => x.roll);
                        result.yaw = msgBuffer.Sum(x => x.yaw);
                    }

                    // read buffer.
                    switch (axis)
                    {
                        case 1:
                            measurements[i] = result.pitch;
                            break;
                        case 2:
                            measurements[i] = result.roll;
                            break;
                        case 3:
                            measurements[i] = result.yaw;
                            break;

                    }

                    // Output.
                    Console.WriteLine(string.Format("\nPitch: {0:0.00},\tRoll: {1:0.00},\tYaw: {2:0.00}",
                        result.pitch, result.roll, result.yaw));
                    // Yaw: degrees.
                    if (axis == 3)
                    {
                        Console.WriteLine(string.Format("Calibration value: {0:0.0000000}",
                        Math.Abs(180 / measurements[i])));
                    }
                    // Pitch/Roll: arc length.
                    else
                    {
                        Console.WriteLine(string.Format("Calibration value: {0:0.0000000}",
                         Math.Abs(arcLength / measurements[i])));
                    }

                }
                // Averaged result.
                Console.WriteLine("\nResult:");

                // Yaw: degrees.
                if (axis == 3)
                {
                    Console.WriteLine(string.Format("Averaged calibration value: {0:0.0000000}",
                    Math.Abs(180 / measurements.Average())));
                }
                // Pitch/Roll: arc length.
                else
                {
                    Console.WriteLine(string.Format("Averaged calibration value: {0:0.0000000}",
                     Math.Abs(arcLength / measurements.Average())));
                }
            }
            Exit("\nDone!");
        }

        static void receiveMessage(object sender, MqttMsgPublishEventArgs e)
        {
            Data msg = JsonConvert.DeserializeObject<Data>(Encoding.UTF8.GetString(e.Message));
            lock (msgBuffer)
            {
                msgBuffer.Add(msg);
            }
        }

        public static string QuestionString(string msg)
        {
            Console.WriteLine(msg);
            string reply = Console.ReadLine();
            return reply;
        }

        public static void Exit(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press any key to exit..");
            Console.ReadKey();
            System.Environment.Exit(1);
        }
    }
}
