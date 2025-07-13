using Landfall.Haste;
using Landfall.Modding;
using UnityEngine;
using UnityEngine.Localization;
using Zorro.Settings;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zorro.Core.CLI;
using TMPro;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using Zorro.Core;
using UnityEngine.Events;
using Unity.Collections;
using System.Text;


namespace ZoeToZoe;

[LandfallPlugin]
public class ClientMessageManager
{

    static byte messageID;

    static List<(ulong, Message)> RecievedMessages = new List<(ulong, Message)>();

    static Dictionary<string, MessageDelegate> messages = new();

    static FixedString64Bytes terminator;

    static List<Message> messageList = new List<Message>();

    static ClientMessageManager()
    {
        byte b = 255;
        for (int i = 0; i < 61; i++)
        {

            terminator.Add(in b);
        }


        On.Landfall.Haste.NGOPlayer.Update += (orig, self) =>
        {
            orig(self);

            if (!self.IsOwner)
                return;

            foreach (var msg in messageList)
            {
                self.Items.Add(msg.name);
                foreach (var Param in msg.ParamString)
                {
                    self.Items.Add(Param);
                }
                self.Items.Add(terminator);
            }
        };

        On.Landfall.Haste.NGOPlayer.NetworkTick += (orig, self) =>
        {
            orig(self);

            //Loop through all ngo players
            //look at items
            //call delegates
            //clear messagelist
            // Debug.LogWarning("Alloo Network Tick Alloo" + HasteNetworking.Player.Items.Count);
            foreach (var msg in messageList)
            {
                msg.Ticks++;
            }
            foreach (var msg in RecievedMessages)
            {
                msg.Item2.Ticks++;

                // Debug.LogWarning($"Received Message from {msg.Item1} for {msg.Item2.Ticks} ticks with ID {msg.Item2.id}");
            }

            foreach (NGOPlayer player in HasteNetworking.Players)
            {
                for (int i = 0; i < player.Items.Count; i++)
                {
                    byte[] bytes = player.Items[i].AsFixedList().ToArray();
                    string name = Encoding.UTF8.GetString(bytes[0..(bytes.Length - 3)]);
                    List<byte[]> paramList = new List<byte[]>();
                    if (messages.ContainsKey(name) && (bytes[bytes.Length - 1] == NetworkManager.Singleton.LocalClientId || bytes[bytes.Length - 1] == 255))
                    {
                        while (player.Items[i + 1].ToString() != terminator)
                        {
                            i++;
                            paramList.Add(player.Items[i].AsFixedList().ToArray());
                        }
                        i++;
                        Message asMsg = new(name, new FixedString64Bytes[1], bytes[bytes.Length-3], bytes[bytes.Length - 2]);
                        if (!RecievedMessages.Exists(t => t.Item1 == player.OwnerClientId && t.Item2.id == bytes[bytes.Length - 2]))
                        {
                            messages[name].Invoke(player.OwnerClientId, paramList.ToArray());
                            RecievedMessages.Add((player.OwnerClientId, asMsg));
                            Debug.LogWarning($"Adding Message to List -- clientID: {player.OwnerClientId} {asMsg}");
                        }
                    }
                }
            }

            messageList.RemoveAll(msg => msg.Ticks > msg.Life);
            RecievedMessages.RemoveAll(msg => msg.Item2.Ticks > msg.Item2.Life*1.5f);
        };


    }

    public static void SendMessage(string name, byte[][] Params, int life = 5, byte targetId = 255)
    {
        if (Encoding.UTF8.GetByteCount(name) > 58)
        {
            throw new Exception("Message name byte size is more than 58 byte limit on names! Byte size: " + Encoding.UTF8.GetByteCount(name));
        }

        if (NetworkManager.Singleton == null)
        {
            throw new Exception("Network Manager has not been initialized! Cannot send message: " + name);
        }
        
        FixedString64Bytes msgName = name;
        msgName.Add((byte) life);
        msgName.Add(in messageID);
        msgName.Add(in targetId);

        FixedString64Bytes[] ParamStrings = new FixedString64Bytes[Params.Length];
        for (int i = 0; i < Params.Length; i++)
        {
            if (Params[i].Length > 61)
            {
                throw new Exception($"Parameter {i} in message {name} exceeds 61 byte limit! Byte size: {Params[i].Length}");
            }
            for (int j = 0; j < Params[i].Length; j++)
                ParamStrings[i].Add(Params[i][j]);
        }

        messageList.Add(new Message(msgName, ParamStrings, life));
    }

    public static void RegisterMessageDelegate(string name, MessageDelegate del)
    {
        if (messages.ContainsKey(name))
        {
            throw new Exception("Message delegate already exists with name: " + name);
        }
        if (Encoding.UTF8.GetByteCount(name) > 58)
        {
            throw new Exception("Message name byte size is more than 58 byte limit on names! Byte size: " + Encoding.UTF8.GetByteCount(name));
        }

        messages.Add(name, del);
    }






    public delegate void MessageDelegate(ulong id, byte[][] Params);



    public class Message
    {
        public FixedString64Bytes name;
        public FixedString64Bytes[] ParamString;
        public int Ticks = 0;
        public int Life = 3;
        public byte id = 0;

        public Message(FixedString64Bytes name, FixedString64Bytes[] ParamString, int Life)
        {
            this.name = name;
            this.ParamString = ParamString;
            id = messageID;
            messageID++;
            this.Life = Life;
        }
        public Message(FixedString64Bytes name, FixedString64Bytes[] ParamString, int Life, byte id)
        {
            this.name = name;
            this.ParamString = ParamString;
            this.id = id;
            this.Life = Life;
        }
        public override string ToString()
        {
            return $"Name: {name} ID: {id} Life: {Life} Ticks: {Ticks}";
        }

    }
}


