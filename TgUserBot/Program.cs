using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TeleSharp.TL.Contacts;
using TeleSharp.TL.Messages;
using TLSharp;
using TLSharp.Core;

namespace TgUserBot
{
    static class Program
    {
        #region StaticVars
        static FileSessionStore store = new FileSessionStore();
        static TelegramClient client = new TelegramClient(1204435, "09f2f88cfeecfb6561741387b903b4f3", store);
        static TLUser User;
        static Thread thread;
        private static int offset=100;
        static StringBuilder sb = new StringBuilder();
        #endregion

        static async Task Main(string[] args)
        {
            label:
            #region Auth

            if (File.Exists("session.dat"))
            {
                if (client.IsUserAuthorized())
                {
                    await client.ConnectAsync();
                    User = client.Session.TLUser;
                }
                else
                {
                    File.Delete("session.dat");
                    goto label;
                }
            }
            else
            {
                string hash = "", number = "";
                try
                {
                    await client.ConnectAsync();
                    Console.Write("Enter phone number:");
                    number = Console.ReadLine();
                    hash = await client.SendCodeRequestAsync(number);
                    Console.Write("Sending code");
                    await Task.Delay(500);
                    Console.Write(".");
                    await Task.Delay(500);
                    Console.Write(".");
                    await Task.Delay(500);
                    Console.WriteLine(".");
                    File.WriteAllText("hash", hash);
                    
                }
                catch (Exception ex)
                {
                    if (ex.Message == "AUTH_RESTART")
                    {
                        hash = File.ReadAllText("hash");
                    }
                }
                Console.Write("Enter Code:");
                var code = Console.ReadLine();
                try
                {
                    User = await client.MakeAuthAsync(number, hash, code);
                }
                catch (TLSharp.Core.Exceptions.CloudPasswordNeededException)
                {
                    TLPassword password = await client.GetPasswordSetting();
                    Console.WriteLine($"Enter cloud pasword ({password.Hint})");
                    var pass = ReadPass();
                    User = await client.MakeAuthWithPasswordAsync(password, pass);
                }
                if (client.IsUserAuthorized() && File.Exists("hash"))
                {
                    File.Delete("hash");
                }

            }
            #endregion

            Console.WriteLine("[+]Logined!");

            var dialogs = (TLDialogsSlice)await client.GetUserDialogsAsync();
            var chats = dialogs.Users;
            
            Console.WriteLine(chats.Count);
            foreach (TLUser chat in chats)
            {
                /*
                try
                {
                    if (chat.Bot)
                    {
                        continue;
                    }
                    TLInputPeerUser inputPeer = new TLInputPeerUser()
                    {
                        UserId = chat.Id,
                        AccessHash = (long)chat.AccessHash
                    };
                    TLAbsMessages res = await client.SendRequestAsync<TLAbsMessages>(new TLRequestGetHistory()
                    {
                        Peer = inputPeer,
                        Limit = 1000,
                        AddOffset = offset,
                        OffsetId = 0
                    });
                    TLMessages tLMessages = new();
                    TLMessagesSlice tLMessagesSlice = new();
                    TLVector<TLAbsMessage> msgs = new();
                    if (res is TLMessages)
                    {
                        tLMessages = res as TLMessages;
                        msgs = tLMessages.Messages;
                        Console.WriteLine("TLMessages");
                    }
                    else
                    {
                        tLMessagesSlice = res as TLMessagesSlice;
                        msgs = tLMessagesSlice.Messages;
                        Console.WriteLine("TLMessagesSlice");
                    }
                    while (true)
                        if (msgs.Count > offset)
                        {
                            offset += msgs.Count;
                            foreach (TLAbsMessage msg in msgs)
                            {
                                if (msg is TLMessage)
                                {
                                    TLMessage message = msg as TLMessage;
                                    
                                    sb.Append("\t" +
                                        message.Id + "\t" + message.FromId + "\t" + message.Message + Environment.NewLine);
                                }
                                if (msg is TLMessageService)
                                    continue;

                            }
                            await Task.Delay(250); //to avoid TelegramFloodException
                        }
                        else
                        {
                            File.WriteAllText("Messages.txt", sb.ToString());
                            break;
                        }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                */
                await Task.Delay(250);
                chat.getMessAsync(offset);
            }
           
            
        }

        static string ReadPass()
        {
            string pass = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
                else if(key.Key != ConsoleKey.Backspace)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                
                else
                {
                    Console.Write("\b");
                    Console.Write(" ");
                    Console.Write("\b");
                    pass = pass.Remove(pass.Length - 1);
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);
            return pass;
        }
        static async Task getMessAsync(this TLUser user, int offset)
        {
            //new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        TLInputPeerUser inputPeer = new TLInputPeerUser()
                        {
                            UserId = user.Id,
                            AccessHash = (long)user.AccessHash
                        };
                        TLAbsMessages res =  client.SendRequestAsync<TLAbsMessages>(new TLRequestGetHistory()
                        {
                            Peer = inputPeer,
                            Limit = 1000,
                            AddOffset = offset,
                            OffsetId = 0
                        }).Result;
                        TLMessages tLMessages = new();
                        TLMessagesSlice tLMessagesSlice = new();
                        TLAbsMessage msgs;
                        if (res is TLMessages)
                        {
                            tLMessages = res as TLMessages;
                            msgs = tLMessages.Messages[0];
                            Console.WriteLine("TLMessages");
                        }
                        else
                        {
                            tLMessagesSlice = res as TLMessagesSlice;
                            msgs = tLMessagesSlice.Messages[0];
                            Console.WriteLine("TLMessagesSlice");
                        }
                        var message = msgs as TLMessage;
                        Console.WriteLine(message.Id + "\t" + message.FromId + "\t" + message.Message);
                        await Task.Delay(22000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            }//).Start();
        }
    }
}
