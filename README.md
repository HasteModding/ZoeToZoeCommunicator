# ZoeToZoeCommunicator
Mod enabling a hacky way of client to client messaging

To the best of my ability and research I could not find a way to have two modded clients message eachother without having to go through a modded server first. So I made my own, for when you want modded clients to talk to eachother, but dont want to require the server to be modded aswell.

I couldnt think of too many use cases where this would be needed, because 90% of the time you need clients to message another it would likely cause desyncing where you'd need the server owner to be modded aswell, in which case you could use `[ServerRpc]`'s, `NetworkObjects`, or `NetworkManager.Singleton.CustomMessagingManager` to handle syncing clients and communicating events.
That being said I had a use case with my Zoe recolor mod so maybe others might have uses for it aswell.

## How To

To use this mod in your projects you need to add a reference to its mod folder in your mods .csproj like this:
`<Reference Include="C:\Program Files (x86)\Steam\steamapps\workshop\content\1796470\TempModUploadFolder\*.dll" Private="false"/>` TODO: GET ACTUAL MOD FOLDER NUMBER WHEN UPLOADED
The path to your workshop folder might be different for you so you should verify its location before using, this is just where it is for me.

Then at the top of your mod you can include its namespace with the rest of your using like this:
`using ZoeToZoe;`

To send messages you use:
`ClientMessageManager.SendMessage(string name, byte[][] Params, int life = 5, byte targetId = 255)`
Where:
       name is the name of the message used to identify its corresponding delegate.
       Params is a 2d array of bytes to send as parameters to its delegate.
              Why bytes? The limmiting factor of this system as a whole is it has to send messages as a `fixedString64Bytes` which as a limit of 61 bytes (3 bytes are reserved by fixedString64Bytes for formatting) so each byte[] in Params can only be 61 bytes long, and while you can use strings using `Encoding.UTF8.GetBytes(string s)` it can be much more efficient and encouraged to make your own encoding and decoding functions for byte[]'s (examples included at the bottom.)
       life is how many network ticks to be broadcasting this message for, this is useful to ensure others can more reliably recieve this message due to the potential innacuracies of client network ticks, too little and clients might never see the message, but too high (like ~60) and clients might accidentally see the message twice, I found 3 - 10 to be a fairly consistent number, feel free to change this as needed.
       targetId is the target clientId to recieve the message on (255 represents sending the message to ALL clients), this byte gets appended to the message name then examined by recieving clients to see if it applies to them, since its a byte it only supports values 0-255 where 0-254 represents specific client IDs and 255 represents all client IDs, while this means it only supports unique messaging for up to 254 NGOPlayer spawns I figured this was fairly unrealistic but I can make adjustments to support more if needed in the future.

To set up a recieving delegate you use:
`ClientMessageManager.RegisterMessageDelegate(string name, MessageDelegate del)`
Where:
       name is the name used to identify what messages should use this delegate. Note that message names can only be 58 bytes in length due to the fixedString64byte formatting and then 3 bytes used for encoding message life, a message ID, and recieving client IDs. You can check the byte length of the name with `Encoding.UTF8.GetByteCount(string s)` but this is already done internally and will throw an exception if a messages name exceeds the valid length.
       del is the corresponding method to call when recieving a message from message name. MessageDelegate has the signature `public delegate void MessageDelegate(ulong id, byte[][] Params);` where id is the client ID of the client broadcasting the message and Params are the parameters sent with the message.

##Important notes
A byte[] parameter filled with 255 (255 sixty one times) is invalid as it is used internally as a message terminator marking the end of a current messages parameters

## Examples
A simple example messanger using simple UTF8 Encoding and decoding
```
using Landfall.Haste;
using Landfall.Modding;
using UnityEngine;
using Zorro.Core.CLI;
using Unity.Netcode;
using System.Text;
using ZoeToZoe;

namespace NetworkTest;

[LandfallPlugin]
public class NetworkTest
{
    static NetworkTest()
    {
        ClientMessageManager.RegisterMessageDelegate("TestPing", PingCallBack);
    }

    private static void PingCallBack(ulong clientId, byte[][] Params)
    {
        string msg = Encoding.UTF8.GetString(Params[0]);
        Debug.Log($"Ping Recieved from: {clientId} with Message: {msg}");
    }

    [ConsoleCommand]
    public static void PingServer()
    {
        string msg = $"I am id: {NetworkManager.Singleton.LocalClientId} sending ping";
        byte[][] Params = [Encoding.UTF8.GetBytes(msg)];
        ClientMessageManager.SendMessage("TestPing", Params, 5, 0); //client id of 0 is the server host client id
    }


    [ConsoleCommand]
    public static void PingAll()
    {
        string msg = $"I am id: {NetworkManager.Singleton.LocalClientId} sending ping";
        byte[][] Params = new byte[1][];
        Params[0] = Encoding.UTF8.GetBytes(msg);
        ClientMessageManager.SendMessage("TestPing", Params);
    }
}
```
An example of byte encoding and decoding used in my Zoe recolor mod to encode and decode color presets
```
    public byte[] ToByteParams()
    {
        byte[] toReturn = new byte[]{(byte)(int)(mainColor.r*255),(byte)(int)(mainColor.g*255),(byte)(int)(mainColor.b*255),
            (byte)(int)(accentColor.r*255),(byte)(int)(accentColor.g*255),(byte)(int)(accentColor.b*255),
            (byte)(int)(highlightColor.r*255),(byte)(int)(highlightColor.g*255),(byte)(int)(highlightColor.b*255),
            (byte)(int)(hairColor.r*255),(byte)(int)(hairColor.g*255),(byte)(int)(hairColor.b*255)};

        string byteString = "";
        foreach (byte b in toReturn) {
            byteString += (int)b + " ";
        }
        Debug.LogWarning("Sending Bytes: "+byteString);

        return toReturn;
    }
    public ColorPreset(byte[] bytes)
    {

        this.title = "NA";
        this.mainColor = new Color((int)bytes[0] / 255f, (int)bytes[1] / 255f, (int)bytes[2] / 255f);
        this.accentColor = new Color((int)bytes[3] / 255f, (int)bytes[4] / 255f, (int)bytes[5] / 255f);
        this.highlightColor = new Color((int)bytes[6] / 255f, (int)bytes[7] / 255f, (int)bytes[8] / 255f);
        this.hairColor = new Color((int)bytes[9] / 255f, (int)bytes[10] / 255f, (int)bytes[11] / 255f);

        Debug.LogWarning(this.ToString());
    }
```
If you have any questions/suggestions feel free to lmk on discord
