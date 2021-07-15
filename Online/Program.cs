using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TeleSharp.TL.Messages;
using TLSharp.Core;

namespace Online
{
    class Program
    {
        #region StaticVars
        static FileSessionStore store = new FileSessionStore();
        static TelegramClient client = new TelegramClient(1204435, "09f2f88cfeecfb6561741387b903b4f3", store);
        static TLUser User;
        static Thread thread;
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

            Online();

            Console.ReadKey();
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
                else if (key.Key != ConsoleKey.Backspace)
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
        static void Online()
        {
            thread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(250);
                        var a = client.SendRequestAsync<bool>(new TLRequestUpdateStatus()).Result;
                        if (a)
                        {
                            Console.WriteLine("[!]Detected OFF");
                            Thread.Sleep(250);
                            if (!client.SendRequestAsync<bool>(new TLRequestUpdateStatus() { Offline = false }).Result)
                            {
                                Console.WriteLine("[+]User again online");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("AUTH_KEY_UNREGISTERED") || ex.Message.Contains("SESSION_REVOKED"))
                        {
                            File.Delete("session.dat");
                            Console.WriteLine("[e]Process end with error. Restart app");
                            Process.GetCurrentProcess().Kill();
                        }
                        else
                        {
                            Console.WriteLine("[!]Status checking filed");
                            Console.WriteLine("[e]" + ex.Message);
                        }
                    }

                }

            });
            thread.Start();
        }
    }
}
