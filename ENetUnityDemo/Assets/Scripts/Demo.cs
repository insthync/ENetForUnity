using UnityEngine;
using System;
using System.Threading;

public class Demo : MonoBehaviour {

    private void Awake()
    {
        ENet.Library.Initialize();
    }

    private void Start()
    {
        Thread server = new Thread(Server); server.Start();
        Thread.Sleep(250);
        Thread client = new Thread(Client); client.Start();

        PacketManipulationDemo();

        server.Join();
        client.Join();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        ENet.Library.Deinitialize();
    }

    static void Server()
    {
        using (ENet.Host host = new ENet.Host())
        {
            Console.WriteLine("Initializing server...");

            host.InitializeServer(5000, 1);
            ENet.Peer peer = new ENet.Peer();

            while (true)
            {
                ENet.Event @event;

                if (host.Service(15000, out @event))
                {
                    do
                    {
                        switch (@event.Type)
                        {
                            case ENet.EventType.Connect:
                                peer = @event.Peer;
                                // If you are using ENet 1.3.4 or newer, the following two methods will work:
                                //peer.SetPingInterval(1000);
                                //peer.SetTimeouts(8, 5000, 60000);
                                Console.WriteLine("Connected to client at IP/port {0}.", peer.GetRemoteAddress());
                                for (int i = 0; i < 200; i++)
                                {
                                    ENet.Packet packet = new ENet.Packet();
                                    packet.Initialize(new byte[] { 0, 0 }, 0, 2, ENet.PacketFlags.Reliable);
                                    packet.SetUserData(i);
                                    packet.SetUserData("Test", i * i);
                                    packet.Freed += p =>
                                    {
                                        Console.WriteLine("Initial packet freed (channel {0}, square of channel {1})",
                                            p.GetUserData(),
                                            p.GetUserData("Test"));
                                    };
                                    peer.Send((byte)i, packet);
                                }
                                break;

                            case ENet.EventType.Receive:
                                byte[] data = @event.Packet.GetBytes();
                                ushort value = BitConverter.ToUInt16(data, 0);
                                if (value % 1000 == 1) { Console.WriteLine("  Server: Ch={0} Recv={1}", @event.ChannelID, value); }
                                value++; peer.Send(@event.ChannelID, BitConverter.GetBytes(value), ENet.PacketFlags.Reliable);
                                @event.Packet.Dispose();
                                break;
                        }
                    }
                    while (host.CheckEvents(out @event));
                }
            }
        }
    }

    static void Client()
    {
        using (ENet.Host host = new ENet.Host())
        {
            Console.WriteLine("Initializing client...");
            host.Initialize(null, 1);

            ENet.Peer peer = host.Connect("127.0.0.1", 5000, 1234, 200);
            while (true)
            {
                ENet.Event @event;

                if (host.Service(15000, out @event))
                {
                    do
                    {
                        switch (@event.Type)
                        {
                            case ENet.EventType.Connect:
                                Console.WriteLine("Connected to server at IP/port {0}.", peer.GetRemoteAddress());
                                break;

                            case ENet.EventType.Receive:
                                byte[] data = @event.Packet.GetBytes();
                                ushort value = BitConverter.ToUInt16(data, 0);
                                if (value % 1000 == 0) { Console.WriteLine("  Client: Ch={0} Recv={1}", @event.ChannelID, value); }
                                value++; peer.Send(@event.ChannelID, BitConverter.GetBytes(value), ENet.PacketFlags.Reliable);
                                @event.Packet.Dispose();
                                break;

                            default:
                                Console.WriteLine(@event.Type);
                                break;
                        }
                    }
                    while (host.CheckEvents(out @event));
                }
            }
        }
    }

    static void PacketManipulationDemo()
    {
        Console.WriteLine("Packet manipulation test/demo... should print 3 2 1...");
        using (ENet.Packet packet = new ENet.Packet())
        {
            packet.Initialize(new byte[0]);
            packet.Add((byte)1);
            packet.Insert(0, (byte)3);
            packet.Insert(1, (byte)2);
            packet.Insert(packet.IndexOf((byte)3), 4);
            packet.Remove(1);
            packet.RemoveAt(0);
            if (packet.Contains(3)) { packet.Add((byte)1); }
            if (packet.Contains(4)) { packet.Add((byte)5); }

            byte[] bytes = packet.GetBytes();
            for (int i = 0; i < bytes.Length; i++)
            {
                Console.WriteLine(bytes[i]);
            }
        }
    }
}
